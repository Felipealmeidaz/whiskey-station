// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.WhiteDream.BloodCult.Items;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultItemComponent : Component
{
    /// <summary>
    ///     Allow non-cultists to use this item?
    /// </summary>
    [DataField]
    public bool AllowUseToEveryone;

    [DataField]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(2);

    // <Whiskey> - a knockdown on its own was no deterrent at all.
    [DataField]
    public TimeSpan StunDuration = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Deliberately weaker than the runed door, which repulses at 13000.
    /// </summary>
    [DataField]
    public float BacklashForce = 6000f;

    [DataField]
    public DamageSpecifier BacklashDamage = new() { DamageDict = new() { ["Slash"] = 8 } };
    // </Whiskey>
}
