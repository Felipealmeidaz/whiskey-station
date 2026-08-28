namespace Content.Server.Whiskey.Native;

/// <summary>
/// Generic objective condition backed by a counter owned by a native scenario.
/// </summary>
[RegisterComponent]
public sealed partial class NativeCounterConditionComponent : Component
{
    [DataField(required: true)]
    public uint Token;
}
