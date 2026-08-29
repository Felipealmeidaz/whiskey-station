using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Pinpointer;
using Content.Shared._ES.Camera;
using Content.Shared.Actions;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Whiskey.Native;
using Content.Shared.Whiskey.OperativeHidden;
using Content.Shared.Zombies;
using Content.Trauma.Common.Weapons;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Whiskey.Native;

/// <summary>
/// Owns the data-driven movement and tracking abilities granted exclusively to
/// patients created by the Hidden Operative scenario.
/// </summary>
public sealed partial class NativeAntagPatientSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private ESScreenshakeSystem _screenshake = default!;
    [Dependency] private SharedJitteringSystem _jitter = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NativeAntagPatientComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NativeAntagPatientComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<NativeAntagPatientComponent, OperativeHiddenPatientJumpActionEvent>(OnJump);
        SubscribeLocalEvent<NativeAntagPatientComponent, OperativeHiddenPatientFlairActionEvent>(OnFlair);
        SubscribeLocalEvent<NativeAntagPatientComponent, ThrowDoHitEvent>(OnThrowDoHit);
        SubscribeLocalEvent<NativeAntagPatientComponent, BeforeHarmfulActionEvent>(OnPatientHarmfulAction);
        SubscribeLocalEvent<NativeAntagComponent, BeforeHarmfulActionEvent>(OnMasterHarmfulAction);
        SubscribeLocalEvent<NativeAntagPatientComponent, BeforeDamageChangedEvent>(OnPatientBeforeDamage);
        SubscribeLocalEvent<NativeAntagComponent, BeforeDamageChangedEvent>(OnMasterBeforeDamage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NativeAntagPatientComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var patient, out var patientTransform))
        {
            if (patient.SignalLost && _timing.CurTime >= patient.SignalLostUntil)
            {
                patient.SignalLost = false;
                _popup.PopupEntity(
                    Loc.GetString("operative-hidden-patient-signal-restored"),
                    uid,
                    uid,
                    PopupType.LargeCaution);
            }

            var visualState = patient.SignalLost
                ? OperativeHiddenPuppetVisualState.Reconnect
                : OperativeHiddenPuppetVisualState.Linked;

            if (!Exists(patient.Master) || TerminatingOrDeleted(patient.Master))
            {
                SetVisualState(uid, visualState);
                continue;
            }

            var patientPosition = _transform.GetMapCoordinates((uid, patientTransform));
            var masterPosition = _transform.GetMapCoordinates(patient.Master);
            if (patientPosition.MapId != masterPosition.MapId)
            {
                SetVisualState(
                    uid,
                    patient.SignalLost ? visualState : OperativeHiddenPuppetVisualState.Range);
                PunishBrokenLeash((uid, patient));
                _transform.SetCoordinates(uid, Transform(patient.Master).Coordinates);
                continue;
            }

            var returnVector = masterPosition.Position - patientPosition.Position;
            var distance = returnVector.Length();
            if (distance <= patient.MaxMasterDistance || distance <= 0f)
            {
                SetVisualState(uid, visualState);
                continue;
            }

            SetVisualState(
                uid,
                patient.SignalLost ? visualState : OperativeHiddenPuppetVisualState.Range);
            PunishBrokenLeash((uid, patient));

            // The receiver does not teleport or damage its victim. It seizes
            // the motor pathways and violently forces the body back inside the
            // ten-tile leash in short, visible convulsions.
            var correction = MathF.Min(distance - patient.MaxMasterDistance + 0.5f, 3f);
            _throwing.TryThrow(
                uid,
                returnVector.Normalized() * correction,
                7f,
                patient.Master,
                0f);
        }
    }

    private void OnInit(Entity<NativeAntagPatientComponent> ent, ref ComponentInit args)
    {
        AddAction(ent, ent.Comp.ActionJumpId);
        AddAction(ent, ent.Comp.ActionFlairId);

        // Conversion adds this component to a body whose player is already
        // attached, so do not wait for a later reconnect to disclose the
        // receiver and play its replacement vocalization.
        if (TryComp<ActorComponent>(ent.Owner, out var actor))
            NotifyReception(ent, actor.PlayerSession);
    }

    private void OnPlayerAttached(Entity<NativeAntagPatientComponent> ent, ref PlayerAttachedEvent args)
        => NotifyReception(ent, args.Player);

    private void NotifyReception(Entity<NativeAntagPatientComponent> ent, ICommonSession session)
    {
        var message = Loc.GetString("operative-hidden-patient-reception-message");
        _popup.PopupEntity(message, ent.Owner, ent.Owner, PopupType.LargeCaution);
        _chatManager.DispatchServerMessage(session, message);
        _audio.PlayPvs(ent.Comp.ReceptionSound, ent.Owner);
    }

    private void AddAction(Entity<NativeAntagPatientComponent> ent, EntProtoId? prototype)
    {
        if (prototype is not { } actionPrototype)
            return;

        EntityUid? action = null;
        _actions.AddAction(ent.Owner, ref action, actionPrototype);
        if (action is { } actionUid)
            ent.Comp.ActionEntities.Add(actionUid);
    }

    private void SetVisualState(EntityUid uid, OperativeHiddenPuppetVisualState state)
    {
        if (!TryComp<OperativeHiddenPuppetVisualsComponent>(uid, out var visuals) ||
            visuals.State == state)
        {
            return;
        }

        visuals.State = state;
        Dirty(uid, visuals);
    }

    private void OnJump(Entity<NativeAntagPatientComponent> ent, ref OperativeHiddenPatientJumpActionEvent args)
    {
        if (args.Handled || ent.Comp.ActionJumpId is null || _mobState.IsDead(ent.Owner))
            return;

        var mapTarget = _transform.ToMapCoordinates(args.Target);
        var direction = mapTarget.Position - _transform.GetMapCoordinates(ent.Owner).Position;
        if (direction.Length() > ent.Comp.MaxThrow)
            direction = direction.Normalized() * ent.Comp.MaxThrow;

        args.Handled = _throwing.TryThrow(ent.Owner, direction, 7f, ent.Owner, 10f);
    }

    private void OnFlair(Entity<NativeAntagPatientComponent> ent, ref OperativeHiddenPatientFlairActionEvent args)
    {
        if (args.Handled || ent.Comp.ActionFlairId is null || _mobState.IsDead(ent.Owner))
            return;

        EntityUid? nearest = null;
        var minimumDistance = float.MaxValue;
        var origin = _transform.GetMapCoordinates(ent.Owner);
        var originPosition = origin.Position;
        var query = EntityQueryEnumerator<HumanoidProfileComponent, TransformComponent>();
        while (query.MoveNext(out var target, out _, out var transform))
        {
            if (target == ent.Owner ||
                transform.MapID != origin.MapId ||
                HasComp<ZombieComponent>(target) ||
                HasComp<ZombieImmuneComponent>(target))
                continue;

            var targetPosition = _transform.GetMapCoordinates((target, transform)).Position;
            var distance = Math.Abs(originPosition.X - targetPosition.X) +
                           Math.Abs(originPosition.Y - targetPosition.Y);
            if (distance > ent.Comp.MaxFlairDistance || distance >= minimumDistance)
                continue;

            nearest = target;
            minimumDistance = distance;
        }

        var message = nearest is { } targetUid
            ? Loc.GetString("zombie-flair-location",
                ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(targetUid))))
            : Loc.GetString("zombie-flair-none");
        _popup.PopupEntity(message, ent.Owner, ent.Owner, PopupType.LargeCaution);
        args.Handled = true;
    }

    private void OnThrowDoHit(Entity<NativeAntagPatientComponent> ent, ref ThrowDoHitEvent args)
    {
        if (ent.Comp.ActionJumpId is null ||
            _mobState.IsDead(ent.Owner) ||
            HasComp<ZombieComponent>(args.Target) ||
            HasComp<PendingZombieComponent>(args.Target) ||
            HasComp<NativeAntagComponent>(args.Target) ||
            !_mobState.IsAlive(args.Target))
            return;

        _stun.TryKnockdown(args.Target, ent.Comp.KnockdownTime, force: true);
    }

    private void PunishBrokenLeash(Entity<NativeAntagPatientComponent> patient)
    {
        if (_timing.CurTime < patient.Comp.NextLeashPain)
            return;

        patient.Comp.NextLeashPain = _timing.CurTime + patient.Comp.LeashPainCooldown;
        _popup.PopupEntity(
            Loc.GetString("operative-hidden-patient-leash-pain"),
            patient.Owner,
            patient.Owner,
            PopupType.LargeCaution);
        _chat.TryEmoteWithChat(
            patient.Owner,
            "Scream",
            ignoreActionBlocker: true,
            forceEmote: true);
        _jitter.DoJitter(
            patient.Owner,
            TimeSpan.FromSeconds(2),
            refresh: true,
            amplitude: 16f,
            frequency: 8f,
            forceValueChange: true);
        _screenshake.Screenshake(
            patient.Owner,
            new ESScreenshakeParameters
            {
                Trauma = 0.65f,
                DecayRate = 0.02f,
                Frequency = 0.04f,
            },
            new ESScreenshakeParameters
            {
                Trauma = 0.5f,
                DecayRate = 0.018f,
                Frequency = 0.045f,
            });
        _stun.TryKnockdown(patient.Owner, TimeSpan.FromSeconds(1), force: true);
    }

    private void OnPatientHarmfulAction(
        Entity<NativeAntagPatientComponent> patient,
        ref BeforeHarmfulActionEvent args)
    {
        if (args.User == patient.Owner)
            args.Cancelled = true;
    }

    private void OnMasterHarmfulAction(Entity<NativeAntagComponent> master, ref BeforeHarmfulActionEvent args)
    {
        if (TryComp<NativeAntagPatientComponent>(args.User, out var patient) &&
            patient.Master == master.Owner)
        {
            args.Cancelled = true;
        }
    }

    private void OnPatientBeforeDamage(
        Entity<NativeAntagPatientComponent> patient,
        ref BeforeDamageChangedEvent args)
    {
        if (ResolvePatientSource(args.Origin) == patient.Owner)
            args.Cancelled = true;
    }

    private void OnMasterBeforeDamage(Entity<NativeAntagComponent> master, ref BeforeDamageChangedEvent args)
    {
        if (ResolvePatientSource(args.Origin) is not { } source ||
            !TryComp<NativeAntagPatientComponent>(source, out var patient) ||
            patient.Master != master.Owner)
        {
            return;
        }

        args.Cancelled = true;
    }

    private EntityUid? ResolvePatientSource(EntityUid? origin)
    {
        if (origin is not { } source || !Exists(source))
            return null;

        if (HasComp<NativeAntagPatientComponent>(source))
            return source;

        if (TryComp<ProjectileComponent>(source, out var projectile) &&
            projectile.Shooter is { } shooter &&
            HasComp<NativeAntagPatientComponent>(shooter))
        {
            return shooter;
        }

        if (TryComp<ThrownItemComponent>(source, out var thrown) &&
            thrown.Thrower is { } thrower &&
            HasComp<NativeAntagPatientComponent>(thrower))
        {
            return thrower;
        }

        return null;
    }
}
