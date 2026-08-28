using Content.Server.Pinpointer;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Whiskey.Native;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Whiskey.Native;

/// <summary>
/// Owns the data-driven movement and tracking abilities granted exclusively to
/// patients created by the Hidden Operative scenario.
/// </summary>
public sealed partial class NativeAntagPatientSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NativeAntagPatientComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NativeAntagPatientComponent, OperativeHiddenPatientJumpActionEvent>(OnJump);
        SubscribeLocalEvent<NativeAntagPatientComponent, OperativeHiddenPatientFlairActionEvent>(OnFlair);
        SubscribeLocalEvent<NativeAntagPatientComponent, ThrowDoHitEvent>(OnThrowDoHit);
    }

    private void OnInit(Entity<NativeAntagPatientComponent> ent, ref ComponentInit args)
    {
        AddAction(ent, ent.Comp.ActionJumpId);
        AddAction(ent, ent.Comp.ActionFlairId);
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

    private void OnJump(Entity<NativeAntagPatientComponent> ent, ref OperativeHiddenPatientJumpActionEvent args)
    {
        if (args.Handled || ent.Comp.ActionJumpId is null || _mobState.IsDead(ent.Owner))
            return;

        var mapTarget = _transform.ToMapCoordinates(args.Target);
        var direction = mapTarget.Position - _transform.GetMapCoordinates(ent.Owner).Position;
        if (direction.Length() > ent.Comp.MaxThrow)
            direction = direction.Normalized() * ent.Comp.MaxThrow;

        args.Handled = _throwing.TryThrow(ent.Owner, direction, 7f, ent.Owner, 10f);
        if (args.Handled)
            _chat.TryEmoteWithChat(ent.Owner, "ZombieGroan");
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
            !_mobState.IsAlive(args.Target))
            return;

        _stun.TryAddParalyzeDuration(args.Target, ent.Comp.ParalyzeTime);
        _damageable.TryChangeDamage(args.Target, ent.Comp.ThrowDamage, origin: args.Thrown);
    }
}
