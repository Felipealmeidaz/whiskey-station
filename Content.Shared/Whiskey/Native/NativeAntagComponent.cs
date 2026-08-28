using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Prototypes;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Whiskey.Native;

/// <summary>
/// Connects an entity to a versioned native antagonist scenario. This component
/// contains only ABI resources and ephemeral routing handles; gameplay state is
/// owned by the native module.
/// </summary>
[RegisterComponent]
public sealed partial class NativeAntagComponent : Component
{
    [DataField(required: true)]
    public string Module = string.Empty;

    [DataField(required: true)]
    public uint Scenario;

    [DataField]
    public Dictionary<uint, EntProtoId> Actions = new();

    [DataField]
    public Dictionary<uint, string> Popups = new();

    [DataField]
    public Dictionary<uint, EntProtoId> ComponentBundles = new();

    [DataField]
    public Dictionary<uint, ProtoId<NpcFactionPrototype>> Factions = new();

    [DataField]
    public Dictionary<uint, SoundSpecifier> Sounds = new();

    /// <summary>
    /// Dedicated surgical instrument prototypes mapped to compact native tool
    /// tokens. The bridge reports only the active-hand instrument as a bit mask;
    /// native state owns and validates the ordered procedure step.
    /// </summary>
    [DataField]
    public Dictionary<uint, EntProtoId> RequiredTools = new();

    /// <summary>
    /// Detached component profiles used by generic transformation commands.
    /// The entity prototype is a data container and is never spawned.
    /// </summary>
    [DataField]
    public Dictionary<uint, EntProtoId> ZombieProfiles = new();

    [ViewVariables]
    public ulong Handle;

    /// <summary>
    /// Ephemeral entity routing value used to build ECS snapshots during a
    /// native procedure. It is not a managed copy of procedure state.
    /// </summary>
    [ViewVariables]
    public EntityUid? RoutedTarget;

    [ViewVariables]
    public Dictionary<uint, EntityUid> ActionEntities = new();

    [ViewVariables]
    public Dictionary<uint, EntityUid> AudioStreams = new();

    [ViewVariables]
    public Dictionary<EntityUid, EntityUid> PatientSpeechStreams = new();

    public uint[] RequiredToolTokenCache = [];
}

/// <summary>
/// Marks an entity produced by a native scenario and records its native owner.
/// </summary>
[RegisterComponent]
public sealed partial class NativeAntagPatientComponent : Component
{
    [DataField]
    public EntityUid Master;

    /// <summary>
    /// Sound token resolved from the owning scenario whenever this patient
    /// speaks. Zero disables the patient speech cue.
    /// </summary>
    [DataField]
    public uint SpeechSoundToken;

    [DataField]
    public EntProtoId? ActionJumpId;

    [DataField]
    public EntProtoId? ActionFlairId;

    [DataField]
    public TimeSpan ParalyzeTime = TimeSpan.FromSeconds(5);

    [DataField]
    public DamageSpecifier ThrowDamage = new()
    {
        DamageDict = new()
        {
            { "Slash", 15 },
        },
    };

    [DataField]
    public float MaxThrow = 10f;

    [DataField]
    public float MaxFlairDistance = 500f;

    [ViewVariables]
    public List<EntityUid> ActionEntities = new();
}

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

/// <summary>
/// Disables a game rule unless its exact native module and ABI are available.
/// </summary>
[RegisterComponent]
public sealed partial class NativeModuleRequirementComponent : Component
{
    [DataField(required: true)]
    public string Module = string.Empty;

    [DataField]
    public uint AbiVersion = 1;
}
