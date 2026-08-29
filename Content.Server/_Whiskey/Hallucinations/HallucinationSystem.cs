// SPDX-FileCopyrightText: 2026 Zequinza <felipe828218@gmail.com>
// SPDX-FileCopyrightText: 2026 Whiskey Station Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Chat.Managers;
using Content.Server.Mind;
using Content.Server.Traits.Assorted;
using Content.Shared._Whiskey.Hallucinations;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Whiskey.Hallucinations;

/// <summary>
/// Toca os canais do <see cref="HallucinationComponent"/>.
///
/// O canal de som não é tocado aqui: ele é entregue à paracusia, que já resolve
/// som posicional falso do lado do cliente. Este sistema só liga e desliga.
/// </summary>
public sealed partial class HallucinationSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private ParacusiaSystem _paracusia = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        // ComponentStartup, e não MapInitEvent. O TraitSystem responde a
        // PlayerSpawnCompleteEvent e faz AddComponents numa entidade que já
        // nasceu, então MapInit não dispara nesse caminho e o traço não faria
        // nada. É o mesmo evento que a paracusia usa do lado do cliente.
        SubscribeLocalEvent<HallucinationComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HallucinationComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<HallucinationComponent> ent, ref ComponentStartup args)
    {
        LigarSom(ent);
        AgendarProximaFala(ent);
    }

    /// <summary>
    /// Põe e configura a paracusia, se esta alucinação tem som.
    ///
    /// O <c>EnsureComp</c> devolve verdadeiro quando o componente **já
    /// existia**. Quem já tinha paracusia por traço próprio fica com a
    /// configuração dele, e não é tocado nem aqui nem no desligamento.
    /// </summary>
    private void LigarSom(Entity<HallucinationComponent> ent)
    {
        if (ent.Comp.Sounds is not { } sons || ent.Comp.PosParacusia)
            return;

        if (EnsureComp<ParacusiaComponent>(ent, out var paracusia))
            return;

        ent.Comp.PosParacusia = true;
        _paracusia.SetSounds(ent, sons, paracusia);
        _paracusia.SetTime(ent, ent.Comp.MinTimeBetweenSounds, ent.Comp.MaxTimeBetweenSounds, paracusia);
        _paracusia.SetDistance(ent, ent.Comp.MaxSoundDistance, paracusia);
    }

    private void OnShutdown(Entity<HallucinationComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.PosParacusia)
            RemComp<ParacusiaComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var agora = _timing.CurTime;

        // O HallucinationComponent é raro, então ele vem primeiro e corta a
        // busca antes de olhar qualquer outra coisa.
        var consulta = EntityQueryEnumerator<HallucinationComponent>();
        while (consulta.MoveNext(out var uid, out var alucinacao))
        {
            // De novo aqui, e não só no startup, porque quem preencher Sounds
            // depois de adicionar o componente ficaria com o canal de som mudo
            // e sem aviso nenhum. É barato: o componente é raro e o
            // LigarSom sai na primeira linha quando não há o que fazer.
            LigarSom((uid, alucinacao));

            if (alucinacao.Messages is null || agora < alucinacao.NextMessageTime)
                continue;

            Falar((uid, alucinacao));
            AgendarProximaFala((uid, alucinacao));
        }
    }

    private void AgendarProximaFala(Entity<HallucinationComponent> ent)
    {
        var espera = _random.NextFloat(ent.Comp.MinTimeBetweenMessages, ent.Comp.MaxTimeBetweenMessages);
        ent.Comp.NextMessageTime = _timing.CurTime + TimeSpan.FromSeconds(espera);
    }

    /// <summary>
    /// Manda uma frase falsa, e manda só para quem alucina.
    ///
    /// A frase nunca passa pelo ChatSystem, então ninguém mais escuta, não sai
    /// no rádio e não entra no log de fala. Do ponto de vista do servidor, não
    /// houve fala nenhuma.
    /// </summary>
    private void Falar(Entity<HallucinationComponent> ent)
    {
        if (ent.Comp.Messages is not { } listaId)
            return;

        if (!_proto.TryIndex(listaId, out var lista) || lista.Values.Count == 0)
            return;

        if (!_mind.TryGetMind(ent, out _, out var mente) || mente.UserId is not { } usuario)
            return;

        if (!_player.TryGetSessionById(usuario, out var sessao))
            return;

        // O Pick já devolve a frase traduzida, então não cabe Loc.GetString aqui.
        var frase = _random.Pick(lista);

        // Um canal ou o outro, nunca os dois. Mandar nos dois mostrava a mesma
        // frase duas vezes seguidas na tela, o que estraga o efeito: voz na
        // cabeça repetida vira mensagem de sistema.
        if (ent.Comp.Popup)
        {
            // O terceiro argumento é o destinatário. A sobrecarga de dois
            // mostraria para todo mundo por perto, e a estação inteira leria a
            // voz que existe só na cabeça de uma pessoa.
            _popup.PopupEntity(frase, ent, ent, PopupType.LargeCaution);
            return;
        }

        var embrulho = Loc.GetString("chat-manager-server-wrap-message", ("message", frase));

        _chat.ChatMessageToOne(
            ChatChannel.Server,
            frase,
            embrulho,
            default,
            false,
            sessao.Channel);
    }
}
