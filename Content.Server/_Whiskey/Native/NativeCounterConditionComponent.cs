namespace Content.Server.Whiskey.Native;

/// <summary>
/// Generic objective condition backed by a counter owned by a native scenario.
/// </summary>
[RegisterComponent]
public sealed partial class NativeCounterConditionComponent : Component
{
    [DataField(required: true)]
    public uint Token;

    /// <summary>
    /// Authoritative managed mirror of committed native progress. Objective
    /// entities belong to the mind, so this survives ghosting and body deletion.
    /// </summary>
    [ViewVariables]
    public int Current;

    /// <summary>
    /// Entity identities already accepted for this counter. This makes progress
    /// idempotent even if a native notification is duplicated.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> DistinctTargets = [];
}
