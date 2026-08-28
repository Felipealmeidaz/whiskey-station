namespace Content.Server.Antag.Components;

public sealed partial class AntagSpawnerComponent
{
    /// <summary>
    /// Transfers the selected session's existing mind to the spawned body
    /// before antagonist initialization. This prevents body-replacement roles
    /// from manufacturing a second mind and orphaning the original one.
    /// </summary>
    [DataField]
    public bool PreserveMind;
}
