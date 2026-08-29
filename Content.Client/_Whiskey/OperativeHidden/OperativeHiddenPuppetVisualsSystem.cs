// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whiskey.OperativeHidden;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Whiskey.OperativeHidden;

/// <summary>
/// Adds the receiver above the patient's ordinary humanoid and clothing
/// layers without occupying the head slot.
/// </summary>
public sealed partial class OperativeHiddenPuppetVisualsSystem : EntitySystem
{
    private static readonly ResPath ControllerRsi =
        new("_Whiskey/OperativeHidden/puppet_head_controller.rsi");

    private enum VisualLayers : byte
    {
        HeadController,
    }

    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OperativeHiddenPuppetVisualsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<OperativeHiddenPuppetVisualsComponent, AfterAutoHandleStateEvent>(OnState);
        SubscribeLocalEvent<OperativeHiddenPuppetVisualsComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<OperativeHiddenPuppetVisualsComponent> ent, ref ComponentStartup args)
        => UpdateVisual(ent);

    private void OnState(Entity<OperativeHiddenPuppetVisualsComponent> ent, ref AfterAutoHandleStateEvent args)
        => UpdateVisual(ent);

    private void OnShutdown(Entity<OperativeHiddenPuppetVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent.Owner, out var sprite))
            _sprite.RemoveLayer((ent.Owner, sprite), VisualLayers.HeadController, false);
    }

    private void UpdateVisual(Entity<OperativeHiddenPuppetVisualsComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        var state = ent.Comp.State switch
        {
            OperativeHiddenPuppetVisualState.Intact => "equipped-HEAD",
            OperativeHiddenPuppetVisualState.Range => "equipped-HEAD-range",
            OperativeHiddenPuppetVisualState.Reconnect => "equipped-HEAD-reconnect",
            _ => "equipped-HEAD-linked",
        };

        if (!_sprite.LayerMapTryGet(
                (ent.Owner, sprite),
                VisualLayers.HeadController,
                out var layer,
                false))
        {
            layer = _sprite.AddLayer(
                (ent.Owner, sprite),
                new SpriteSpecifier.Rsi(ControllerRsi, state));
            _sprite.LayerMapSet((ent.Owner, sprite), VisualLayers.HeadController, layer);
            return;
        }

        _sprite.LayerSetRsiState((ent.Owner, sprite), layer, state);
    }
}
