// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

using Content.Shared.Antag;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.WhiteDream.BloodCult.BloodCultist;

/// <summary>
///     Whiskey - someone the cult leader has pointed Nar'Sie's attention at. Wears an icon only
///     the cult, its constructs and ghosts can see, and falls off on its own after a while.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultMarkComponent : Component, IAntagStatusIconComponent
{
    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultTarget";

    [DataField]
    public bool IconVisibleToGhost { get; set; } = true;

    /// <summary>
    ///     When the mark wears off. Server-side bookkeeping, the icon itself is networked.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan EndTime;
}
