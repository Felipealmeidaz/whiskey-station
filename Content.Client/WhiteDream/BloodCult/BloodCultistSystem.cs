// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

using System.Numerics;
using Content.Shared.Antag;
using Content.Shared.Ghost;
using Content.Shared.StatusIcon.Components;
using Content.Shared.WhiteDream.BloodCult;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Components;
using Content.Shared.WhiteDream.BloodCult.Constructs;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Ghost.Components;

namespace Content.Client.WhiteDream.BloodCult;

public sealed partial class BloodCultistSystem : EntitySystem
{
    private static readonly ResPath LeaderAuraRsi =
        new("WhiteDream/BloodCult/Effects/leader_aura.rsi");

    private const string LeaderAuraState = "leader_aura";

    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PentagramComponent, ComponentStartup>(OnPentagramAdded);
        SubscribeLocalEvent<PentagramComponent, ComponentShutdown>(OnPentagramRemoved);
        SubscribeLocalEvent<BloodCultLeaderComponent, ComponentStartup>(OnLeaderAdded);
        SubscribeLocalEvent<BloodCultLeaderComponent, ComponentShutdown>(OnLeaderRemoved);

        SubscribeLocalEvent<ConstructComponent, GetStatusIconsEvent>(GetCultStatusIcon);
        SubscribeLocalEvent<BloodCultistComponent, GetStatusIconsEvent>(GetBloodCultistStatusIcon);
        SubscribeLocalEvent<BloodCultLeaderComponent, GetStatusIconsEvent>(GetCultStatusIcon);
        SubscribeLocalEvent<BloodCultMarkComponent, GetStatusIconsEvent>(GetCultStatusIcon); // Whiskey

        SubscribeLocalEvent<ConstructComponent, CanDisplayStatusIconsEvent>(OnCanShowCultIcon);
        SubscribeLocalEvent<BloodCultistComponent, CanDisplayStatusIconsEvent>(OnCanShowCultIcon);
        SubscribeLocalEvent<BloodCultLeaderComponent, CanDisplayStatusIconsEvent>(OnCanShowCultIcon);
        SubscribeLocalEvent<BloodCultMarkComponent, CanDisplayStatusIconsEvent>(OnCanShowCultIcon); // Whiskey
    }

    private void GetCultStatusIcon<T>(Entity<T> ent, ref GetStatusIconsEvent args)
        where T : IComponent, IAntagStatusIconComponent
    {
        var canEv = new CanDisplayStatusIconsEvent(_player.LocalSession?.AttachedEntity);
        RaiseLocalEvent(ent, ref canEv);

        if (canEv.Cancelled || !_prototype.TryIndex(ent.Comp.StatusIcon, out var icon))
            return;

        args.StatusIcons.Add(icon);
    }

    private void GetBloodCultistStatusIcon(Entity<BloodCultistComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<BloodCultLeaderComponent>(ent))
            return;

        GetCultStatusIcon(ent, ref args);
    }

    private void OnPentagramAdded(EntityUid uid, PentagramComponent component, ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!sprite.LayerMapTryGet(PentagramKey.Key, out _))
        {
            var adj = sprite.Bounds.Height / 2 + 1.0f / 32 * 10.0f;

            var randomState = _random.Pick(component.States);

            var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(component.RsiPath, randomState));

            sprite.LayerMapSet(PentagramKey.Key, layer);
            sprite.LayerSetOffset(layer, new Vector2(0.0f, adj));
        }

        if (HasComp<BloodCultLeaderComponent>(uid))
            RefreshLeaderAura(uid);
    }

    private void OnPentagramRemoved(EntityUid uid, PentagramComponent component, ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite) &&
            sprite.LayerMapTryGet(PentagramKey.Key, out var layer))
            sprite.RemoveLayer(layer);

        RemoveLeaderAura(uid);
    }

    private void OnLeaderAdded(EntityUid uid, BloodCultLeaderComponent component, ComponentStartup args)
        => RefreshLeaderAura(uid);

    private void OnLeaderRemoved(EntityUid uid, BloodCultLeaderComponent component, ComponentShutdown args)
        => RemoveLeaderAura(uid);

    private void RefreshLeaderAura(EntityUid uid)
    {
        if (!HasComp<BloodCultLeaderComponent>(uid) || !HasComp<PentagramComponent>(uid))
        {
            RemoveLeaderAura(uid);
            return;
        }

        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            sprite.LayerMapTryGet(BloodCultVisualLayers.LeaderAura, out _))
            return;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(LeaderAuraRsi, LeaderAuraState));
        sprite.LayerMapSet(BloodCultVisualLayers.LeaderAura, layer);
    }

    private void RemoveLeaderAura(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !sprite.LayerMapTryGet(BloodCultVisualLayers.LeaderAura, out var layer))
            return;

        sprite.RemoveLayer(layer);
    }

    /// <summary>
    /// Determine whether a client should display the cult icon.
    /// </summary>
    private void OnCanShowCultIcon<T>(EntityUid uid, T comp, ref CanDisplayStatusIconsEvent args)
        where T : IAntagStatusIconComponent
    {
        if (!CanDisplayIcon(args.User, comp.IconVisibleToGhost))
            args.Cancelled = true;
    }

    /// <summary>
    /// The criteria that determine whether a client should see Cult/Cult leader icons.
    /// </summary>
    private bool CanDisplayIcon(EntityUid? uid, bool visibleToGhost)
    {
        if (HasComp<BloodCultistComponent>(uid) || HasComp<BloodCultLeaderComponent>(uid) ||
            HasComp<ConstructComponent>(uid))
            return true;

        return visibleToGhost && HasComp<GhostComponent>(uid);
    }
}

internal enum BloodCultVisualLayers : byte
{
    LeaderAura,
}
