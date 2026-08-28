using System.Buffers;
using System.Linq;
using System.Numerics;
using Content.Medical.Common.Damage;
using Content.Medical.Common.Targeting;
using Content.Server.Zombies;
using Content.Shared.Actions;
using Content.Shared.Administration.Systems;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Whiskey.Native;
using Content.Shared.Zombies;
using Robust.Shared.Log;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Whiskey.Native;

/// <summary>
/// Generic ECS/native adapter. It snapshots primitive facts, dispatches an ABI
/// event, validates returned entity handles, and executes primitive commands.
/// It intentionally contains no Hidden Operative state machine or combo rules.
/// </summary>
public sealed partial class NativeAntagBridgeSystem : EntitySystem
{
    private const float MeleeRange = 1.5f;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThresholds = default!;
    [Dependency] private NpcFactionSystem _factions = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ZombieSystem _zombies = default!;

    private NativeAntagLoader? _native;
    private ISawmill _log = default!;
    private readonly Queue<(EntityUid Owner, NativeAntagEventType Type, EntityUid? Target)> _deferredEvents = new();
    private int _commandExecutionDepth;

    public bool NativeAvailable => _native?.Available == true;
    public string? NativeFailure => _native?.Failure;

    public bool SupportsModule(string module, uint abiVersion)
        => NativeAvailable && module == NativeAntagLoader.ModuleId && abiVersion == NativeAntagAbi.Version;

    public bool TryQueryCounter(EntityUid uid, uint token, out int value)
    {
        value = 0;
        if (!TryComp<NativeAntagComponent>(uid, out var component))
            return false;

        var reported = false;
        var observed = 0;
        Dispatch((uid, component), NativeAntagEventType.ObjectiveQuery, input: token, commandObserver: command =>
        {
            if ((NativeAntagCommandType) command.Type != NativeAntagCommandType.ReportCounter || command.Token != token)
                return;
            observed = command.Value0;
            reported = true;
        });
        value = observed;
        return reported;
    }

    public override void Initialize()
    {
        base.Initialize();
        _log = _logManager.GetSawmill("native.antag.bridge");
        _native = new NativeAntagLoader(_logManager);

        SubscribeLocalEvent<NativeAntagComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NativeAntagComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NativeAntagComponent, NativeAntagTargetActionEvent>(OnTargetAction);
        SubscribeLocalEvent<NativeAntagComponent, NativeAntagInstantActionEvent>(OnInstantAction);
        SubscribeLocalEvent<NativeAntagComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<NativeAntagComponent, DamageDealtEvent>(OnDamageTaken);
        SubscribeLocalEvent<NativeAntagComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<NativeAntagComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NativeAntagComponent, EntitySpokeEvent>(OnSpoke);
        SubscribeLocalEvent<NativeAntagPatientComponent, EntitySpokeEvent>(OnPatientSpoke);
        SubscribeLocalEvent<NativeAntagPatientComponent, ComponentShutdown>(OnPatientShutdown);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Shutdown()
    {
        _native?.Dispose();
        _native = null;
        base.Shutdown();
    }

    private void OnStartup(Entity<NativeAntagComponent> ent, ref ComponentStartup args)
    {
        if (!SupportsModule(ent.Comp.Module, NativeAntagAbi.Version))
        {
            _log.Error($"Cannot initialize native scenario '{ent.Comp.Module}' for {ToPrettyString(ent)}: " +
                       (_native?.Available == true ? "unknown module" : _native?.Failure));
            return;
        }

        ent.Comp.Handle = _native!.Create(ToHandle(ent.Owner));
        if (ent.Comp.Handle == 0)
        {
            _log.Error($"Native scenario '{ent.Comp.Module}' exhausted instance storage for {ToPrettyString(ent)}");
            return;
        }

        ent.Comp.RequiredToolTokenCache = ent.Comp.RequiredTools.Keys.OrderBy(token => token).ToArray();
        Dispatch(ent, NativeAntagEventType.Spawn);
    }

    private void OnShutdown(Entity<NativeAntagComponent> ent, ref ComponentShutdown args)
    {
        foreach (var action in ent.Comp.ActionEntities.Values)
            _actions.RemoveAction(ent.Owner, action);
        ent.Comp.ActionEntities.Clear();
        StopAudio(ent.Comp, 0);
        foreach (var stream in ent.Comp.PatientSpeechStreams.Values)
            _audio.Stop(stream);
        ent.Comp.PatientSpeechStreams.Clear();

        if (ent.Comp.Handle != 0)
            _native?.Destroy(ent.Comp.Handle);
        ent.Comp.Handle = 0;
        ent.Comp.RoutedTarget = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_native?.Available != true)
            return;

        var query = EntityQueryEnumerator<NativeAntagComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.RoutedTarget is { } staleTarget && !Valid(staleTarget))
            {
                Dispatch((uid, component), NativeAntagEventType.EntityDeleted, staleTarget);
                component.RoutedTarget = null;
            }

