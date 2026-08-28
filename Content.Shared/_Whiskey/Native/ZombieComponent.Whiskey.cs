using Robust.Shared.Localization;

namespace Content.Shared.Zombies;

public sealed partial class ZombieComponent
{
    /// <summary>
    /// Name modifier used after transformation. Ordinary zombies retain the
    /// canonical modifier while scenario profiles can preserve a victim name.
    /// </summary>
    [DataField]
    public LocId NameModifier = "zombie-name-prefix";
}
