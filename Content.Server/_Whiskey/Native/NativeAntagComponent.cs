using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Whiskey.Native;

/// <summary>
/// Connects a server entity to a versioned native antagonist scenario. No part
/// of this component is replicated; clients receive only ordinary action and
/// presentation state produced by the authoritative ECS systems.
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

    /// <summary>
    /// Body-local idempotency cache used by the bridge even when no mind owns
    /// this entity. Mind objectives keep the durable round-end copy.
    /// </summary>
    [ViewVariables]
    public Dictionary<uint, HashSet<EntityUid>> CountedTargets = new();
}

/// <summary>
/// Marks an entity produced by a native scenario and records its native owner.
/// </summary>
[RegisterComponent]
public sealed partial class NativeAntagPatientComponent : Component
{
    [DataField]
    public EntityUid Master;

    [DataField]
    public uint SpeechSoundToken;

    [DataField]
    public EntProtoId? ActionJumpId;

    [DataField]
    public EntProtoId? ActionFlairId;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier ReceptionSound = new SoundPathSpecifier(
        "/Audio/_Whiskey/OperativeHidden/operative_hidden_patient_reception.ogg");

    [DataField]
    public float MaxThrow = 10f;

    [DataField]
    public float MaxFlairDistance = 500f;

    /// <summary>
    /// Maximum distance the receiver permits between this conscious patient
    /// and its Hidden Operative before seizing the patient's motor cortex.
    /// </summary>
    [DataField]
    public float MaxMasterDistance = 10f;

    [DataField]
    public TimeSpan LeashPainCooldown = TimeSpan.FromSeconds(3);

    [ViewVariables]
    public TimeSpan NextLeashPain;

    [ViewVariables]
    public TimeSpan SignalLostUntil;

    [ViewVariables]
    public bool SignalLost;

    [ViewVariables]
    public List<EntityUid> ActionEntities = new();
}

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
