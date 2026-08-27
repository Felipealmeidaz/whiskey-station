// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

using System.Linq;
using Robust.Shared.Prototypes;
using Content.Shared.Bible.Components;
using Content.Trauma.Common.Language.Systems;
using Content.Shared.Gibbing;
using Content.Server.Cuffs;
using Content.Server.Mind;
using Content.Goobstation.Common.Religion;
using Content.Shared.Stunnable;
using Content.Server.WhiteDream.BloodCult.Gamerule;
using Content.Server.WhiteDream.BloodCult.Runes.Revive;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Mindshield;
using Content.Shared.Mobs.Systems;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.Damage.Systems;
using Content.Server.Popups;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using StatusEffectsNewSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem; // Trauma

namespace Content.Server.WhiteDream.BloodCult.Runes.Offering;

public sealed partial class CultRuneOfferingSystem : EntitySystem
{
    // Trauma - muting moved to the new status effect system
    private static readonly EntProtoId MutedEffect = "StatusEffectMuted";

    [Dependency] private BloodCultRuleSystem _bloodCultRule = default!;
    [Dependency] private CommonLanguageSystem _language = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private CuffableSystem _cuffable = default!;
    [Dependency] private CultRuneBaseSystem _cultRune = default!;
    [Dependency] private CultRuneReviveSystem _cultRuneRevive = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private MindShieldSystem _mindShield = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StatusEffectsNewSystem _statusEffectsNew = default!; // Trauma
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CultRuneOfferingComponent, TryInvokeCultRuneEvent>(OnOfferingRuneInvoked);
    }

    private void OnOfferingRuneInvoked(Entity<CultRuneOfferingComponent> rune, ref TryInvokeCultRuneEvent args)
    {
        var possibleTargets = _cultRune.GetTargetsNearRune(
            rune,
            rune.Comp.OfferingRange,
            entity => HasComp<BloodCultistComponent>(entity));

        if (possibleTargets.Count == 0)
        {
            args.Cancel();
            return;
        }

        var target = possibleTargets.First();
        if (!TryOffer(rune, target, args.User, args.Invokers.Count))
            args.Cancel();
    }

    private bool TryOffer(Entity<CultRuneOfferingComponent> rune, EntityUid target, EntityUid user, int invokersTotal)
    {
        // WhiteDream - Nar'Sie's marked offering always costs three of us, alive or dead. Any other
        // corpse only needs one.
        if (_bloodCultRule.IsTarget(target))
            return TrySacrifice(rune, target, invokersTotal);

        if (_mobState.IsDead(target))
        {
            Sacrifice(rune, target);
            return true;
        }

        // WhiteDream - MindShieldComponent lives on the implant/clothing, never on the person, so
        // HasComp here was always false and mindshielded crew were being converted. MindShieldSystem
        // says outright: never look for the component, ask it instead.
        if (!_mind.TryGetMind(target, out _, out _) || _bloodCultRule.IsTarget(target) ||
            HasComp<BibleUserComponent>(target) || _mindShield.IsShielded(target))
            return TrySacrifice(rune, target, invokersTotal);

        return TryConvert(rune, target, user, invokersTotal);
    }

    private bool TrySacrifice(Entity<CultRuneOfferingComponent> rune, EntityUid target, int invokersAmount)
    {
        if (invokersAmount < rune.Comp.AliveSacrificeInvokersAmount)
        {
            // WhiteDream - it used to just cancel in silence, so nothing at all seemed to happen.
            _popup.PopupEntity(
                Loc.GetString("cult-offering-need-invokers",
                    ("current", invokersAmount),
                    ("required", rune.Comp.AliveSacrificeInvokersAmount)),
                rune,
                PopupType.MediumCaution);

            return false;
        }

        Sacrifice(rune, target);
        return true;
    }

    private bool TryConvert(Entity<CultRuneOfferingComponent> rune, EntityUid target, EntityUid user, int invokersTotal)
    {
        if (invokersTotal < rune.Comp.ConvertInvokersAmount)
        {
            _popup.PopupEntity(
                Loc.GetString("cult-offering-need-invokers",
                    ("current", invokersTotal),
                    ("required", rune.Comp.ConvertInvokersAmount)),
                rune,
                PopupType.MediumCaution);

            return false;
        }

        _cultRuneRevive.AddCharges(rune, rune.Comp.ReviveChargesPerOffering);
        Convert(rune, target, user);
        return true;
    }

    private void Sacrifice(Entity<CultRuneOfferingComponent> rune, EntityUid target)
    {
        // Whiskey - the marked one only counts once they are given on the rune. Before this the
        // objective completed on death alone, so a body in the morgue unlocked the rending rune.
        _bloodCultRule.MarkOfferingSacrificed(target);

        _cultRuneRevive.AddCharges(rune, rune.Comp.ReviveChargesPerOffering);

        // WhiteDream - the veil takes its due.
        _audio.PlayPvs(rune.Comp.SacrificeSound, rune, AudioParams.Default.WithVolume(2f));

        var transform = Transform(target);

        if (!_mind.TryGetMind(target, out var mindId, out _))
            Spawn(rune.Comp.SoulShardGhostProto, transform.Coordinates);
        else
        {
            var shard = Spawn(rune.Comp.SoulShardProto, transform.Coordinates);
            _mind.TransferTo(mindId, shard);
            _mind.UnVisit(mindId);
            _language.UpdateEntityLanguages(shard);
        }

        _gibbing.Gib(target);
    }

    private void Convert(Entity<CultRuneOfferingComponent> rune, EntityUid target, EntityUid user)
    {
        _bloodCultRule.Convert(target);
        _stun.TryAddStunDuration(target, TimeSpan.FromSeconds(2f));
        if (TryComp(target, out CuffableComponent? cuffs) && cuffs.Container.ContainedEntities.Count >= 1)
        {
            var lastAddedCuffs = cuffs.Container.ContainedEntities[^1];
            _cuffable.Uncuff(target, user, lastAddedCuffs);
        }

        // Trauma - muting moved to the new status effect system, the old call silently did nothing.
        _statusEffectsNew.TryRemoveStatusEffect(target, MutedEffect);
        _damageable.TryChangeDamage(target, rune.Comp.ConvertHealing);

        _audio.PlayPvs(rune.Comp.ConvertSound, rune, AudioParams.Default.WithVolume(1f));
    }
}
