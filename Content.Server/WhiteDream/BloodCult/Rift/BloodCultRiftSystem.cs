// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

// Ported from funky-station (BloodCultRiftSystem) and adapted to our gamerule.
using Content.Server.Audio;
using Content.Server.Lightning; // Whiskey
using Content.Server.Popups;
using Content.Trauma.Common.Language.Systems;
using Content.Server.Mind;
using Content.Server.WhiteDream.BloodCult.Commune;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Server.WhiteDream.BloodCult.Runes;
using Content.Shared.Anomaly;
using Content.Shared.Audio;
using Content.Shared.Camera; // Whiskey
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Gibbing;
using Content.Shared.Popups;
using Content.Shared.Mobs.Systems;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems; // Whiskey
using Robust.Shared.Random;

namespace Content.Server.WhiteDream.BloodCult.Rift;

/// <summary>
///     Makes the rift bleed, and runs the final summoning chant on the runes around it.
/// </summary>
public sealed partial class BloodCultRiftSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private BloodCultRuleSystem _cultRule = default!;
    [Dependency] private BloodCultCommuneSystem _commune = default!;
    [Dependency] private CommonLanguageSystem _language = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ServerGlobalSoundSystem _sound = default!;
    // <Whiskey>
    [Dependency] private LightningSystem _lightning = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;
    // </Whiskey>
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FinalSummoningRuneComponent, TryInvokeCultRuneEvent>(OnFinalRuneInvoked);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BloodCultRiftComponent>();
        while (query.MoveNext(out var uid, out var rift))
        {
            rift.TimeUntilNextPulse -= frameTime;
            if (rift.TimeUntilNextPulse <= 0f)
            {
                Pulse(uid, rift);
                rift.TimeUntilNextPulse = rift.PulseInterval;
            }

            rift.TimeUntilNextGuardian -= frameTime;
            if (rift.TimeUntilNextGuardian <= 0f)
                TrySpawnGuardian(uid, rift);

            if (!rift.RitualInProgress)
                continue;

            rift.TimeUntilNextChant -= frameTime;
            if (rift.TimeUntilNextChant <= 0f)
                ProcessChantStep(uid, rift);
        }
    }

    /// <summary>
    ///     Tops the pool back up so the rift keeps spilling a slowly growing ocean of blood.
    /// </summary>
    private void Pulse(EntityUid uid, BloodCultRiftComponent rift)
    {
        if (_solution.TryGetSolution(uid, BloodCultRiftComponent.SolutionName, out var solutionEnt, out _))
            _solution.TryAddReagent(solutionEnt.Value,
                BloodCultRiftComponent.Reagent,
                FixedPoint2.New(rift.BloodPerPulse),
                out _);

        if (HasComp<AppearanceComponent>(uid))
            _appearance.SetData(uid, AnomalyVisualLayers.Animated, true);
    }

    #region Guardians

    /// <summary>
    ///     Something crawls out of the wound. Whiskey - it comes out once and only once. The rift
    ///     used to patch its own guard, which turned the fight into a queue: the crew killed a
    ///     hellspawn, caught their breath, and the next one was already crawling out.
    /// </summary>
    private void TrySpawnGuardian(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        rift.TimeUntilNextGuardian = rift.RitualInProgress
            ? rift.RitualGuardianInterval
            : rift.GuardianInterval;

        if (rift.GuardiansSpawned >= rift.TotalGuardians)
            return;

        rift.Guardians.RemoveAll(guardian => TerminatingOrDeleted(guardian) || _mobState.IsDead(guardian));

        var cap = rift.RitualInProgress ? rift.RitualMaxGuardians : rift.MaxGuardians;
        if (rift.Guardians.Count >= cap)
            return;

        var coordinates = Transform(riftUid).Coordinates
            .Offset(_random.NextVector2(rift.GuardianSpawnRange));

        rift.Guardians.Add(Spawn(rift.GuardianProto, coordinates));
        rift.GuardiansSpawned++;
    }

    #endregion

    #region Ritual

    private void OnFinalRuneInvoked(Entity<FinalSummoningRuneComponent> rune, ref TryInvokeCultRuneEvent args)
    {
        if (rune.Comp.Rift is not { } riftUid || !TryComp<BloodCultRiftComponent>(riftUid, out var rift))
        {
            args.Cancel();
            return;
        }

        if (rift.RitualInProgress)
        {
            _popup.PopupEntity(Loc.GetString("cult-final-ritual-already"), rune, args.User, PopupType.MediumCaution);
            args.Cancel();
            return;
        }

        if (!_cultRule.IsObjectiveFinished())
        {
            _popup.PopupEntity(Loc.GetString("cult-rending-target-alive"), rune, args.User, PopupType.LargeCaution);
            args.Cancel();
            return;
        }

        var participants = GetParticipants(rift);
        if (participants.Count < rift.RequiredCultists)
        {
            _popup.PopupEntity(
                Loc.GetString("cult-final-ritual-not-enough",
                    ("current", participants.Count),
                    ("required", rift.RequiredCultists)),
                rune,
                args.User,
                PopupType.LargeCaution);
            args.Cancel();
            return;
        }

        rift.RitualInProgress = true;
        rift.RitualStartingRequiredCultists = rift.RequiredCultists;
        rift.ChantsInCycle = 0;
        rift.SacrificesDone = 0;
        rift.PendingSacrifice = null;
        rift.TimeUntilNextChant = 0f;

        StartMusic(riftUid, rift);
        _cultRule.NotifyCultists(Loc.GetString("cult-final-ritual-started"));
        ProcessChantStep(riftUid, rift);
    }

    private void ProcessChantStep(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        var participants = GetParticipants(rift);
        if (participants.Count < rift.RequiredCultists)
        {
            AbortRitual(riftUid, rift, participants.Count);
            return;
        }

        // Whoever draws the long chant is the next to be taken. Foreshadowing is half the fun.
        if (rift.PendingSacrifice is not { } pending || !participants.Contains(pending))
            rift.PendingSacrifice = _random.Pick(participants);

        // Everyone chants. The marked one always says more, and the whole cult says more as the
        // ritual tightens, so it builds from a murmur into a frenzy instead of cutting abruptly.
        var leaderChant = _commune.GenerateChant(rift.LeaderChantWords);
        foreach (var cultist in participants)
        {
            var line = cultist == rift.PendingSacrifice
                ? leaderChant
                : _commune.GenerateChant(rift.FollowerChantWords);

            _cultRule.Speak(cultist, line);
        }

        rift.ChantsInCycle++;

        var cycle = rift.CurrentCycle;
        if (rift.ChantsInCycle <= cycle.Count)
        {
            rift.TimeUntilNextChant = cycle[rift.ChantsInCycle - 1];
            return;
        }

        // The cycle finished, so the veil takes the one who was chanting.
        if (!TakeSacrifice(riftUid, rift))
        {
            // Couldn't take them, start the cycle over rather than stalling.
            rift.PendingSacrifice = null;
            rift.ChantsInCycle = 0;
            rift.TimeUntilNextChant = 1f;
            return;
        }

        rift.SacrificesDone++;
        rift.RequiredCultists = Math.Max(1, rift.RequiredCultists - 1);
        rift.PendingSacrifice = null;
        rift.ChantsInCycle = 0;
        rift.TimeUntilNextChant = 1f;

        // Whiskey - the first one she takes is the moment Central Command notices, so the station
        // gets its octarine before the other two are eaten.
        if (rift.SacrificesDone == 1)
            _cultRule.ForceAlertLevel(rift.FirstSacrificeAlertLevel);

        _cultRule.NotifyCultists(Loc.GetString("cult-final-ritual-sacrifice",
            ("done", rift.SacrificesDone),
            ("required", rift.RequiredSacrifices)));

        if (rift.SacrificesDone >= rift.RequiredSacrifices)
            SummonNarsie(riftUid, rift);
    }

    private void AbortRitual(EntityUid riftUid, BloodCultRiftComponent rift, int present)
    {
        rift.RitualInProgress = false;
        rift.ChantsInCycle = 0;
        // Each completed sacrifice lowers the live requirement. Restore the exact value from the
        // beginning of the attempt: adding SacrificesDone is wrong once the requirement hits its
        // lower bound of one.
        if (rift.RitualStartingRequiredCultists is { } startingRequirement)
            rift.RequiredCultists = startingRequirement;

        rift.RitualStartingRequiredCultists = null;
        rift.SacrificesDone = 0;
        rift.PendingSacrifice = null;
        rift.TimeUntilNextChant = 0f;
        StopMusic(riftUid, rift);

        _popup.PopupEntity(Loc.GetString("cult-final-ritual-broken"), riftUid, PopupType.LargeCaution);
        _cultRule.NotifyCultists(Loc.GetString("cult-final-ritual-failed",
            ("current", present),
            ("required", rift.RequiredCultists)));
    }

    /// <summary>
    ///     Nar'Sie takes the one who was chanting and gives them back as a herald. Being offered on
    ///     the rift is a promotion, not a soulstone - only the mindless end up as shards.
    /// </summary>
    private bool TakeSacrifice(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        if (rift.PendingSacrifice is not { } victim || TerminatingOrDeleted(victim))
            return false;

        var coordinates = Transform(victim).Coordinates;

        // Whiskey - she reaches down for the one she is taking. triggerLightningEvents is false
        // on purpose: this is spectacle, it must not hurt the cultists standing around the runes.
        _lightning.ShootLightning(riftUid, victim, rift.SacrificeLightningProto, false);

        if (!_mind.TryGetMind(victim, out var mindId, out _))
        {
            Spawn(rift.SoulShardGhostProto, coordinates);
        }
        else
        {
            var harvester = Spawn(rift.HarvesterProto, coordinates);
            _mind.TransferTo(mindId, harvester);
            _mind.UnVisit(mindId);
            _language.UpdateEntityLanguages(harvester);
        }

        _gibbing.Gib(victim);

        // Whiskey - the station shakes and the lights go with it.
        _cultRule.FlickerStationLights(rift.SacrificeFlickerTime);
        ShakeScreens(riftUid, rift);

        return true;
    }

    /// <summary>
    ///     Whiskey - a kick to everyone chanting, plus the rift itself so anyone watching from
    ///     nearby feels it too.
    /// </summary>
    private void ShakeScreens(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        foreach (var cultist in GetParticipants(rift))
        {
            if (!TerminatingOrDeleted(cultist))
                _recoil.KickCamera(cultist, _random.NextVector2(0.5f, 1.5f));
        }

        if (!TerminatingOrDeleted(riftUid))
            _recoil.KickCamera(riftUid, _random.NextVector2(0.5f, 1.5f));
    }

    private void SummonNarsie(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        rift.RitualInProgress = false;
        rift.RitualStartingRequiredCultists = null;
        rift.TimeUntilNextChant = 0f;
        StopMusic(riftUid, rift);

        // Whiskey - she is heard before she is seen. The announcement itself comes from Nar'Sie's
        // own AnnounceOnSpawn a moment later; saying it here as well gave the station two.
        _sound.PlayGlobalOnStation(riftUid, _audio.ResolveSound(rift.SummonSound));

        RaiseLocalEvent(new BloodCultNarsieSummoned());
        Spawn(rift.NarsiePrototype, _transform.GetMapCoordinates(riftUid));
    }

    private void StartMusic(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        if (rift.MusicPlaying)
            return;

        _sound.DispatchStationEventMusic(riftUid, rift.RitualMusic, StationEventMusicType.BloodCult);
        rift.MusicPlaying = true;
    }

    private void StopMusic(EntityUid riftUid, BloodCultRiftComponent rift)
    {
        if (!rift.MusicPlaying)
            return;

        _sound.StopStationEventMusic(riftUid, StationEventMusicType.BloodCult);
        rift.MusicPlaying = false;
    }

    private List<EntityUid> GetParticipants(BloodCultRiftComponent rift)
    {
        return _cultRule.GetCultistsOnRunes(rift.SummoningRunes, rift.RuneRange);
    }

    #endregion
}
