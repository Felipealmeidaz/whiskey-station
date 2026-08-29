// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whiskey.OperativeHidden;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Timing;

namespace Content.Client.Whiskey.OperativeHidden;

public sealed partial class OperativeHiddenHallucinationSystem : EntitySystem
{
    private const string HandTexture = "/Textures/_Whiskey/OperativeHidden/Hallucinations/hand.png";
    private const string EyeTexture = "/Textures/_Whiskey/OperativeHidden/Hallucinations/eye.png";

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IOverlayManager _overlays = default!;
    [Dependency] private IResourceCache _resources = default!;

    private OperativeHiddenHallucinationOverlay? _activeOverlay;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<OperativeHiddenHallucinationEvent>(OnHallucination);
    }

    public override void Shutdown()
    {
        ClearOverlay();
        base.Shutdown();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_activeOverlay != null && _timing.RealTime >= _activeOverlay.EndsAt)
            ClearOverlay();
    }

    private void OnHallucination(OperativeHiddenHallucinationEvent message)
    {
        ClearOverlay();
        var texture = message.Hallucination switch
        {
            OperativeHiddenHallucinationType.Hand => HandTexture,
            OperativeHiddenHallucinationType.Eye => EyeTexture,
            _ => throw new ArgumentOutOfRangeException(nameof(message.Hallucination)),
        };

        _activeOverlay = new OperativeHiddenHallucinationOverlay(
            _resources,
            texture,
            _timing.RealTime);
        _overlays.AddOverlay(_activeOverlay);
    }

    private void ClearOverlay()
    {
        if (_activeOverlay == null)
            return;

        _overlays.RemoveOverlay(_activeOverlay);
        _activeOverlay = null;
    }
}
