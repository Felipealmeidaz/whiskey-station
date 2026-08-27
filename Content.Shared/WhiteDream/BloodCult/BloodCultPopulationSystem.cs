// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.WhiteDream.BloodCult.Runes;
using Robust.Shared.Player;

namespace Content.Shared.WhiteDream.BloodCult;

/// <summary>
///     Whiskey - the cult sizes itself against the crew, so several places need the same
///     head count: the stage thresholds, the apocalypse rune, the stun decay and the rune drawer.
///     The drawer filter runs on both the client and the server, so the count has to live in shared
///     or the two sides disagree about which runes are available.
/// </summary>
public sealed partial class BloodCultPopulationSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;

    /// <summary>
    ///     Crew currently playing, ignoring the lobby, observers and the dead.
    ///     Trauma keeps the humanoid data on HumanoidProfileComponent, not HumanoidAppearanceComponent.
    ///     ActorComponent is not networked, so this only returns a real number on the server. Callers on
    ///     the client would get zero, which is why the rune drawer filter below is server-gated.
    /// </summary>
    public int GetActivePlayerCount()
    {
        var count = 0;
        var query = EntityQueryEnumerator<ActorComponent, HumanoidProfileComponent, MobStateComponent>();

        while (query.MoveNext(out var uid, out _, out _, out _))
        {
            if (!_mobState.IsDead(uid))
                count++;
        }

        return count;
    }

    /// <summary>
    ///     A rune can ask for a share of the crew instead of a flat count.
    /// </summary>
    public int GetRequiredCultists(RuneSelectorPrototype selector)
    {
        if (selector.RequiredCultistsPercent <= 0f)
            return selector.RequiredTotalCultists;

        return (int) MathF.Ceiling(GetActivePlayerCount() * selector.RequiredCultistsPercent);
    }
}
