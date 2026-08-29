// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Mind;
using Content.Shared._Whiskey.Stress;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Whiskey.Stress;

/// <summary>
/// Dispara os episódios do <see cref="PeriodicStressComponent"/>.
/// </summary>
public sealed partial class PeriodicStressSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
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

        if (ent.Comp.Message is not { } chave)
            return;

        if (!_mind.TryGetMind(ent, out _, out var mente) || mente.UserId is not { } usuario)
            return;

        if (!_player.TryGetSessionById(usuario, out var sessao))
            return;

        var frase = Loc.GetString(chave);

        // Popup em vez de chat, e só para quem tem o traço. Ver o comentário no
        // HallucinationSystem sobre a sobrecarga de três argumentos.
        _popup.PopupEntity(frase, ent, ent, PopupType.LargeCaution);
    }
}
