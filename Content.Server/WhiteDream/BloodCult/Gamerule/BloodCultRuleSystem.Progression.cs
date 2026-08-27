// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

// WhiteDream - objective bookkeeping, the pentagram grace period and the victory wind-down.
using System.Linq;
using Content.Server.WhiteDream.BloodCult.Objectives;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.Mobs.Components;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Components;
using Content.Shared.WhiteDream.BloodCult.Runes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Gamerule;

public sealed partial class BloodCultRuleSystem
{
    private static readonly EntProtoId SacrificeObjective = "KillTargetCultObjective";
    private static readonly EntProtoId SummonObjective = "SummonNarsieObjective";

    private static readonly TimeSpan ObjectiveCheckInterval = TimeSpan.FromSeconds(5);

    private static readonly ProtoId<RuneSelectorPrototype> RendingSelector = "CultRuneDimensionalRending";

    private static readonly SoundSpecifier AscensionSound =
        new SoundPathSpecifier("/Audio/WhiteDream/BloodCult/curse.ogg");

    [Dependency] private IPrototypeManager _proto = default!;

    private void TickProgression(BloodCultRuleComponent rule)
    {
        var now = _timing.CurTime;

        if (now >= rule.NextObjectiveCheck)
        {
            rule.NextObjectiveCheck = now + ObjectiveCheckInterval;
            EnsureOfferingTarget(rule);
            EnsureObjectives(rule);
            CheckRendingUnlocked(rule);
            ReconcileLeader(rule); // Whiskey - keeps the rule and the world agreeing on the leader.
        }

        if (rule.RedEyesTime is { } redEyesTime && now >= redEyesTime)
        {
            rule.RedEyesTime = null;
            ApplyRedEyes(rule);
        }

        if (rule.PentagramTime is { } pentagramTime && now >= pentagramTime)
        {
            rule.PentagramTime = null;
            ApplyPentagrams(rule);
        }

        TickLeaderVote(rule);

        if (rule.VictoryEndTime is { } endTime && now >= endTime)
        {
            rule.VictoryEndTime = null;
            _roundEnd.EndRound();
        }
    }

    #region Objectives

    /// <summary>
    ///     The target used to be picked exactly once, when the first cultist was made. If nobody valid
    ///     had spawned yet it stayed null for the whole round and the cult never got a sacrifice.
    /// </summary>
    private void EnsureOfferingTarget(BloodCultRuleComponent rule)
    {
        // She has been given. Nar'Sie does not ask twice.
        if (rule.OfferingSacrificed)
            return;

        if (rule.OfferingTarget is { } target)
        {
            // Whiskey - the offering only counts on the rune now, so a body that no longer exists
            // can never be given and would lock the cult out of the rending rune for the rest of
            // the round. Name someone else instead. A corpse still on the station stays named:
            // being dead is not the same as being gone.
            if (IsAvailableOfferingTarget(target))
            {
                if (rule.ObjectivesOfferingTarget != target)
                {
                    RefreshOfferingObjectives(rule);
                    rule.ObjectivesOfferingTarget = target;
                }

                return;
            }
        }

        var previous = rule.OfferingTarget;
        SetRandomCultTarget(rule);

        if (rule.OfferingTarget == previous)
            return;

        RefreshOfferingObjectives(rule);
        rule.ObjectivesOfferingTarget = rule.OfferingTarget;

        if (rule.OfferingTarget is { } picked)
            NotifyCultists(Loc.GetString("cult-offering-target-chosen", ("name", Name(picked))));
    }

    /// <summary>
    ///     A sacrifice has to remain physically obtainable. Entering cryostorage makes the old body
    ///     unavailable even though the entity still exists on the paused cryo map.
    /// </summary>
    private bool IsAvailableOfferingTarget(EntityUid target) =>
        !TerminatingOrDeleted(target) &&
        !HasComp<BloodCultistComponent>(target) &&
        !HasComp<CryostorageContainedComponent>(target);

    /// <summary>
    ///     Existing cultists already own an objective entity naming the previous offering. Replace
    ///     that objective so their character panel follows the rule's newly selected target.
    /// </summary>
    private void RefreshOfferingObjectives(BloodCultRuleComponent rule)
    {
        foreach (var cultist in rule.Cultists)
        {
            if (!cultist.Comp.ObjectivesGranted ||
                TerminatingOrDeleted(cultist.Owner) ||
                !_mind.TryGetMind(cultist.Owner, out var mindId, out var mind))
                continue;

            for (var i = mind.Objectives.Count - 1; i >= 0; i--)
            {
                if (HasComp<KillTargetCultComponent>(mind.Objectives[i]))
                    _mind.TryRemoveObjective(mindId, mind, i);
            }

            if (rule.OfferingTarget is not null && rule.OfferingTarget != cultist.Owner)
                _mind.TryAddObjective(mindId, mind, SacrificeObjective);
        }
    }

