using System.Linq;
using System.Numerics;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Clothing.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.PDA.Ringer;
using Content.Server.Roles;
using Content.Server.Traitor.Uplink;
using Content.Server.Whiskey.Native;
using Content.Server.Zombies;
using Content.Shared.Body;
using Content.Shared.Antag;
using Content.Shared.Chat;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mind;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.NukeOps;
using Content.Shared.PDA.Ringer;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Storage;
using Content.Shared.StatusEffect;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Whiskey.Native;
using Content.Shared.Zombies;
using Content.Trauma.Common.Language;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Components;
using Robust.Shared.Containers;
using Robust.Shared.Localization;
using Robust.UnitTesting;

namespace Content.IntegrationTests.Tests.Whiskey;

[TestFixture]
public sealed class OperativeHiddenTest : GameTest
{
    private static readonly EntProtoId[] OperativeActions =
    [
        "ActionOperativeHiddenTouch",
        "ActionOperativeHiddenProcedure",
        "ActionOperativeHiddenSelfHeal",
        "ActionOperativeHiddenPatientHeal",
        "ActionOperativeHiddenPatientKill",
    ];
    private static readonly EntProtoId OperativeBody = "MobOviniaOperativeHidden";
    private static readonly EntProtoId OrdinaryOvinia = "MobOvinia";
    private static readonly EntProtoId OperativeRule = "OperativeHiddenRule";
    private static readonly EntProtoId LoneOpsRule = "LoneOpsSpawn";
    private static readonly EntProtoId OperativeDuffel = "ClothingBackpackDuffelSyndicateOperativeHidden";
    private static readonly EntProtoId PatientZombieProfile = "OperativeHiddenPatientZombieProfile";
    private static readonly EntProtoId PatientComponentBundle = "OperativeHiddenPatientComponents";
    private static readonly ProtoId<StartingGearPrototype> OperativeGear = "OperativeHiddenGear";
    private static readonly ProtoId<StartingGearPrototype> LoneOpsGear = "SyndicateLoneOperativeGearFull";
    private static readonly ProtoId<LanguagePrototype> UniversalLanguage = "Universal";
    private static readonly ProtoId<LanguagePrototype> PatientLanguage = "TauCetiBasic";
    private static readonly ProtoId<AntagSpecifierPrototype> OperativeSpecifier = "OperativeHidden";
    private static readonly ProtoId<AntagSpecifierPrototype> LoneOpsSpecifier = "LoneOp";
    private static readonly ProtoId<NpcFactionPrototype> SyndicateFaction = "Syndicate";

    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [Test]
    public async Task FixedOviniaBodyCyberneticsAndSelfContainedGear()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await pair.Client.WaitAssertion(() =>
        {
            var spriteName = pair.Client.EntMan.ComponentFactory.CompName<SpriteComponent>();
            foreach (var actionId in OperativeActions)
            {
                var action = pair.Client.ProtoMan.Index(actionId);
                Assert.That(action.HasComp(spriteName), Is.True,
                    $"{actionId} must expose a client Sprite component for the action bar and key binds");
            }
        });

