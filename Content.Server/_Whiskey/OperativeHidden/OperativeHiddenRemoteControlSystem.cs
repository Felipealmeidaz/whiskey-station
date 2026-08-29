// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Whiskey.Native;
using Content.Shared.Actions;
using Content.Shared.CombatMode;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Whiskey.OperativeHidden;
using Robust.Shared.Timing;

namespace Content.Server.Whiskey.OperativeHidden;

/// <summary>
/// Lets the Hidden Operative drive a patient through the ordinary mover and
/// interaction relays. No mind, actor, or player session changes body, so an
/// existing patient player remains present and the patient's own hands and
/// held weapons are used for every relayed interaction.
/// </summary>
public sealed partial class OperativeHiddenRemoteControlSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OperativeHiddenRemoteControlComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<OperativeHiddenRemoteControlComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<OperativeHiddenRemoteControlComponent, OperativeHiddenReceptionActionEvent>(OnReception);
        SubscribeLocalEvent<OperativeHiddenRemoteControlComponent, DamageDealtEvent>(OnDamageTaken);
        SubscribeLocalEvent<OperativeHiddenRemoteControlComponent, DisarmedEvent>(OnShoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OperativeHiddenRemoteControlComponent>();
        while (query.MoveNext(out var uid, out var remote))
        {
            if (remote.ControlledPatient is not { } patient)
                continue;

            if (!Exists(patient) ||
                TerminatingOrDeleted(patient) ||
                !TryComp<NativeAntagPatientComponent>(patient, out var patientComponent) ||
                patientComponent.SignalLost ||
                _mobState.IsDead(uid) ||
                _mobState.IsDead(patient) ||
                !TryComp<RelayInputMoverComponent>(uid, out var moverRelay) ||
                moverRelay.RelayEntity != patient ||
                !TryComp<InteractionRelayComponent>(uid, out var interactionRelay) ||
                interactionRelay.RelayEntity != patient)
            {
                StopReception((uid, remote), "operative-hidden-reception-return-lost");
            }
        }
    }

    private void OnMapInit(Entity<OperativeHiddenRemoteControlComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.ActionId);
    }

    private void OnShutdown(Entity<OperativeHiddenRemoteControlComponent> ent, ref ComponentShutdown args)
    {
        StopReception(ent, null);
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
        ent.Comp.ActionEntity = null;
    }

    private void OnReception(
        Entity<OperativeHiddenRemoteControlComponent> operative,
        ref OperativeHiddenReceptionActionEvent args)
    {
        if (args.Handled || !CanControl(operative, args.Target))
        {
            if (!args.Handled)
            {
                _popup.PopupEntity(
                    Loc.GetString("operative-hidden-reception-invalid"),
                    operative.Owner,
                    operative.Owner,
                    PopupType.Medium);
            }

            return;
        }

        if (operative.Comp.ControlledPatient is not null)
            StopReception(operative, null);

        if (HasComp<RelayInputMoverComponent>(operative.Owner) ||
            HasComp<InteractionRelayComponent>(operative.Owner) ||
            HasComp<MovementRelayTargetComponent>(args.Target))
        {
            _popup.PopupEntity(
                Loc.GetString("operative-hidden-reception-invalid"),
                operative.Owner,
                operative.Owner,
                PopupType.Medium);
            return;
        }

        operative.Comp.ControlledPatient = args.Target;
        if (TryComp<EyeComponent>(operative.Owner, out var eye))
        {
            operative.Comp.PreviousEyeTarget = eye.Target;
            _eye.SetTarget(operative.Owner, args.Target, eye);
        }

        _mover.SetRelay(operative.Owner, args.Target);
        var interactionRelay = EnsureComp<InteractionRelayComponent>(operative.Owner);
        _interaction.SetRelay(operative.Owner, args.Target, interactionRelay);

        _popup.PopupEntity(
            Loc.GetString("operative-hidden-reception-start", ("target", args.Target)),
            operative.Owner,
            operative.Owner,
            PopupType.LargeCaution);
        _popup.PopupEntity(
            Loc.GetString("operative-hidden-reception-patient-controlled"),
            args.Target,
            args.Target,
            PopupType.LargeCaution);
        args.Handled = true;
    }

    private bool CanControl(Entity<OperativeHiddenRemoteControlComponent> operative, EntityUid target)
    {
        return target != operative.Owner &&
               Exists(target) &&
               !TerminatingOrDeleted(target) &&
               TryComp<NativeAntagPatientComponent>(target, out var patient) &&
               !patient.SignalLost &&
               !_mobState.IsDead(operative.Owner) &&
               !_mobState.IsDead(target);
    }

    private void OnDamageTaken(
        Entity<OperativeHiddenRemoteControlComponent> operative,
        ref DamageDealtEvent args)
    {
        var damage = args.ModifiedDamage.GetTotal();
        if (damage <= FixedPoint2.Zero)
            return;

        if (operative.Comp.ControlledPatient is not null)
            StopReception(operative, "operative-hidden-reception-return-damage");

        var now = _timing.CurTime;
        if (now - operative.Comp.DamageWindowStarted > operative.Comp.SignalLossDamageWindow)
        {
            operative.Comp.DamageWindowStarted = now;
            operative.Comp.DamageInWindow = 0f;
        }

        operative.Comp.DamageInWindow += damage.Float();
        if (operative.Comp.DamageInWindow < operative.Comp.SignalLossDamageThreshold)
            return;

        operative.Comp.DamageWindowStarted = now;
        operative.Comp.DamageInWindow = 0f;
        DropPatientSignals(operative, now);
    }

    private void DropPatientSignals(Entity<OperativeHiddenRemoteControlComponent> operative, TimeSpan now)
    {
        var query = EntityQueryEnumerator<NativeAntagPatientComponent>();
        while (query.MoveNext(out var uid, out var patient))
        {
            if (patient.Master != operative.Owner || _mobState.IsDead(uid))
                continue;

            patient.SignalLost = true;
            patient.SignalLostUntil = now + operative.Comp.SignalLossDuration;
            if (TryComp<OperativeHiddenPuppetVisualsComponent>(uid, out var visuals))
            {
                visuals.State = OperativeHiddenPuppetVisualState.Reconnect;
                Dirty(uid, visuals);
            }
            _stun.TryKnockdown(uid, operative.Comp.SignalLossDuration, force: true);
            _popup.PopupEntity(
                Loc.GetString("operative-hidden-patient-signal-lost"),
                uid,
                uid,
                PopupType.LargeCaution);
        }
    }

    private void OnShoved(Entity<OperativeHiddenRemoteControlComponent> operative, ref DisarmedEvent args)
    {
        if (operative.Comp.ControlledPatient is not null)
            StopReception(operative, "operative-hidden-reception-return-shove");
    }

    private void StopReception(
        Entity<OperativeHiddenRemoteControlComponent> operative,
        string? popup)
    {
        if (operative.Comp.ControlledPatient is not { } patient)
            return;

        if (TryComp<RelayInputMoverComponent>(operative.Owner, out var moverRelay) &&
            moverRelay.RelayEntity == patient)
        {
            RemComp<RelayInputMoverComponent>(operative.Owner);
        }

        if (TryComp<InteractionRelayComponent>(operative.Owner, out var interactionRelay) &&
            interactionRelay.RelayEntity == patient)
        {
            RemComp<InteractionRelayComponent>(operative.Owner);
        }

        if (TryComp<EyeComponent>(operative.Owner, out var eye))
            _eye.SetTarget(operative.Owner, operative.Comp.PreviousEyeTarget, eye);

        operative.Comp.ControlledPatient = null;
        operative.Comp.PreviousEyeTarget = null;

        if (popup is not null && Exists(operative.Owner) && !TerminatingOrDeleted(operative.Owner))
        {
            _popup.PopupEntity(
                Loc.GetString(popup),
                operative.Owner,
                operative.Owner,
                PopupType.LargeCaution);
        }
    }
}