    /// <summary>
    ///     Hands out the sacrifice and summon objectives. Runs on a tick because the mind role isn't
    ///     always in place at the moment the cultist component is added.
    /// </summary>
    private void EnsureObjectives(BloodCultRuleComponent rule)
    {
        if (rule.OfferingTarget is null)
            return;

        foreach (var cultist in rule.Cultists)
        {
            if (cultist.Comp.ObjectivesGranted || TerminatingOrDeleted(cultist.Owner))
                continue;

            if (!_mind.TryGetMind(cultist.Owner, out var mindId, out var mind))
                continue;

            // The sacrifice target can't be one of us, and there is nothing to offer once she has
            // already been given.
            if (rule.OfferingTarget != cultist.Owner && !rule.OfferingSacrificed)
                _mind.TryAddObjective(mindId, mind, SacrificeObjective);

            _mind.TryAddObjective(mindId, mind, SummonObjective);
            cultist.Comp.ObjectivesGranted = true;
        }
    }

    /// <summary>
    ///     How many cultists the rending rune needs, read straight off the rune selector so the
    ///     prototype stays the single source of truth.
    /// </summary>
    public int GetRendingCultistsRequired()
    {
        return _proto.TryIndex(RendingSelector, out var selector) ? GetRequiredCultists(selector) : 0;
    }

    /// <summary>
    ///     Whiskey - a rune can ask for a share of the crew instead of a flat count.
    /// </summary>
    public int GetRequiredCultists(RuneSelectorPrototype selector) =>
        _cultPopulation.GetRequiredCultists(selector);

    public bool CanRendingBeDrawn(BloodCultRuleComponent rule)
    {
        return rule.Cultists.Count >= GetRendingCultistsRequired() && IsObjectiveFinished();
    }

    /// <summary>
    ///     Tells the cult, once, the moment both conditions are finally met.
    /// </summary>
    private void CheckRendingUnlocked(BloodCultRuleComponent rule)
    {
        if (rule.RendingUnlockedAnnounced || rule.VeilWeakened || !CanRendingBeDrawn(rule))
            return;

        rule.RendingUnlockedAnnounced = true;

        NotifyCultists(Loc.GetString("cult-rending-unlocked"));

        if (rule.EmergencyMarkersMode)
        {
            NotifyCultists(Loc.GetString("cult-status-rending-emergency", ("amount", rule.EmergencyMarkersCount)));
            return;
        }

        foreach (var site in GetAvailableRendingSites(rule))
            NotifyCultists(Loc.GetString("cult-status-rending-location", ("location", site.Name)));
    }

    #endregion

    #region Ascension

    /// <summary>
    ///     The cult hit the pentagram threshold. Warn them first, brand them later.
    /// </summary>
    /// <summary>
    ///     Whiskey - the veil thins as the cult grows, and the cult is told before it shows.
    /// </summary>
    private void BeginRedEyes(BloodCultRuleComponent rule)
    {
        if (rule.RedEyesApplied || rule.RedEyesTime is not null)
            return;

        rule.RedEyesTime = _timing.CurTime + rule.RedEyesWarningDelay;

        NotifyCultists(Loc.GetString("cult-red-eyes-warning",
            ("minutes", (int) Math.Round(rule.RedEyesWarningDelay.TotalMinutes))));

        FlickerStationLights(TimeSpan.FromSeconds(3));
    }

    private void ApplyRedEyes(BloodCultRuleComponent rule)
    {
        rule.RedEyesApplied = true;

        foreach (var cultist in rule.Cultists)
        {
            if (TerminatingOrDeleted(cultist.Owner))
                continue;

            // Trauma - eye colour is stored on the eye organ now
            cultist.Comp.OriginalEyeColor ??= _humanoid.GetEyeColor(_humanoid.GetOrgansData(cultist));
            _humanoid.SetEyeColor(cultist, rule.EyeColor);
        }

        NotifyCultists(Loc.GetString("cult-red-eyes-marked"));
    }

    private void BeginAscension(BloodCultRuleComponent rule)
    {
        if (rule.PentagramApplied || rule.PentagramTime is not null)
            return;

        rule.PentagramTime = _timing.CurTime + rule.PentagramWarningDelay;

        NotifyCultists(Loc.GetString("cult-ascension-warning",
            ("minutes", (int) Math.Round(rule.PentagramWarningDelay.TotalMinutes))));

        // The station notices before the crew knows why.
        FlickerStationLights(TimeSpan.FromSeconds(6));

        _chat.DispatchGlobalAnnouncement(Loc.GetString("cult-ascension-centcom-announcement"),
            Loc.GetString("cult-ascension-centcom-sender"),
            true,
            colorOverride: Color.Goldenrod);
    }

    private void ApplyPentagrams(BloodCultRuleComponent rule)
    {
        rule.PentagramApplied = true;

        foreach (var cultist in rule.Cultists)
        {
            if (!TerminatingOrDeleted(cultist.Owner))
                EnsureComp<PentagramComponent>(cultist);
        }

        NotifyCultists(Loc.GetString("cult-ascension-marked"));

        FlickerStationLights(TimeSpan.FromSeconds(10));
        PlayGlobalCultSound(AscensionSound, -4f);
    }

    #endregion
}
