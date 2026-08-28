// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

using Content.Shared.Damage;
using Robust.Shared.Prototypes;

namespace Content.Shared.WhiteDream.BloodCult.Runes;

[Prototype]
public sealed partial class RuneSelectorPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField]
    public float DrawTime = 4f;

    [DataField]
    public bool RequireTargetDead;

    [DataField]
    public int RequiredTotalCultists = 1;

    /// <summary>
    ///     Whiskey - when above zero the requirement is this share of the crew currently
    ///     playing, and <see cref="RequiredTotalCultists"/> is ignored.
    /// </summary>
    [DataField]
    public float RequiredCultistsPercent;

    /// <summary>
    ///     WhiteDream - only the cult leader may draw this rune.
    /// </summary>
    [DataField]
    public bool RequireLeader;

    /// <summary>
    ///     WhiteDream - this rune cannot be drawn until the veil has been torn.
    /// </summary>
    [DataField]
    public bool RequireVeilWeakened;

    /// <summary>
    ///     Damage dealt on the rune drawing.
    /// </summary>
    [DataField]
    public DamageSpecifier DrawDamage = new() { DamageDict = new() { ["Slash"] = 15 } };
}
