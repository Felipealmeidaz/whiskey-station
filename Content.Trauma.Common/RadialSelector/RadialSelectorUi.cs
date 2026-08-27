// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Common.RadialSelector;

[NetSerializable, Serializable]
public enum RadialSelectorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RadialSelectorState(List<RadialSelectorEntry> entries, bool openCentered = false)
    : BoundUserInterfaceState
{
    public List<RadialSelectorEntry> Entries = entries;

    // WhiteDream - Blood Cult
    public bool OpenCentered { get; private set; } = openCentered;
}

[Serializable, NetSerializable]
public sealed class RadialSelectorSelectedMessage(string selectedItem) : BoundUserInterfaceMessage
{
    public readonly string SelectedItem = selectedItem;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class RadialSelectorEntry
{
    [DataField]
    public string? Prototype { get; set; }

    /// <summary>
    ///     How many copies the server should produce when this entry is selected. Menus that do
    ///     not support batches simply ignore it; factories use it for ammunition bundles.
    /// </summary>
    [DataField]
    public int Amount { get; set; } = 1;

    // <WhiteDream> - Blood Cult
    [DataField]
    public string? Name { get; set; }

    [DataField]
    public bool CloseUiOnSelect = true;

    /// <summary>
    ///     Entity whose sprite is used as the icon.
    ///     Actions no longer carry a SpriteSpecifier on this engine, so the only way to draw one
    ///     is to point at the action entity itself. Stuffing its EntityUid into Prototype instead
    ///     makes the client try to spawn a prototype named after a number, which throws and takes
    ///     the whole BUI down with it.
    /// </summary>
    [DataField]
    public NetEntity? IconEntity { get; set; }
    // </WhiteDream>

    [DataField]
    public SpriteSpecifier? Icon { get; set; }

    [DataField]
    public RadialSelectorCategory? Category { get; set; }
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class RadialSelectorCategory
{
    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    [DataField(required: true)]
    public List<RadialSelectorEntry> Entries = default!;
}