            var target = Valid(component.RoutedTarget) ? component.RoutedTarget : null;
            Dispatch((uid, component), NativeAntagEventType.Update, target, value0: frameTime);
        }
    }

    private void OnTargetAction(Entity<NativeAntagComponent> ent, ref NativeAntagTargetActionEvent args)
    {
        if (args.Handled || !Valid(args.Target))
            return;

        ent.Comp.RoutedTarget = args.Target;
        args.Handled = Dispatch(ent, (NativeAntagEventType) args.EventType, args.Target) > 0;
    }

    private void OnInstantAction(Entity<NativeAntagComponent> ent, ref NativeAntagInstantActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = Dispatch(ent, (NativeAntagEventType) args.EventType) > 0;
    }

    private void OnMobStateChanged(Entity<NativeAntagComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            Dispatch(ent, NativeAntagEventType.Died);
    }

    private void OnDamageTaken(Entity<NativeAntagComponent> ent, ref DamageDealtEvent args)
    {
        if (!args.InterruptsDoAfters)
            return;

        var appliedDamage = args.ModifiedDamage.GetTotal();
        if (appliedDamage <= FixedPoint2.Zero)
            return;

        _log.Info($"Procedure interrupted by {appliedDamage} applied damage to {ToPrettyString(ent.Owner)} " +
                  $"from {(args.Origin is { } origin ? ToPrettyString(origin) : "<environment>")}");
        Dispatch(ent, NativeAntagEventType.ProcedureInterrupted);
    }

    private void OnPlayerDetached(Entity<NativeAntagComponent> ent, ref PlayerDetachedEvent args)
        => Dispatch(ent, NativeAntagEventType.Disconnected);

    private void OnPlayerAttached(Entity<NativeAntagComponent> ent, ref PlayerAttachedEvent args)
        => Dispatch(ent, NativeAntagEventType.PlayerAttached);

    private void OnSpoke(Entity<NativeAntagComponent> ent, ref EntitySpokeEvent args)
    {
        if (args.Source == ent.Owner)
            Dispatch(ent, NativeAntagEventType.Spoke);
    }

    private void OnPatientSpoke(Entity<NativeAntagPatientComponent> ent, ref EntitySpokeEvent args)
    {
        if (args.Source != ent.Owner ||
            ent.Comp.SpeechSoundToken == 0 ||
            !Valid(ent.Comp.Master) ||
            !TryComp<NativeAntagComponent>(ent.Comp.Master, out var owner))
            return;

        PlayPatientSpeech((ent.Comp.Master, owner), ent.Owner, ent.Comp.SpeechSoundToken);
    }

    private void OnPatientShutdown(Entity<NativeAntagPatientComponent> ent, ref ComponentShutdown args)
    {
        foreach (var action in ent.Comp.ActionEntities)
            _actions.RemoveAction(ent.Owner, action);
        ent.Comp.ActionEntities.Clear();

        if (!Valid(ent.Comp.Master) || !TryComp<NativeAntagComponent>(ent.Comp.Master, out var owner))
            return;

        StopPatientSpeech(owner, ent.Owner);

        if (_commandExecutionDepth > 0)
        {
            _deferredEvents.Enqueue((ent.Comp.Master, NativeAntagEventType.PatientRemoved, ent.Owner));
            return;
        }

        Dispatch((ent.Comp.Master, owner), NativeAntagEventType.PatientRemoved, ent.Owner);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        var query = EntityQueryEnumerator<NativeAntagComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            Dispatch((uid, component), NativeAntagEventType.RoundEnded);
            component.Handle = 0;
            component.RoutedTarget = null;
        }

        _deferredEvents.Clear();

        if (_native?.Available == true && !_native.Reset())
            _log.Error("Native antagonist module failed to reset during round cleanup");
    }

    private int Dispatch(
        Entity<NativeAntagComponent> ent,
        NativeAntagEventType type,
        EntityUid? target = null,
        uint? input = null,
        float value0 = 0f,
        NativeAntagFlags flags = 0,
        Action<NativeAntagCommand>? commandObserver = null)
    {
        if (_native?.Available != true || ent.Comp.Handle == 0)
            return 0;

        var toolMask = GetActiveToolMask(ent.Owner, ent.Comp);
        var activeItem = GetActiveItemHandle(ent.Owner);
        var selfPosition = Transform(ent).Coordinates.Position;
        var targetPosition = target is { } coordinateTarget && Valid(coordinateTarget)
            ? Transform(coordinateTarget).Coordinates.Position
            : Vector2.Zero;
        var nativeEvent = new NativeAntagEvent
        {
            Type = (uint) type,
            Flags = (uint) (flags | BuildFlags(ent.Owner, target, toolMask)),
            Handle = ent.Comp.Handle,
            ServerTick = (ulong) Math.Max(0, _timing.CurTime.TotalMilliseconds),
            Self = ToHandle(ent.Owner),
            Target = target is { } targetUid ? ToHandle(targetUid) : 0,
            Input = input ?? toolMask,
            Value0 = value0,
            SelfX = selfPosition.X,
            SelfY = selfPosition.Y,
            TargetX = targetPosition.X,
            TargetY = targetPosition.Y,
            Random = ((uint) _random.Next() << 8) | (GetRandomToolToken(ent.Comp) & byte.MaxValue),
            ActiveItem = activeItem,
        };

        if (type == NativeAntagEventType.ProcedureAction)
        {
            var activePrototype = _hands.TryGetActiveItem(ent.Owner, out var held)
                ? MetaData(held.Value).EntityPrototype?.ID ?? "<sem protótipo>"
                : "<mão ativa vazia>";
            var snapshotFlags = (NativeAntagFlags) nativeEvent.Flags;
            var targetState = (snapshotFlags & NativeAntagFlags.TargetDead) != 0
                ? "Dead"
                : (snapshotFlags & NativeAntagFlags.TargetAlive) != 0
                    ? "Alive"
                    : "<sem estado vivo/morto>";
            _log.Info($"Procedure snapshot owner={ToPrettyString(ent.Owner)} target=" +
                      $"{(target is { } displayTarget ? ToPrettyString(displayTarget) : "<nenhum>")} " +
                      $"state={targetState} active={activePrototype}/{activeItem} toolMask=0x{toolMask:X8} " +
                      $"flags={(NativeAntagFlags) nativeEvent.Flags} distance=" +
                      $"{Vector2.Distance(selfPosition, targetPosition):F2}");
        }

        var commands = ArrayPool<NativeAntagCommand>.Shared.Rent(NativeAntagAbi.CommandCapacity);
        try
        {
            var count = _native.Dispatch(ref nativeEvent, commands, NativeAntagAbi.CommandCapacity);
            var previousSucceeded = true;
            _commandExecutionDepth++;
            try
            {
                for (var i = 0; i < count; i++)
                {
                    var command = commands[i];
                    if (((NativeAntagCommandFlags) command.Flags & NativeAntagCommandFlags.RequirePreviousSuccess) != 0 &&
                        !previousSucceeded)
                        continue;

                    previousSucceeded = Execute(ent, command);
                    if ((NativeAntagCommandType) command.Type == NativeAntagCommandType.Popup &&
                        command.Token is >= 1 and <= 15 &&
                        type is NativeAntagEventType.ProcedureAction or
                            NativeAntagEventType.Update or
                            NativeAntagEventType.ProcedureInterrupted)
                    {
                        _log.Info($"Procedure result event={type} popupToken={command.Token} " +
                                  $"executed={previousSucceeded} owner={ToPrettyString(ent.Owner)}");
                    }
                    commandObserver?.Invoke(command);
                }
            }
            finally
            {
                _commandExecutionDepth--;
            }

            DrainDeferredEvents();
            return count;
        }
        finally
        {
            ArrayPool<NativeAntagCommand>.Shared.Return(commands);
        }
    }

    private void DrainDeferredEvents()
    {
        while (_commandExecutionDepth == 0 && _deferredEvents.TryDequeue(out var pending))
        {
            if (!Valid(pending.Owner) || !TryComp<NativeAntagComponent>(pending.Owner, out var component))
                continue;

            Dispatch((pending.Owner, component), pending.Type, pending.Target);
        }
    }

    private NativeAntagFlags BuildFlags(EntityUid self, EntityUid? target, uint toolMask)
    {
        var flags = NativeAntagFlags.None;
        if (HasComp<ActorComponent>(self))
            flags |= NativeAntagFlags.SelfHasSession;
        if (toolMask != 0)
            flags |= NativeAntagFlags.RequiredToolHeld;

        if (target is not { } targetUid || !Valid(targetUid))
            return flags;

        flags |= NativeAntagFlags.TargetValid;
        if (HasComp<HumanoidProfileComponent>(targetUid))
            flags |= NativeAntagFlags.TargetHumanoid;
        if (HasComp<NativeAntagComponent>(targetUid) || HasComp<ZombieImmuneComponent>(targetUid))
            flags |= NativeAntagFlags.TargetProtected;
        if (TryComp<NativeAntagPatientComponent>(targetUid, out var patient))
        {
            flags |= NativeAntagFlags.TargetConverted;
            if (patient.Master == self)
                flags |= NativeAntagFlags.TargetOwnPatient;
        }
        if (HasComp<ActorComponent>(targetUid))
            flags |= NativeAntagFlags.TargetHasSession;
        if (TryComp<MobStateComponent>(targetUid, out var mobState))
        {
            flags |= mobState.CurrentState == MobState.Dead
                ? NativeAntagFlags.TargetDead
                : NativeAntagFlags.TargetAlive;
            if (_mobState.HasState(targetUid, MobState.Dead, mobState))
                flags |= NativeAntagFlags.TargetCanDie;
        }
        if (_transform.InRange(Transform(self).Coordinates, Transform(targetUid).Coordinates, MeleeRange))
            flags |= NativeAntagFlags.TargetInMeleeRange;

        return flags;
    }

    private uint GetActiveToolMask(EntityUid self, NativeAntagComponent component)
    {
        if (!_hands.TryGetActiveItem(self, out var held) ||
            MetaData(held.Value).EntityPrototype?.ID is not { } prototype)
            return 0;

        foreach (var (token, tool) in component.RequiredTools)
        {
            if (tool == prototype && token is > 0 and <= 32)
                return 1u << ((int) token - 1);
        }

        return 0;
    }

    private uint GetActiveItemHandle(EntityUid self)
        => _hands.TryGetActiveItem(self, out var held) ? (uint) held.Value.Id : 0;

    private uint GetRandomToolToken(NativeAntagComponent component)
    {
        var tokens = component.RequiredToolTokenCache;
        return tokens.Length == 0 ? 0 : _random.Pick(tokens);
    }

    private bool Execute(Entity<NativeAntagComponent> owner, NativeAntagCommand command)
    {
        if (!TryEntity(command.Source, out var source))
        {
            _log.Warning($"Rejected native command type={command.Type}: invalid source handle {command.Source} for {ToPrettyString(owner)}");
            return false;
        }

        TryEntity(command.Target, out var target);
        if (source != owner.Owner && target != owner.Owner)
        {
            _log.Warning($"Rejected native command type={command.Type}: command is unrelated to {ToPrettyString(owner)}");
            return false;
        }

        switch ((NativeAntagCommandType) command.Type)
        {
            case NativeAntagCommandType.AddAction:
                return AddAction(owner, command.Token);
            case NativeAntagCommandType.SetActionCooldown:
                if (owner.Comp.ActionEntities.TryGetValue(command.Token, out var action) && Valid(action))
                {
                    _actions.SetCooldown(action, TimeSpan.FromMilliseconds(command.Value0));
                    return true;
                }
                return false;
            case NativeAntagCommandType.SetMobState when Valid(target):
                var desiredState = (MobState) command.Value0;
                if (!TryComp<MobStateComponent>(target, out var state) || !_mobState.HasState(target, desiredState, state))
                    return false;
                return SetMobState(source, target, desiredState, state);
            case NativeAntagCommandType.AddComponentBundle when Valid(target):
                return ApplyComponentBundle(owner.Comp, target, command.Token, add: true);
            case NativeAntagCommandType.RemoveComponentBundle when Valid(target):
                return ApplyComponentBundle(owner.Comp, target, command.Token, add: false);
            case NativeAntagCommandType.ZombifyEntity when Valid(target):
                if (command.Token != 0)
                {
                    if (!owner.Comp.ZombieProfiles.TryGetValue(command.Token, out var profile) ||
                        !_prototypes.TryIndex(profile, out var profilePrototype) ||
                        !profilePrototype.Components.TryGetValue(
                            EntityManager.ComponentFactory.GetComponentName<ZombieComponent>(),
                            out var zombieEntry))
                        return false;

                    var zombie = (ZombieComponent) EntityManager.ComponentFactory.GetComponent(zombieEntry);
                    if (((NativeAntagCommandFlags) command.Flags & NativeAntagCommandFlags.PreserveVisualSkin) != 0 &&
                        _humanoidProfile.GetSkinColor(_humanoidProfile.GetOrgansData(target)) is { } skinColor)
                        zombie.SkinColor = skinColor;
                    _zombies.ZombifyEntity(target, zombieComponentOverride: zombie);
                    return HasComp<ZombieComponent>(target);
                }

                _zombies.ZombifyEntity(target);
                return true;
            case NativeAntagCommandType.UnzombifyEntity when Valid(target):
                return _zombies.UnZombify(target, target, null);
            case NativeAntagCommandType.RejuvenateEntity when Valid(target):
                _rejuvenate.PerformRejuvenate(target);
                return true;
            case NativeAntagCommandType.Popup:
                if (owner.Comp.Popups.TryGetValue(command.Token, out var loc))
                {
                    _popup.PopupEntity(Loc.GetString(loc), source, source, PopupType.Medium);
                    return true;
                }
                return false;
            case NativeAntagCommandType.SetFaction when Valid(target):
                return SetFaction(owner.Comp, target, command.Token);
            case NativeAntagCommandType.SetNativeOwner when Valid(target):
                if (TryComp<NativeAntagPatientComponent>(target, out var relation))
                {
                    relation.Master = source;
                    return true;
                }
                return false;
            case NativeAntagCommandType.ReportCounter:
                return true;
            case NativeAntagCommandType.ClearRoutedTarget:
                owner.Comp.RoutedTarget = null;
                return true;
            case NativeAntagCommandType.PlaySound:
                return PlayAudio(owner.Comp, source, command);
            case NativeAntagCommandType.StopSound:
                return StopAudio(owner.Comp, command.Token);
            case NativeAntagCommandType.NotifyEvent when Valid(target):
                if (!Enum.IsDefined((NativeAntagEventType) command.Token))
                    return false;
                _deferredEvents.Enqueue((owner.Owner, (NativeAntagEventType) command.Token, target));
                return true;
        }

        return false;
    }

    private bool SetMobState(EntityUid source, EntityUid target, MobState desiredState, MobStateComponent state)
    {
        if (desiredState != MobState.Dead ||
            !TryComp<DamageableComponent>(target, out var damageable) ||
            !_mobThresholds.TryGetThresholdForState(target, MobState.Dead, out var deadThreshold))
        {
            _mobState.ChangeMobState(target, desiredState, state, source);
            return state.CurrentState == desiredState;
        }

        // A bare MobState transition is temporary: the next damage/organ update
        // recalculates a healthy victim back to Alive. Back a requested death
        // with lethal vital damage so the ordinary death pipeline and corpse
        // state remain authoritative after the native command returns.
        var currentDamage = _mobThresholds.CheckVitalDamage((target, damageable));
        var lethalDamage = deadThreshold.Value - currentDamage + FixedPoint2.New(1);
        if (lethalDamage > FixedPoint2.Zero)
        {
            var damage = new DamageSpecifier();
            damage.DamageDict.Add("Cellular", lethalDamage);
            _damageable.TryChangeDamage(
                target,
                damage,
                ignoreResistances: true,
                origin: source,
                ignoreGlobalModifiers: true,
                targetPart: TargetBodyPart.Vital,
                ignoreBlockers: true,
                splitDamage: SplitDamageBehavior.SplitEnsureAll,
                canMiss: false);
        }

        // Keep support for unusual mobs whose damage pipeline does not emit a
        // threshold transition even though they advertise a Dead state.
        if (state.CurrentState != MobState.Dead)
            _mobState.ChangeMobState(target, MobState.Dead, state, source);

        return state.CurrentState == MobState.Dead;
    }

    private bool PlayAudio(NativeAntagComponent component, EntityUid source, NativeAntagCommand command)
    {
        if (!component.Sounds.TryGetValue(command.Token, out var sound))
            return false;

        StopAudio(component, command.Token);
        var parameters = sound.Params.WithPlayOffset(Math.Max(0, command.Value1));
        if (command.Value2 > 0f)
            parameters = parameters.WithMaxDistance(command.Value2);
        var sourceTransform = Transform(source);
        if (sourceTransform.MapUid is not { } mapUid)
            return false;

        // Attaching a broadcast stream directly to the operative still makes
        // remote clients depend on receiving that hidden entity through PVS.
        // Anchor the positional stream to the globally-known map instead, at
        // the operative's current world position, so every recipient can hear
        // it without networking the operative herself.
        var coordinates = new EntityCoordinates(mapUid, _transform.GetWorldPosition(sourceTransform));
        var stream = _audio.PlayStatic(sound, Filter.Broadcast(), coordinates, true, parameters)?.Entity;
        if (stream is not { } streamUid)
            return false;
        component.AudioStreams[command.Token] = streamUid;
        return true;
    }

    private bool StopAudio(NativeAntagComponent component, uint token)
    {
        if (token == 0)
        {
            foreach (var stream in component.AudioStreams.Values)
                _audio.Stop(stream);
            component.AudioStreams.Clear();
            return true;
        }

        if (!component.AudioStreams.Remove(token, out var active))
            return true;
        _audio.Stop(active);
        return true;
    }

    private bool PlayPatientSpeech(
        Entity<NativeAntagComponent> owner,
        EntityUid patient,
        uint soundToken)
    {
        if (!owner.Comp.Sounds.TryGetValue(soundToken, out var sound) || !Valid(patient))
            return false;

        StopPatientSpeech(owner.Comp, patient);
        var patientTransform = Transform(patient);
        if (patientTransform.MapUid is not { } mapUid)
            return false;

        // Patient speech deliberately uses only the owner's short speech
        // collection. It never dispatches the continuous disclosure-radio
        // command that belongs exclusively to the operative body.
        var coordinates = new EntityCoordinates(mapUid, _transform.GetWorldPosition(patientTransform));
        var stream = _audio.PlayStatic(sound, Filter.Broadcast(), coordinates, true, sound.Params)?.Entity;
        if (stream is not { } streamUid)
            return false;

        owner.Comp.PatientSpeechStreams[patient] = streamUid;
        return true;
    }

    private void StopPatientSpeech(NativeAntagComponent owner, EntityUid patient)
    {
        if (!owner.PatientSpeechStreams.Remove(patient, out var stream))
            return;

        _audio.Stop(stream);
    }

    private bool AddAction(Entity<NativeAntagComponent> owner, uint token)
    {
        if (!owner.Comp.Actions.TryGetValue(token, out var prototype) || owner.Comp.ActionEntities.ContainsKey(token))
            return false;

        EntityUid? action = null;
        _actions.AddAction(owner, ref action, prototype);
        if (action is { } actionUid)
        {
            owner.Comp.ActionEntities[token] = actionUid;
            return true;
        }

        return false;
    }

    private bool ApplyComponentBundle(NativeAntagComponent owner, EntityUid target, uint token, bool add)
    {
        if (!owner.ComponentBundles.TryGetValue(token, out var bundleId) ||
            !_prototypes.TryIndex<EntityPrototype>(bundleId, out var bundle))
            return false;

        if (add)
            EntityManager.AddComponents(target, bundle.Components);
        else
            EntityManager.RemoveComponents(target, bundle.Components);
        return true;
    }

    private bool SetFaction(NativeAntagComponent owner, EntityUid target, uint token)
    {
        if (!owner.Factions.TryGetValue(token, out var faction))
            return false;
        _factions.ClearFactions(target, dirty: false);
        _factions.AddFaction(target, faction);
        return true;
    }

    private bool Valid(EntityUid? uid)
        => uid is { Valid: true } value && !TerminatingOrDeleted(value);

    private static ulong ToHandle(EntityUid uid)
        => (uint) uid.Id;

    private bool TryEntity(ulong handle, out EntityUid uid)
    {
        uid = handle is > 0 and <= int.MaxValue ? new EntityUid((int) handle) : EntityUid.Invalid;
        return Valid(uid);
    }
}
