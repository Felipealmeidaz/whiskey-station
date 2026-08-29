// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whiskey.OperativeHidden;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client.Whiskey.OperativeHidden;

/// <summary>
/// Brief fullscreen hallucination rendered only on the victim's client.
/// </summary>
public sealed partial class OperativeHiddenHallucinationOverlay : Overlay
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(1.25);

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public TimeSpan EndsAt => _startedAt + Duration;

    private readonly Texture _texture;
    private readonly TimeSpan _startedAt;

    public OperativeHiddenHallucinationOverlay(
        IResourceCache resources,
        string texturePath,
        TimeSpan startedAt)
    {
        IoCManager.InjectDependencies(this);
        _texture = resources.GetResource<TextureResource>(texturePath).Texture;
        _startedAt = startedAt;
        ZIndex = 100;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_timing.RealTime >= EndsAt ||
            !_entityManager.TryGetComponent(_player.LocalEntity, out EyeComponent? eye))
        {
            return false;
        }

        return args.Viewport.Eye == eye.Eye;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var elapsed = (float) ((_timing.RealTime - _startedAt) / Duration);
        var alpha = MathF.Sin(Math.Clamp(elapsed, 0f, 1f) * MathF.PI) * 0.95f;
        args.WorldHandle.DrawTextureRect(_texture, args.WorldBounds, Color.White.WithAlpha(alpha));
    }
}
