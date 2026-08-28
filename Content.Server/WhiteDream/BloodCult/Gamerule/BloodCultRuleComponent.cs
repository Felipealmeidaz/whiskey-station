// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Server.WhiteDream.BloodCult.RendingRunePlacement;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Constructs;
using Robust.Shared.Prototypes;

namespace Content.Server.WhiteDream.BloodCult.Gamerule;

[RegisterComponent]
public sealed partial class BloodCultRuleComponent : Component
{
    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    [DataField]
    public ProtoId<NpcFactionPrototype> BloodCultFaction = "GeometerOfBlood";

    [DataField]
    public EntProtoId HarvesterPrototype = "ConstructHarvester";

    [DataField]
    public Color EyeColor = Color.FromHex("#f80000");

    // Whiskey - stage thresholds are a share of the active crew, not a flat count.
    // 10% reveals the eyes; 20% reveals the pentagram.
    [DataField]
    public float ReadEyeThreshold = 0.1f;

    [DataField]
    public float PentagramThreshold = 0.2f;

    /// <summary>
    ///     Whiskey - the crew count captured when the round starts. Percentage requirements use this
    ///     frozen denominator so killing or removing crew cannot advance the cult by itself.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int ProgressionCrewCount;

    /// <summary>
    ///     Whiskey - the final reckoning is one per cult, not one per leader. Killing the leader
    ///     and electing another must not hand the spell back.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool FinalReckoningUsed;

    [DataField]
    public int RendingRunePlacementsAmount = 3;

    /// <summary>
    ///     How close a cultist has to be to a chosen site to draw the rending rune there.
    /// </summary>
    [DataField]
    public float RendingSiteRange = 8f;

    /// <summary>
    ///     Picked at round start from station beacons when the map has no rending markers, so the
    ///     rune is always restricted to a handful of announced places.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<RendingSite> RendingSites = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public bool RendingUnlockedAnnounced;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool LeaderSelected;

    /// <summary>
    ///     If no rending rune markers were placed on the map, players will be able to place these runes anywhere on the map
    ///     but no more than <see cref="RendingRunePlacementsAmount">total available</see>.
    /// </summary>
    [DataField]
    public bool EmergencyMarkersMode;

    public int EmergencyMarkersCount;

    /// <summary>
    ///     The entityUid of body which should be sacrificed.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? OfferingTarget;

    /// <summary>
    ///     The target currently written into already granted offering objectives. Kept separately
    ///     so a target changed during objective assignment is reconciled on the next rule tick.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? ObjectivesOfferingTarget;

    /// <summary>
    ///     Whiskey - set only when the marked one is actually given up on an offering rune. Killing
    ///     them used to be enough on its own, which handed the cult the rending rune for free.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool OfferingSacrificed;

    /// <summary>
    ///     Whiskey - who Nar'Sie will accept. A random passenger was nobody's problem: the cult
    ///     could take them apart in maintenance and the crew would never know a name was missing.
    ///     Asking for security or command puts the offering behind the people who are armed and
    ///     the people who are watched, which is the fight the objective is supposed to start.
    ///     Leave the list empty to let her take anyone.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> OfferingDepartments = new()
    {
        "Security",
        "Command"
    };

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? CultLeader;

    [ViewVariables(VVAccess.ReadOnly)]
    public CultStage Stage = CultStage.Start;

    public CultWinCondition WinCondition = CultWinCondition.Draw;

    #region Veil progression (ported from funky-station)

    /// <summary>
    ///     Whiskey - the chant is a flat head count, not a share of the crew. Scaling it meant a full
    ///     round needed eight people standing still on runes, which is a different game from the three
    ///     it was written for. Leave at zero to keep it flat; anything above zero brings scaling back.
    /// </summary>
    [DataField]
    public float VeilRitualCultistRatio;

    [DataField]
    public int VeilRitualMinCultists = 3;

    /// <summary>
    ///     How long after the veil is torn before the blood rift bleeds through.
    /// </summary>
    [DataField]
    public TimeSpan RiftSpawnDelay = TimeSpan.FromMinutes(2);

    [DataField]
    public EntProtoId RiftPrototype = "BloodCultRift";

    /// <summary>
    ///     Recalculated whenever someone tries to start the ritual.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int MinimumCultistsForVeilRitual = 2;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool VeilWeakened;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? RiftSpawnTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Rift;

    /// <summary>
    ///     Human readable location of the rift, already localised.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public string? RiftLocation;

    #endregion

    #region Ascension

    /// <summary>
    ///     Grace period between the cult being told the pentagram is coming and it actually showing up,
    ///     so nobody gets branded mid-conversation with security.
    /// </summary>
    /// <summary>
    ///     Whiskey - the red eyes get the same grace period the pentagram already had. They used
    ///     to appear out of nowhere.
    /// </summary>
    [DataField]
    public TimeSpan RedEyesWarningDelay = TimeSpan.FromMinutes(1);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? RedEyesTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool RedEyesApplied;

    [DataField]
    public TimeSpan PentagramWarningDelay = TimeSpan.FromMinutes(2);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? PentagramTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool PentagramApplied;

    /// <summary>
    ///     How long the cult gets to be harvesters before the round is called.
    /// </summary>
    [DataField]
    public TimeSpan VictoryEndDelay = TimeSpan.FromSeconds(45);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? VictoryEndTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextObjectiveCheck;

    #endregion

    #region Leadership

    /// <summary>
    ///     How long after the round starts before the cult votes on who speaks for Nar'Sie.
    /// </summary>
    [DataField]
    public TimeSpan LeaderVoteDelay = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Grace period before Nar'Sie calls a new vote after the leader dies.
    /// </summary>
    [DataField]
    public TimeSpan LeaderRevoteDelay = TimeSpan.FromSeconds(45);

    [DataField]
    public TimeSpan LeaderVoteDuration = TimeSpan.FromSeconds(45);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan? LeaderVoteTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool LeaderVoteRunning;

    #endregion

    public List<Entity<BloodCultistComponent>> Cultists = new();

    public List<Entity<ConstructComponent>> Constructs = new();

    /// <summary>
    ///     Whiskey - Nar'Sie harvests every cultist the moment she arrives, which empties both lists
    ///     above before the round end summary is built. These two survive it.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int PeakCultists;

    [ViewVariables(VVAccess.ReadOnly)]
    public int TotalConstructs;
}

/// <summary>
///     A place on the station where the veil is thin enough to tear.
/// </summary>
public sealed class RendingSite
{
    public EntityUid Beacon;
    public string Name = string.Empty;
    public bool Used;
}

public enum CultWinCondition : byte
{
    Draw,
    Win,
    Failure
}

public enum CultStage : byte
{
    Start,
    RedEyes,
    Pentagram
}

public sealed class BloodCultNarsieSummoned : EntityEventArgs;
