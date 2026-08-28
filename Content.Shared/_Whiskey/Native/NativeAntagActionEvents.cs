using Content.Shared.Actions;

namespace Content.Shared.Whiskey.Native;

/// <summary>
/// Generic target action forwarded as a primitive event to a native scenario.
/// </summary>
public sealed partial class NativeAntagTargetActionEvent : EntityTargetActionEvent
{
    [DataField(required: true)]
    public uint EventType;
}

/// <summary>
/// Generic instant action forwarded as a primitive event to a native scenario.
/// </summary>
public sealed partial class NativeAntagInstantActionEvent : InstantActionEvent
{
    [DataField(required: true)]
    public uint EventType;
}
