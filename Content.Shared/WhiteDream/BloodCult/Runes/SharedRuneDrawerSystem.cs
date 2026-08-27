// SPDX-License-Identifier: AGPL-3.0-or-later
// Blood Cult: ported from WWhiteDreamProject/wwdpublic. See Content.Shared/WhiteDream/BloodCult/ATTRIBUTION.md

using System.Linq;
using Content.Trauma.Common.RadialSelector;
using Content.Shared.UserInterface;
using Content.Shared.WhiteDream.BloodCult.BloodCultist;
using Content.Shared.WhiteDream.BloodCult.Runes;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.WhiteDream.BloodCult.Runes;

public sealed partial class SharedRuneDrawerSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _protoManager = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private BloodCultPopulationSystem _cultPopulation = default!; // Whiskey
    [Dependency] private INetManager _net = default!; // Whiskey

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RuneDrawerComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);
    }

    private void OnBeforeUiOpen(Entity<RuneDrawerComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUi(ent);
    }

    public void UpdateUi(EntityUid uid)
    {
        // Whiskey - the crew head count comes from ActorComponent, which is not networked, so
        // on the client it reads zero and every percentage rune would pass the filter. The server owns
        // the BUI state anyway, so let it be the one that builds the list.
        if (!_net.IsServer)
            return;

        var availableRunes = new List<ProtoId<RuneSelectorPrototype>>();
        var totalCultists = CountCultists();

        foreach (var runeSelector in _protoManager.EnumeratePrototypes<RuneSelectorPrototype>().OrderBy(r => r.ID))
        {
            // Whiskey - the requirement can be a share of the crew. This filter runs on both
            // sides, so it has to agree with CultRuneBaseSystem.OnRuneDrawerOpened on the server.
            if (_cultPopulation.GetRequiredCultists(runeSelector) > totalCultists)
                continue;

            availableRunes.Add(runeSelector.ID);
        }

        _ui.SetUiState(uid, RuneDrawerBuiKey.Key, new RuneDrawerMenuState(availableRunes));
    }

    private int CountCultists()
    {
        var count = 0;

        var cultistQuery = EntityQueryEnumerator<BloodCultistComponent>();
        while (cultistQuery.MoveNext(out _, out _))
            count++;

        return count;
    }
}
