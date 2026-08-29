// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.FixedPoint;

namespace Content.Server.Whiskey.OperativeHidden;

/// <summary>
/// Server-owned timeline for a victim of the Hidden Operative's triclor dose.
/// </summary>
[RegisterComponent]
public sealed partial class OperativeHiddenTriclorComponent : Component
{
    [ViewVariables]
    public EntityUid Source;

    [ViewVariables]
    public TimeSpan NextVomitAt;

    [ViewVariables]
    public TimeSpan NextScreamAt;

    [ViewVariables]
    public TimeSpan HandHallucinationAt;

    [ViewVariables]
    public TimeSpan EyeHallucinationAt;

    [ViewVariables]
    public TimeSpan DeathAt;

    [ViewVariables]
    public int VomitCount;

    [ViewVariables]
    public bool HandHallucinationSent;

    [ViewVariables]
    public bool EyeHallucinationSent;

    [ViewVariables]
    public bool DeathApplied;

    [ViewVariables]
    public FixedPoint2 OriginalDeathThreshold;
}
