// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Systems;
using Content.Server.Whiskey.Native;
using Content.Shared._ES.Camera;
using Content.Shared.Administration.Logs;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Humanoid;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Whiskey.OperativeHidden;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Whiskey.OperativeHidden;

/// <summary>
/// Runs the Hidden Operative's deliberately theatrical triclor execution in
/// ordinary ECS code. The victim takes only a trace amount of real damage;
/// the temporary death threshold supplies a persistent, normally revivable
/// corpse without displaying hundreds of points of artificial damage.
/// </summary>
public sealed partial class OperativeHiddenTriclorSystem : EntitySystem
{
    private const float MeleeRange = 1.5f;
    private static readonly TimeSpan SequenceDuration = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan VomitInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ScreamInterval = TimeSpan.FromSeconds(2);
    private static readonly ProtoId<ReagentPrototype> Triclor = "OperativeHiddenTriclor";

    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private ESScreenshakeSystem _screenshake = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThresholds = default!;
    [Dependency] private SharedPuddleSystem _puddle = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private VomitSystem _vomit = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NativeAntagComponent, OperativeHiddenTriclorActionEvent>(OnTriclorAction);
        SubscribeLocalEvent<OperativeHiddenTriclorComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<OperativeHiddenTriclorComponent, RejuvenateEvent>(OnRejuvenated);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<OperativeHiddenTriclorComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var triclor, out var mobState))
        {
            if (triclor.DeathApplied)
                continue;

            // An unrelated death ends the pending presentation instead of
            // continuing to torment a corpse or its newly detached ghost.
            if (mobState.CurrentState == MobState.Dead)
            {
                RemCompDeferred<OperativeHiddenTriclorComponent>(uid);
                continue;
            }

            while (now >= triclor.NextVomitAt && triclor.NextVomitAt < triclor.DeathAt)
            {
                triclor.VomitCount++;
                Vomit(uid, bloody: triclor.VomitCount is 3 or 6);
                triclor.NextVomitAt += VomitInterval;
            }

            while (now >= triclor.NextScreamAt && triclor.NextScreamAt < triclor.DeathAt)
            {
                Scream(uid);
                triclor.NextScreamAt += ScreamInterval;
            }

            if (!triclor.HandHallucinationSent && now >= triclor.HandHallucinationAt)
            {
                triclor.HandHallucinationSent = true;
                RaiseNetworkEvent(
                    new OperativeHiddenHallucinationEvent(OperativeHiddenHallucinationType.Hand),
                    uid);
            }

            if (!triclor.EyeHallucinationSent && now >= triclor.EyeHallucinationAt)
            {
                triclor.EyeHallucinationSent = true;
                RaiseNetworkEvent(
                    new OperativeHiddenHallucinationEvent(OperativeHiddenHallucinationType.Eye),
                    uid);
            }

            if (now >= triclor.DeathAt)
                ApplyDeath((uid, triclor), mobState);
        }
    }

    private void OnTriclorAction(
        Entity<NativeAntagComponent> operative,
        ref OperativeHiddenTriclorActionEvent args)
    {
        if (args.Handled || !IsValidTarget(operative.Owner, args.Target, out var thresholds))
        {
            if (!args.Handled)
                _popup.PopupEntity(
                    Loc.GetString("operative-hidden-popup-invalid"),
                    operative.Owner,
                    operative.Owner,
                    PopupType.Medium);
            return;
        }

        var now = _timing.CurTime;
        var victim = EnsureComp<OperativeHiddenTriclorComponent>(args.Target);
        victim.Source = operative.Owner;
        victim.NextVomitAt = now + VomitInterval;
        victim.NextScreamAt = now + ScreamInterval;
        victim.HandHallucinationAt = now + TimeSpan.FromSeconds(2);
        victim.EyeHallucinationAt = now + TimeSpan.FromSeconds(6);
        victim.DeathAt = now + SequenceDuration;
        victim.VomitCount = 1;
        victim.OriginalDeathThreshold = _mobThresholds.GetThresholdForState(
            args.Target,
            MobState.Dead,
            thresholds);

        InjectTriclor(args.Target);
        Vomit(args.Target, bloody: false);
        Scream(args.Target);

        _jitter.DoJitter(
            args.Target,
            SequenceDuration,
            refresh: true,
            amplitude: 24f,
            frequency: 9f,
            forceValueChange: true);

        var translation = new ESScreenshakeParameters
        {
            Trauma = 0.8f,
            DecayRate = 0.0125f,
            Frequency = 0.035f,
        };
        var rotation = new ESScreenshakeParameters
        {
            Trauma = 0.65f,
            DecayRate = 0.01f,
            Frequency = 0.04f,
        };
        _screenshake.Screenshake(args.Target, translation, rotation);

        _popup.PopupEntity(
            Loc.GetString("operative-hidden-triclor-applied-target"),
            args.Target,
            args.Target,
            PopupType.LargeCaution);
        _popup.PopupEntity(
            Loc.GetString("operative-hidden-triclor-applied-user", ("target", args.Target)),
            args.Target,
            operative.Owner,
            PopupType.Medium);

        _adminLog.Add(
            LogType.Action,
            LogImpact.Extreme,
            $"{ToPrettyString(operative.Owner):user} injected hypercatalyzed triclor into {ToPrettyString(args.Target):target}");

        args.Handled = true;
    }

    private bool IsValidTarget(
        EntityUid operative,
        EntityUid target,
        out MobThresholdsComponent thresholds)
    {
        thresholds = default!;
        if (!Exists(target) ||
            TerminatingOrDeleted(target) ||
            target == operative ||
            HasComp<OperativeHiddenTriclorComponent>(target) ||
            HasComp<NativeAntagComponent>(target) ||
            HasComp<ZombieImmuneComponent>(target) ||
            !HasComp<HumanoidProfileComponent>(target))
            return false;

        if (!TryComp<MobStateComponent>(target, out var state) ||
            state.CurrentState == MobState.Dead ||
            !_mobState.HasState(target, MobState.Dead, state))
            return false;

        if (!TryComp<MobThresholdsComponent>(target, out var targetThresholds) || targetThresholds is null)
            return false;

        thresholds = targetThresholds;
        if (!_transform.InRange(Transform(operative).Coordinates, Transform(target).Coordinates, MeleeRange))
            return false;

        return true;
    }

    private void InjectTriclor(EntityUid target)
    {
        if (!TryComp<BloodstreamComponent>(target, out var bloodstream) ||
            !_solutions.ResolveSolution(
                target,
                bloodstream.BloodSolutionName,
                ref bloodstream.BloodSolution,
                out _))
        {
            return;
        }

        _solutions.TryAddReagent(
            bloodstream.BloodSolution!.Value,
            Triclor.Id,
            FixedPoint2.New(1),
            out _);
    }

    private void Vomit(EntityUid target, bool bloody)
    {
        // Small satiation deltas keep the spectacle from secretly inflicting a
        // second large punishment through hunger and thirst.
        _vomit.Vomit(target, thirstAdded: -2f, hungerAdded: -2f, force: true);
        if (!bloody || !TryComp<BloodstreamComponent>(target, out var bloodstream))
            return;

        var bloodData = _bloodstream.GetEntityBloodData((target, bloodstream));
        var solution = new Solution();
        solution.AddReagent(new ReagentId("Blood", bloodData), FixedPoint2.New(4));
        solution.AddReagent(new ReagentId("Vomit", bloodData), FixedPoint2.New(1));
        _puddle.TrySpillAt(target, solution, out _, sound: false);
    }

    private void Scream(EntityUid target)
    {
        _chat.TryEmoteWithChat(
            target,
            "Scream",
            ignoreActionBlocker: true,
            forceEmote: true);
    }

    private void ApplyDeath(
        Entity<OperativeHiddenTriclorComponent> victim,
        MobStateComponent mobState)
    {
        if (victim.Comp.DeathApplied)
            return;

        victim.Comp.DeathApplied = true;

        // One point of poison is enough to anchor the lowered death threshold;
        // the health analyzer therefore reports a trace exposure, not 200+
        // points of fabricated cellular damage.
        var traceDamage = new DamageSpecifier();
        traceDamage.DamageDict.Add("Poison", FixedPoint2.New(1));
        _damageable.TryChangeDamage(
            victim.Owner,
            traceDamage,
            ignoreResistances: true,
            origin: Exists(victim.Comp.Source) && !TerminatingOrDeleted(victim.Comp.Source)
                ? victim.Comp.Source
                : null,
            ignoreGlobalModifiers: true,
            targetPart: Content.Medical.Common.Targeting.TargetBodyPart.Vital,
            ignoreBlockers: true,
            splitDamage: Content.Medical.Common.Damage.SplitDamageBehavior.SplitEnsureAll,
            canMiss: false);

        var vitalDamage = _mobThresholds.CheckVitalDamage(victim.Owner);
        var forcedThreshold = FixedPoint2.Max(FixedPoint2.New(0.01f), vitalDamage - FixedPoint2.New(0.01f));
        _mobThresholds.SetMobStateThreshold(victim.Owner, forcedThreshold, MobState.Dead);

        if (mobState.CurrentState != MobState.Dead)
            _mobState.ChangeMobState(
                victim.Owner,
                MobState.Dead,
                mobState,
                Exists(victim.Comp.Source) && !TerminatingOrDeleted(victim.Comp.Source)
                    ? victim.Comp.Source
                    : null);
    }

    private void OnMobStateChanged(
        Entity<OperativeHiddenTriclorComponent> victim,
        ref MobStateChangedEvent args)
    {
        if (!victim.Comp.DeathApplied || args.NewMobState == MobState.Dead)
            return;

        RestoreDeathThreshold(victim);
        RemCompDeferred<OperativeHiddenTriclorComponent>(victim.Owner);
    }

    private void OnRejuvenated(
        Entity<OperativeHiddenTriclorComponent> victim,
        ref RejuvenateEvent args)
    {
        if (victim.Comp.DeathApplied)
            RestoreDeathThreshold(victim);
        RemCompDeferred<OperativeHiddenTriclorComponent>(victim.Owner);
    }

    private void RestoreDeathThreshold(Entity<OperativeHiddenTriclorComponent> victim)
    {
        if (victim.Comp.OriginalDeathThreshold > FixedPoint2.Zero)
        {
            _mobThresholds.SetMobStateThreshold(
                victim.Owner,
                victim.Comp.OriginalDeathThreshold,
                MobState.Dead);
        }
    }
}
