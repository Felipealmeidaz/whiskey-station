using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared.Whiskey.Native;

public sealed partial class OperativeHiddenPatientJumpActionEvent : WorldTargetActionEvent;

public sealed partial class OperativeHiddenPatientFlairActionEvent : InstantActionEvent;

/// <summary>
/// Network-visible marker used by data-driven faction icon prototypes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NativeScenarioOwnerIconViewerComponent : Component;

/// <summary>
/// Network-visible marker used by data-driven faction icon prototypes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NativeScenarioPatientIconViewerComponent : Component;