        await server.WaitAssertion(() =>
        {
            var factory = server.ResolveDependency<IComponentFactory>();
            var containerSystem = server.System<SharedContainerSystem>();
            var inventorySystem = server.System<InventorySystem>();
            var outfitSystem = server.System<OutfitSystem>();

            var bodyPrototype = server.ProtoMan.Index(OperativeBody);
            Assert.That(bodyPrototype.TryComp<InitialBodyComponent>(out var initialBody, factory), Is.True);
            Assert.That(bodyPrototype.TryComp<HumanoidProfileComponent>(out var profile, factory), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(profile.Species.Id, Is.EqualTo("Ovinia"));
                Assert.That(initialBody.Organs["Brain"].Id, Is.EqualTo("OrganOperativeHiddenCyberBrain"));
                Assert.That(initialBody.Organs["Eyes"].Id, Is.EqualTo("OrganOperativeHiddenCyberEyes"));
                Assert.That(initialBody.Organs["Heart"].Id, Is.EqualTo("OrganOperativeHiddenCyberHeart"));
                Assert.That(initialBody.Organs["Lungs"].Id, Is.EqualTo("OrganOperativeHiddenCyberLungs"));
                Assert.That(initialBody.Organs["Liver"].Id, Is.EqualTo("OrganOperativeHiddenCyberLiver"));
                Assert.That(initialBody.Organs["Kidneys"].Id, Is.EqualTo("OrganOperativeHiddenCyberKidneys"));
            });

            var ordinary = server.ProtoMan.Index(OrdinaryOvinia);
            Assert.That(ordinary.TryComp<InitialBodyComponent>(out var ordinaryBody, factory), Is.True);
            Assert.That(ordinaryBody.Organs.Values.All(id => !id.Id.StartsWith("OrganOperativeHidden")), Is.True,
                "ordinary Ovinias must not inherit operative cybernetics");

            var operative = server.EntMan.SpawnEntity(OperativeBody, map.GridCoords);
            var bodyContainer = containerSystem.GetContainer(operative, BodyComponent.ContainerID);
            var installed = bodyContainer.ContainedEntities
                .Select(uid => server.EntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID)
                .Where(id => id != null)
                .ToHashSet();

            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.GetComponent<MetaDataComponent>(operative).EntityName, Is.Not.Empty);
                Assert.That(installed, Does.Contain("OrganOperativeHiddenCyberBrain"));
                Assert.That(installed, Does.Contain("OrganOperativeHiddenCyberEyes"));
                Assert.That(installed, Does.Contain("OrganOperativeHiddenCyberHeart"));
                Assert.That(installed, Does.Contain("OrganOperativeHiddenCyberLungs"));
                Assert.That(installed, Does.Contain("OrganOperativeHiddenCyberLiver"));
                Assert.That(installed, Does.Contain("OrganOperativeHiddenCyberKidneys"));
                Assert.That(server.EntMan.HasComponent<UplinkComponent>(operative), Is.False);
                Assert.That(server.EntMan.HasComponent<RingerAccessUplinkComponent>(operative), Is.False);
                Assert.That(server.EntMan.HasComponent<RingerUplinkComponent>(operative), Is.False);
                Assert.That(server.EntMan.HasComponent<Content.Shared.CombatMode.Pacification.PacifiedComponent>(operative), Is.False,
                    "the operative must not receive a permanent mental pacification restraint");
                Assert.That(server.EntMan.GetComponent<StatusEffectsComponent>(operative).AllowedEffects,
                    Does.Not.Contain("Pacified"),
                    "the operative must reject every later attempt to reapply pacifism");
                var thresholds = server.EntMan.GetComponent<MobThresholdsComponent>(operative).Thresholds;
                Assert.That(thresholds.Values, Does.Contain(MobState.Dead),
                    "the operative must be able to leave Critical through the normal death state");
            });

            var native = server.EntMan.GetComponent<NativeAntagComponent>(operative);
            Assert.Multiple(() =>
            {
                Assert.That(native.Handle, Is.Not.Zero, "the real native ELF must initialize during entity startup");
                Assert.That(native.ActionEntities, Has.Count.EqualTo(5));
            });

            Assert.That(outfitSystem.SetOutfit(operative, "OperativeHiddenGear"), Is.True);
            Assert.That(inventorySystem.TryGetSlotEntity(operative, "id", out var equippedId), Is.True);
            var equippedIdUid = equippedId!.Value;
            Assert.Multiple(() =>
            {
                Assert.That(server.EntMan.GetComponent<MetaDataComponent>(equippedIdUid).EntityPrototype?.ID,
                    Is.EqualTo("OperativeHiddenMortuaryIDCard"));
                Assert.That(server.EntMan.HasComponent<UplinkComponent>(equippedIdUid), Is.False);
                Assert.That(server.EntMan.HasComponent<RingerAccessUplinkComponent>(equippedIdUid), Is.False);
                Assert.That(server.EntMan.HasComponent<RingerUplinkComponent>(equippedIdUid), Is.False);
            });
            var gear = server.ProtoMan.Index(OperativeGear);
            Assert.That(gear.Inhand, Is.Empty, "the operative must not spawn with a firearm or other held weapon");
            Assert.That(gear.Storage, Is.Empty, "the filled backpack owns its contents and avoids duplicate outfit insertion");
            Assert.Multiple(() =>
            {
                Assert.That(gear.Equipment["jumpsuit"], Is.EqualTo((EntProtoId) "ClothingUniformOperativeHiddenMourningDress"));
                Assert.That(gear.Equipment["mask"], Is.EqualTo((EntProtoId) "ClothingMaskOperativeHiddenSuturedGaiter"));
                Assert.That(gear.Equipment["eyes"], Is.EqualTo((EntProtoId) "ClothingEyesOperativeHiddenDeadroomLenses"));
                Assert.That(gear.Equipment["head"], Is.EqualTo((EntProtoId) "ClothingHeadOperativeHiddenTheaterCap"));
                Assert.That(gear.Equipment["outerClothing"], Is.EqualTo((EntProtoId) "ClothingOuterOperativeHiddenOssuaryCoat"));
                Assert.That(gear.Equipment.Values.Any(id => id.Id.Contains("Helmet", StringComparison.OrdinalIgnoreCase)), Is.False,
                    "the tested default outfit does not contain a helmet");
            });

            foreach (var slot in gear.Equipment.Keys)
            {
                Assert.That(inventorySystem.TryGetSlotEntity(operative, slot, out var equipped), Is.True,
                    $"operative gear slot {slot} must be equipped");
                Assert.That(server.EntMan.HasComponent<UnremoveableComponent>(equipped!.Value), Is.True,
                    $"operative gear slot {slot} must be irremovable");
                Assert.That(server.EntMan.GetComponent<MetaDataComponent>(equipped.Value).EntityName, Is.Not.Empty,
                    $"operative gear slot {slot} must have a localized name");
                Assert.That(server.EntMan.GetComponent<MetaDataComponent>(equipped.Value).EntityDescription, Is.Not.Empty,
                    $"operative gear slot {slot} must have a localized description");
                Assert.That(inventorySystem.TryUnequip(operative, slot), Is.False,
                    $"operative gear slot {slot} must reject ordinary removal");
            }

            var duffel = server.EntMan.SpawnEntity(OperativeDuffel, map.GridCoords);
            var contents = server.EntMan.GetComponent<StorageComponent>(duffel).Container.ContainedEntities;
            var ids = contents
                .Select(uid => server.EntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID)
                .Where(id => id != null)
                .ToHashSet();
            Assert.Multiple(() =>
            {
                Assert.That(contents, Has.Count.EqualTo(9));
                Assert.That(ids, Does.Contain("MedkitCombatFilled"));
                Assert.That(ids, Does.Not.Contain("OmnimedToolSyndie"));
                Assert.That(ids, Does.Contain("Scalpel"));
                Assert.That(ids, Does.Contain("Retractor"));
                Assert.That(ids, Does.Contain("Hemostat"));
                Assert.That(ids, Does.Contain("Cautery"));
                Assert.That(ids, Does.Contain("Saw"));
                Assert.That(ids, Does.Contain("Drill"));
                Assert.That(ids, Does.Contain("Bonesetter"));
                Assert.That(ids, Does.Contain("BoneGel"));
                Assert.That(contents.Any(uid => server.EntMan.HasComponent<UplinkComponent>(uid)), Is.False);
                Assert.That(contents.Any(uid => server.EntMan.HasComponent<RingerAccessUplinkComponent>(uid)), Is.False);
                Assert.That(contents.Any(uid => server.EntMan.HasComponent<RingerUplinkComponent>(uid)), Is.False);
                Assert.That(ids.Any(id => id!.Contains("Telecrystal") || id.Contains("Uplink")), Is.False);
            });

            server.EntMan.DeleteEntity(operative);
            server.EntMan.DeleteEntity(duffel);
            server.System<SharedMapSystem>().DeleteMap(map.MapId);
        });
    }

    [Test]
    public async Task TouchDeathWorksForPlayableSpeciesAndSurvivesThresholdRecalculation()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var mobState = server.System<MobStateSystem>();
            var thresholds = server.System<MobThresholdSystem>();
            foreach (var species in new[]
                     {
                         "MobHuman",
                         "MobOvinia",
                         "MobReptilian",
                         "MobMoth",
                         "MobDiona",
                         "MobSlimePerson",
                         "MobVox",
                     })
            {
                var operative = server.EntMan.SpawnEntity(OperativeBody, map.GridCoords);
                var victim = server.EntMan.SpawnEntity(species, map.GridCoords);
                Assert.That(server.EntMan.HasComponent<HumanoidProfileComponent>(victim), Is.True,
                    $"{species} must be recognized as a playable species target");

                var action = new NativeAntagTargetActionEvent
                {
                    EventType = (uint) NativeAntagEventType.TouchAction,
                    Target = victim,
                };
                server.EntMan.EventBus.RaiseLocalEvent(operative, action);

                Assert.That(mobState.IsDead(victim), Is.True,
                    $"touch must kill playable species {species} through the ordinary damage pipeline");
                Assert.That(thresholds.TryGetThresholdForState(victim, MobState.Dead, out var deadThreshold), Is.True);
                Assert.That(thresholds.CheckVitalDamage(victim), Is.GreaterThanOrEqualTo(deadThreshold!.Value));

                thresholds.VerifyThresholds(victim);
                Assert.That(mobState.IsDead(victim), Is.True,
                    $"a later threshold recalculation must not bring {species} back to life");
            }

            server.System<SharedMapSystem>().DeleteMap(map.MapId);
        });
    }

    [Test]
    public void RuleInheritsLoneOpsConditionsWithoutItsUplinkRuntime()
    {
        var factory = Server.ResolveDependency<IComponentFactory>();
        var operativeRule = SProtoMan.Index(OperativeRule);
        var loneOpsRule = SProtoMan.Index(LoneOpsRule);

        Assert.Multiple(() =>
        {
            Assert.That(operativeRule.HasComp<NativeModuleRequirementComponent>(factory), Is.True);
            Assert.That(operativeRule.HasComp<NukeopsRuleComponent>(factory), Is.False,
                "the operative must not receive the NukeOps uplink/TC setup");
            Assert.That(loneOpsRule.HasComp<NukeopsRuleComponent>(factory), Is.True,
                "the original LoneOps rule must retain its normal uplink runtime");
        });
    }

    [Test]
    public void PatientZombieProfileMatchesFireRuntime()
    {
        var factory = Server.ResolveDependency<IComponentFactory>();
        var profilePrototype = SProtoMan.Index(PatientZombieProfile);
        var bundlePrototype = SProtoMan.Index(PatientComponentBundle);
        Assert.That(profilePrototype.TryComp<ZombieComponent>(out var zombie, factory), Is.True);
        Assert.That(bundlePrototype.TryComp<NativeAntagPatientComponent>(out var patient, factory), Is.True);
        var ordinaryZombie = new ZombieComponent();

        Assert.Multiple(() =>
        {
            Assert.That(zombie.BaseZombieInfectionChance, Is.EqualTo(0.75f));
            Assert.That(zombie.ZombieMovementSpeedDebuff, Is.EqualTo(0.95f));
            Assert.That(zombie.PassiveHealingCritMultiplier, Is.EqualTo(2f));
            Assert.That(zombie.HealingOnBite.DamageDict["Blunt"].Float(), Is.EqualTo(-2f));
            Assert.That(zombie.HealingOnBite.DamageDict["Slash"].Float(), Is.EqualTo(-2f));
            Assert.That(zombie.HealingOnBite.DamageDict["Piercing"].Float(), Is.EqualTo(-2f));
            Assert.That(zombie.ResistanceEffectiveness.DamageDict.ContainsKey("Ballistic"), Is.False);
            Assert.That(zombie.PassiveHealing.DamageDict.ContainsKey("Ballistic"), Is.False);
            Assert.That(zombie.DamageOnBite.DamageDict["Slash"].Float(), Is.EqualTo(13f));
            Assert.That(zombie.DamageOnBite.DamageDict["Piercing"].Float(), Is.EqualTo(7f));
            Assert.That(patient.ThrowDamage.DamageDict["Slash"].Float(), Is.EqualTo(15f));
            Assert.That(patient.ParalyzeTime, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(patient.MaxThrow, Is.EqualTo(10f));
            Assert.That(patient.MaxFlairDistance, Is.EqualTo(500f));
            Assert.That(patient.ActionJumpId?.Id, Is.EqualTo("ZombieJump"));
            Assert.That(patient.ActionFlairId?.Id, Is.EqualTo("ZombieFlair"));
            Assert.That(zombie.ForcedLanguage.Id, Is.EqualTo("TauCetiBasic"));
            Assert.That(zombie.NameModifier.Id, Is.EqualTo("operative-hidden-patient-name-prefix"));
            Assert.That(SProtoMan.HasIndex<EntityPrototype>(patient.ActionJumpId!.Value), Is.True);
            Assert.That(SProtoMan.HasIndex<EntityPrototype>(patient.ActionFlairId!.Value), Is.True);
            Assert.That(ordinaryZombie.BaseZombieInfectionChance, Is.EqualTo(1f),
                "the patient profile must not rebalance ordinary Whiskey zombies");
            Assert.That(ordinaryZombie.PassiveHealingCritMultiplier, Is.EqualTo(5f));
            Assert.That(ordinaryZombie.HealingOnBite.DamageDict["Blunt"].Float(), Is.EqualTo(-25f));
        });
    }

    [Test]
    public async Task CorpseProcedureConvertsThroughNativeBridge()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var bridge = server.System<NativeAntagBridgeSystem>();
            var factions = server.System<NpcFactionSystem>();
            var hands = server.System<SharedHandsSystem>();
            var mobState = server.System<MobStateSystem>();
            var transform = server.System<SharedTransformSystem>();
            var operative = server.EntMan.SpawnEntity(OperativeBody, map.GridCoords);
            var victim = server.EntMan.SpawnEntity("MobHuman", map.GridCoords);
            var originalName = server.EntMan.GetComponent<MetaDataComponent>(victim).EntityName;
            mobState.ChangeMobState(victim, MobState.Dead);

            var activeHand = server.EntMan.GetComponent<HandsComponent>(operative).ActiveHandId!;
            foreach (var invalidTool in new[] { "OmnimedToolSyndie", "Saw" })
            {
                var held = server.EntMan.SpawnEntity(invalidTool, map.GridCoords);
                Assert.That(hands.TryPickup(operative, held, activeHand), Is.True);

                var rejectedAction = new NativeAntagTargetActionEvent
                {
                    EventType = (uint) NativeAntagEventType.ProcedureAction,
                    Target = victim,
                };
                server.EntMan.EventBus.RaiseLocalEvent(operative, rejectedAction);
                bridge.Update(4f);

                Assert.That(server.EntMan.HasComponent<ZombieComponent>(victim), Is.False,
                    $"{invalidTool} must not start or skip the first cautery stage");
                Assert.That(hands.TryDrop(operative, held, checkActionBlocker: false), Is.True);
                server.EntMan.DeleteEntity(held);
            }

            var toolOrder = new[] { "Cautery", "Drill", "Scalpel", "Retractor", "Hemostat", "Saw" };
            foreach (var tool in toolOrder)
            {
                var held = server.EntMan.SpawnEntity(tool, map.GridCoords);
                Assert.That(hands.TryPickup(operative, held, activeHand), Is.True);

                var action = new NativeAntagTargetActionEvent
                {
                    EventType = (uint) NativeAntagEventType.ProcedureAction,
                    Target = victim,
                };
                server.EntMan.EventBus.RaiseLocalEvent(operative, action);

                if (tool == toolOrder[0])
                {
                    var blockedDamage = new DamageSpecifier();
                    var zeroDamage = new DamageDealtEvent(
                        blockedDamage,
                        victim,
                        InterruptsDoAfters: true,
                        IgnoreBlockers: false,
                        ModifiedDamage: blockedDamage);
                    server.EntMan.EventBus.RaiseLocalEvent(operative, ref zeroDamage);
                    transform.SetCoordinates(operative, map.GridCoords.Offset(new Vector2(0.1f, 0.1f)));
                    transform.SetCoordinates(victim, map.GridCoords.Offset(new Vector2(-0.1f, -0.1f)));
                }

                bridge.Update(3.5f);

                if (tool != toolOrder[^1])
                    Assert.That(server.EntMan.HasComponent<ZombieComponent>(victim), Is.False,
                        $"the procedure must not convert before completing the {tool} stage");

                Assert.That(hands.TryDrop(operative, held, checkActionBlocker: false), Is.True);
                server.EntMan.DeleteEntity(held);
            }

            Assert.That(server.EntMan.HasComponent<ZombieComponent>(victim), Is.True,
                "the ordered six-instrument procedure must convert the patient");
            var patient = server.EntMan.GetComponent<NativeAntagPatientComponent>(victim);
            var patientZombie = server.EntMan.GetComponent<ZombieComponent>(victim);
            var localization = server.ResolveDependency<ILocalizationManager>();
            var expectedName = localization.GetString(
                "operative-hidden-patient-name-prefix",
                ("baseName", originalName));
            Assert.Multiple(() =>
            {
                Assert.That(patient.Master, Is.EqualTo(operative));
                Assert.That(patient.SpeechSoundToken, Is.EqualTo(2));
                Assert.That(patient.ActionEntities, Has.Count.EqualTo(2));
                Assert.That(mobState.IsAlive(victim), Is.True);
                Assert.That(factions.IsMember(victim, SyndicateFaction), Is.True);
                Assert.That(server.EntMan.GetComponent<MetaDataComponent>(victim).EntityName,
                    Is.EqualTo(expectedName));
                Assert.That(patientZombie.ForcedLanguage, Is.EqualTo(PatientLanguage));
                Assert.That(server.EntMan.HasComponent<ZombieAccentOverrideComponent>(victim), Is.True);
                Assert.That(server.EntMan.GetComponent<ZombieAccentOverrideComponent>(victim).Accent,
                    Is.EqualTo("OperativeHiddenLobotomy"));
                Assert.That(server.EntMan.HasComponent<ReplacementAccentComponent>(victim), Is.True,
                    "zombification must apply the configured lobotomy replacement accent");
                Assert.That(server.EntMan.HasComponent<NativeAntagComponent>(victim), Is.False,
                    "patients must not inherit the operative's continuous disclosure radio");
            });

            var accentSource = localization.GetString("operative-hidden-lobotomy-word-1");
            var accentReplacement = localization.GetString("operative-hidden-lobotomy-replacement-1");
            var distorted = server.System<ReplacementAccentSystem>()
                .ApplyReplacements(accentSource, "OperativeHiddenLobotomy", victim);
            Assert.That(distorted, Is.EqualTo(accentReplacement),
                "the lobotomy accent must visibly distort patient chat");

            var language = server.ProtoMan.Index(PatientLanguage);
            var spoke = new EntitySpokeEvent(victim, distorted, null, false, language);
            server.EntMan.EventBus.RaiseLocalEvent(victim, spoke);
            var native = server.EntMan.GetComponent<NativeAntagComponent>(operative);
            Assert.That(native.PatientSpeechStreams.TryGetValue(victim, out var patientSpeech), Is.True,
                "patient speech must play the operative's short speech collection");
            var patientAudio = server.EntMan.GetComponent<AudioComponent>(patientSpeech);
            Assert.Multiple(() =>
            {
                Assert.That(patientAudio.FileName,
                    Does.StartWith("/Audio/Whiskey/OperativeHidden/operative_hidden_speech"));
                Assert.That(patientAudio.FileName,
                    Is.Not.EqualTo("/Audio/Whiskey/OperativeHidden/operative_hidden_position.ogg"));
                Assert.That(patientAudio.Params.MaxDistance, Is.EqualTo(5f));
                Assert.That(patientAudio.Flags.HasFlag(AudioFlags.NoOcclusion), Is.False);
            });

            server.EntMan.DeleteEntity(victim);
            Assert.That(native.PatientSpeechStreams.ContainsKey(victim), Is.False,
                "deleting a patient must stop and forget its short speech stream");

            server.System<SharedMapSystem>().DeleteMap(map.MapId);
        });
    }

    [Test]
    public async Task SpawnerPreservesMindAndIgnoresOriginalSpecies()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var session = pair.Player!;
            var originalBody = server.EntMan.SpawnEntity("MobHuman", map.GridCoords);
            var originalProfile = server.EntMan.GetComponent<HumanoidProfileComponent>(originalBody);
            originalProfile.Species = "Reptilian";

            var mindSystem = server.System<MindSystem>();
            var roleSystem = server.System<RoleSystem>();
            var mind = mindSystem.GetOrCreateMind(session.UserId);
            mindSystem.TransferTo(mind, originalBody, ghostCheckOverride: true, mind: mind.Comp);
            Assert.That(session.AttachedEntity, Is.EqualTo(originalBody));

            var rule = server.EntMan.SpawnEntity(OperativeRule, map.GridCoords);
            var selection = server.EntMan.GetComponent<AntagSelectionComponent>(rule);
            var loadedGrids = new RuleLoadedGridsEvent(map.MapId, new[] { map.Grid.Owner });
            server.EntMan.EventBus.RaiseLocalEvent(rule, ref loadedGrids);
            // The loaded striker shuttle uses the generic nuclear-operative spawn marker.
            server.EntMan.SpawnEntity("SpawnPointNukies", map.GridCoords);

            var specifier = server.ProtoMan.Index(OperativeSpecifier);
            Assert.That(server.System<AntagSelectionSystem>()
                .TryMakeAntag((rule, selection), specifier, session, checkPref: false), Is.True);

            Assert.That(mind.Comp.CurrentEntity, Is.Not.Null);
            var operative = mind.Comp.CurrentEntity!.Value;
            var operativeProfile = server.EntMan.GetComponent<HumanoidProfileComponent>(operative);
            var roles = roleSystem.MindGetAllRoleInfo(mind.Owner);
            var objectivesSystem = server.System<ObjectivesSystem>();
            var objectiveInfo = mind.Comp.Objectives
                .Select(objective => objectivesSystem.GetInfo(objective, mind.Owner, mind.Comp))
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(operative, Is.Not.EqualTo(originalBody));
                Assert.That(operativeProfile.Species.Id, Is.EqualTo("Ovinia"));
                Assert.That(mind.Comp.CurrentEntity, Is.EqualTo(operative));
                Assert.That(session.AttachedEntity, Is.EqualTo(operative));
                Assert.That(roleSystem.MindIsAntagonist(mind.Owner), Is.True);
                Assert.That(roles.Count(role => role.Prototype == "OperativeHidden"), Is.EqualTo(1));
                Assert.That(mind.Comp.Objectives, Has.Count.EqualTo(2));
                Assert.That(objectiveInfo, Has.Length.EqualTo(2));
                Assert.That(objectiveInfo.All(info => info is not null), Is.True,
                    "both objectives must provide title, description, icon, and progress to the character UI");
                Assert.That(server.EntMan.GetComponent<MetaDataComponent>(operative).EntityName, Is.Not.Empty);
            });

            server.EntMan.DeleteEntity(rule);
            server.System<SharedMapSystem>().DeleteMap(map.MapId);
        });
    }

    [Test]
    public async Task PositionalRadioIsBroadcastToOperativeAndNearbyListener()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var operativeSession = await server.AddDummySession("operative_radio_source");
        NetEntity disclosureNet = default;
        NetEntity speechNet = default;

        await server.WaitAssertion(() =>
        {
            var listener = server.EntMan.SpawnEntity("MobHuman", map.GridCoords);
            var listenerSession = pair.Player!;
            server.PlayerMan.SetAttachedEntity(listenerSession, listener);

            var operative = server.EntMan.SpawnEntity(OperativeBody, map.GridCoords);
            server.PlayerMan.SetAttachedEntity(operativeSession, operative);

            var native = server.EntMan.GetComponent<NativeAntagComponent>(operative);
            Assert.That(native.AudioStreams.TryGetValue(1, out var disclosureStream), Is.True,
                "attaching a player must start the continuous disclosure radio");
            AssertBroadcastRecipients(server, disclosureStream, operative, listener);
            disclosureNet = server.EntMan.GetNetEntity(disclosureStream);

            var language = server.ProtoMan.Index(UniversalLanguage);
            var spoke = new EntitySpokeEvent(operative, "teste", null, false, language);
            server.EntMan.EventBus.RaiseLocalEvent(operative, spoke);

            Assert.That(native.AudioStreams.TryGetValue(2, out var speechStream), Is.True,
                "speaking must start the random 1.5/2 second radio cue");
            AssertBroadcastRecipients(server, speechStream, operative, listener);
            speechNet = server.EntMan.GetNetEntity(speechStream);
        });

        await pair.RunUntilSynced();
        await pair.Client.WaitAssertion(() =>
        {
            Assert.That(pair.Client.EntMan.TryGetEntity(disclosureNet, out var disclosureClient), Is.True,
                "the listening client must receive the continuous radio entity");
            Assert.That(pair.Client.EntMan.HasComponent<AudioComponent>(disclosureClient), Is.True);
            Assert.That(pair.Client.EntMan.TryGetEntity(speechNet, out var speechClient), Is.True,
                "the listening client must receive the speech radio entity");
            Assert.That(pair.Client.EntMan.HasComponent<AudioComponent>(speechClient), Is.True);
        });

        await server.WaitAssertion(() =>
        {
            server.PlayerMan.SetAttachedEntity(operativeSession, null);
            server.PlayerMan.SetAttachedEntity(pair.Player!, null);
            server.System<SharedMapSystem>().DeleteMap(map.MapId);
        });
        await server.RemoveDummySession(operativeSession, removeUser: true);
    }

    private static void AssertBroadcastRecipients(
        RobustIntegrationTest.ServerIntegrationInstance server,
        EntityUid stream,
        EntityUid operative,
        EntityUid listener)
    {
        var audio = server.EntMan.GetComponent<AudioComponent>(stream);
        var streamTransform = server.EntMan.GetComponent<TransformComponent>(stream);
        var operativeTransform = server.EntMan.GetComponent<TransformComponent>(operative);

        Assert.Multiple(() =>
        {
            Assert.That(streamTransform.ParentUid, Is.EqualTo(operativeTransform.MapUid),
                "the sound must be map-anchored so it does not depend on operative PVS");
            Assert.That(audio.Params.MaxDistance, Is.EqualTo(5f),
                "the disclosure radio must not be audible beyond five tiles");
            Assert.That(audio.Params.ReferenceDistance, Is.EqualTo(0.8f));
            Assert.That(audio.Params.Volume, Is.EqualTo(-7f));
            Assert.That(audio.Flags.HasFlag(AudioFlags.NoOcclusion), Is.False,
                "walls must keep the engine's low-pass occlusion enabled");
            Assert.That(audio.IncludedEntities, Does.Contain(operative));
            Assert.That(audio.IncludedEntities, Does.Contain(listener),
                "a second attached player must receive the radio audio state");
        });
    }

    [Test]
    public async Task LoneOpsStillSelectsWithRoleUplinkTcAndObjectives()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            var session = pair.Player!;
            var originalBody = server.EntMan.SpawnEntity("MobHuman", map.GridCoords);
            var mindSystem = server.System<MindSystem>();
            var roleSystem = server.System<RoleSystem>();
            var factions = server.System<NpcFactionSystem>();
            var mind = mindSystem.GetOrCreateMind(session.UserId);
            mindSystem.TransferTo(mind, originalBody, ghostCheckOverride: true, mind: mind.Comp);

            var rule = server.EntMan.SpawnEntity(LoneOpsRule, map.GridCoords);
            var selection = server.EntMan.GetComponent<AntagSelectionComponent>(rule);
            var loadedGrids = new RuleLoadedGridsEvent(map.MapId, new[] { map.Grid.Owner });
            server.EntMan.EventBus.RaiseLocalEvent(rule, ref loadedGrids);
            server.EntMan.SpawnEntity("SpawnPointNukies", map.GridCoords);

            var specifier = server.ProtoMan.Index(LoneOpsSpecifier);
            var gear = server.ProtoMan.Index(LoneOpsGear);
            Assert.That(specifier.StartingGear, Is.EqualTo(LoneOpsGear));
            Assert.That(gear.Equipment["pocket2"].Id, Is.EqualTo("LoneOpsUplink225TC"));

            Assert.That(server.System<AntagSelectionSystem>()
                .TryMakeAntag((rule, selection), specifier, session, checkPref: false), Is.True);

            Assert.That(mindSystem.TryGetMind(session, out var loneMindId, out var loneMindComponent), Is.True);
            var loneMind = new Entity<MindComponent>(loneMindId, loneMindComponent);
            Assert.That(loneMind.Comp.CurrentEntity, Is.Not.Null);
            var loneOperative = loneMind.Comp.CurrentEntity!.Value;
            var uplinkUid = server.EntMan.SpawnEntity("LoneOpsUplink225TC", map.GridCoords);
            var store = server.EntMan.GetComponent<StoreComponent>(uplinkUid);

            Assert.Multiple(() =>
            {
                Assert.That(loneOperative, Is.Not.EqualTo(originalBody));
                Assert.That(server.EntMan.HasComponent<NukeOperativeComponent>(loneOperative), Is.True);
                Assert.That(roleSystem.MindHasRole<NukeopsRoleComponent>(loneMind.Owner), Is.True);
                Assert.That(factions.IsMember(loneOperative, SyndicateFaction), Is.True);
                Assert.That(server.EntMan.GetComponent<MetaDataComponent>(uplinkUid).EntityPrototype?.ID,
                    Is.EqualTo("LoneOpsUplink225TC"));
                Assert.That(store.Balance[(ProtoId<CurrencyPrototype>) "Telecrystal"],
                    Is.EqualTo(FixedPoint2.New(225)));
                Assert.That(loneMind.Comp.Objectives, Is.Not.Empty);
            });

            server.System<SharedMapSystem>().DeleteMap(map.MapId);
        });
    }
}
