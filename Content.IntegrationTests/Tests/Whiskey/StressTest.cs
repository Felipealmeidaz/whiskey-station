// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using Content.Server._Whiskey.Stress;
using Content.IntegrationTests.Fixtures;
using Content.Shared._Whiskey.Stress;
using Content.Shared.StatusEffectNew;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Whiskey;

/// <summary>
/// Cobre a medida de estresse: o valor tem que ficar preso entre 0 e 100, cair
/// sozinho, e o efeito de faixa tem que ir embora quando a pessoa se acalma.
/// </summary>
[TestFixture]
public sealed class StressTest : GameTest
{
    /// <summary>
    /// Os três efeitos de faixa precisam existir **como entidade**.
    ///
    /// Isto existe por causa de um erro real: o `StatusEffectSlowdown` do
    /// repositório é `abstract: true`, e o engine recusa com "Attempted to
    /// resolve invalid EntProtoId". O jogo continuava rodando, o embaçado
    /// funcionava, e o servidor lançava exceção doze vezes em silêncio.
    ///
    /// O teste antigo não pegou porque só chegava no primeiro limiar. Este
    /// confere os três sem depender de encenar estresse alto.
    /// </summary>
    [Test]
    public async Task OsTresEfeitosDeFaixaExistemComoEntidade()
    {
        var server = Server;
        var protos = server.ProtoMan;
        var padrao = new StressComponent();

        Assert.Multiple(() =>
        {
            foreach (var (nome, proto) in new[]
                     {
                         ("leve", padrao.MildEffect),
                         ("médio", padrao.MediumEffect),
                         ("alto", padrao.HighEffect),
                     })
            {
                Assert.That(protos.HasIndex<EntityPrototype>(proto), Is.True,
                    $"o efeito da faixa {nome}, {proto}, não existe como entidade");

                Assert.That(protos.Index<EntityPrototype>(proto).Abstract, Is.False,
                    $"o efeito da faixa {nome}, {proto}, é abstract e não pode ser aplicado");
            }
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task ValorFicaPresoEntreZeroECem()
    {
        var pair = Pair;
        var server = Server;
        var mapa = await pair.CreateTestMap();
        var sistema = server.System<StressSystem>();

        EntityUid pessoa = default;
        await server.WaitPost(() =>
        {
            pessoa = server.EntMan.SpawnAtPosition("MobHuman", mapa.GridCoords);
            server.EntMan.AddComponent<StressComponent>(pessoa);
            sistema.Adicionar(pessoa, 500f);
        });

        Assert.That(server.EntMan.GetComponent<StressComponent>(pessoa).Current, Is.EqualTo(100f),
            "somar demais não pode passar de 100");

        await server.WaitPost(() => sistema.Adicionar(pessoa, -500f));

        Assert.That(server.EntMan.GetComponent<StressComponent>(pessoa).Current, Is.EqualTo(0f),
            "subtrair demais não pode ficar negativo");
    }

    /// <summary>
    /// O risco que este teste existe para pegar: o
    /// <c>TryAddStatusEffectDuration</c> **soma** tempo ao efeito que já existe.
    /// Se o sistema usasse ele, chamar de segundo em segundo empilharia duração
    /// para sempre e a pessoa continuaria embaçada muito depois de ter se
    /// acalmado. O certo é o <c>TryUpdateStatusEffectDuration</c>, que define.
    ///
    /// Aqui o estresse é posto no talo, deixado escorrer, e no fim o efeito não
    /// pode mais estar lá.
    /// </summary>
    [Test]
    public async Task EfeitoDaFaixaSaiQuandoOEstresseCai()
    {
        var pair = Pair;
        var server = Server;
        var mapa = await pair.CreateTestMap();
        var sistema = server.System<StressSystem>();
        var efeitos = server.System<StatusEffectsSystem>();

        EntityUid pessoa = default;
        await server.WaitPost(() =>
        {
            pessoa = server.EntMan.SpawnAtPosition("MobHuman", mapa.GridCoords);
            var estresse = server.EntMan.AddComponent<StressComponent>(pessoa);
            // Queda rápida, para o teste não levar um minuto e meio.
            estresse.DecayPerSecond = 40f;
            sistema.Adicionar(pessoa, 100f);
        });

        // O sistema mexe uma vez por segundo, então precisa de tempo real.
        await RunSeconds(2);

        Assert.That(efeitos.HasStatusEffect(pessoa, "StatusEffectBlurryVision"), Is.True,
            "no talo do estresse a visão tem que estar embaçada");

        await RunSeconds(8);

        var estresseFinal = server.EntMan.GetComponent<StressComponent>(pessoa).Current;
        Assert.That(estresseFinal, Is.EqualTo(0f), "o estresse tinha que ter escorrido todo");

        Assert.That(efeitos.HasStatusEffect(pessoa, "StatusEffectBlurryVision"), Is.False,
            "com o estresse zerado o efeito não pode continuar de pé, senão a duração empilhou");
    }
}
