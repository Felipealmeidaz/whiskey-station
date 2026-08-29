// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Mind;
using Content.Shared._Whiskey.Stress;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Whiskey.Stress;

/// <summary>
/// Dispara os episódios do <see cref="PeriodicStressComponent"/>.
/// </summary>
public sealed partial class PeriodicStressSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StressSystem _estresse = default!;

    public override void Initialize()
    {
        base.Initialize();

        // ComponentStartup, e não MapInitEvent: o TraitSystem adiciona o
        // componente numa entidade que já nasceu, e naquele caminho o MapInit
        // não dispara. Mesmo motivo do motor de alucinação.
        SubscribeLocalEvent<PeriodicStressComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<PeriodicStressComponent> ent, ref ComponentStartup args)
    {
        Agendar(ent);

        if (ent.Comp.GainMessage is { } aviso)
            MostrarParaODono(ent, Loc.GetString(aviso));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var agora = _timing.CurTime;

        var consulta = EntityQueryEnumerator<PeriodicStressComponent>();
        while (consulta.MoveNext(out var uid, out var periodico))
        {
            if (agora < periodico.NextEpisode)
                continue;

            Disparar((uid, periodico));
            Agendar((uid, periodico));
        }
    }

    private void Agendar(Entity<PeriodicStressComponent> ent)
    {
        var espera = _random.NextFloat(ent.Comp.MinTimeBetween, ent.Comp.MaxTimeBetween);
        ent.Comp.NextEpisode = _timing.CurTime + TimeSpan.FromSeconds(espera);
    }

    private void Disparar(Entity<PeriodicStressComponent> ent)
    {
        _estresse.Adicionar(ent, ent.Comp.Amount);

        if (ent.Comp.Messages is not { } listaId)
            return;

        if (!_proto.TryIndex(listaId, out var lista) || lista.Values.Count == 0)
            return;

        MostrarParaODono(ent, _random.Pick(lista));
    }

    /// <summary>
    /// Mostra o texto só para quem tem o componente.
    ///
    /// A sobrecarga de três argumentos do popup tem o destinatário no terceiro.
    /// A de dois mostraria para todo mundo por perto, e um pensamento que é da
    /// pessoa passaria a ser lido pela estação inteira.
    /// </summary>
    private void MostrarParaODono(EntityUid uid, string texto)
    {
        _popup.PopupEntity(texto, uid, uid, PopupType.LargeCaution);
    }
}
