// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Server.Whiskey.OperativeHidden;

/// <summary>
/// Server-owned state for the Hidden Operative's remote patient reception.
/// The operative remains attached to this body while input and interactions
/// are relayed to the selected patient.
/// </summary>
[RegisterComponent]
public sealed partial class OperativeHiddenRemoteControlComponent : Component
{
    [DataField]
    public EntProtoId ActionId = "ActionOperativeHiddenReception";

    [DataField]
    public float SignalLossDamageThreshold = 20f;

    [DataField]
    public TimeSpan SignalLossDamageWindow = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan SignalLossDuration = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public EntityUid? ActionEntity;

    [ViewVariables]
    public EntityUid? ControlledPatient;

    [ViewVariables]
    public EntityUid? PreviousEyeTarget;

    [ViewVariables]
    public TimeSpan DamageWindowStarted;

    [ViewVariables]
    public float DamageInWindow;
}
