// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Whiskey.OperativeHidden;

/// <summary>
/// Replicates the visible state of the receiver implanted in a conscious
/// reconditioned patient. The client renders it as an additional head layer,
/// so it never consumes an inventory slot or hides held equipment.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class OperativeHiddenPuppetVisualsComponent : Component
{
    [DataField, AutoNetworkedField]
    public OperativeHiddenPuppetVisualState State = OperativeHiddenPuppetVisualState.Linked;
}

[Serializable, NetSerializable]
public enum OperativeHiddenPuppetVisualState : byte
{
    Intact,
    Linked,
    Range,
    Reconnect,
}
