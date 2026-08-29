// SPDX-FileCopyrightText: 2026 Zequinza <felipe828218@gmail.com>
// SPDX-FileCopyrightText: 2026 Whiskey Station Contributors
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared._Whiskey.Hallucinations;
using Content.Server.Traits.Assorted;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Whiskey;

/// <summary>
/// Cobre o que o motor de alucinação tem de arriscado, que é mexer num
/// componente que não é dele.
/// </summary>
[TestFixture]
public sealed class HallucinationTest : GameTest
{
    private const string Traco = "Schizophrenia";

    /// <summary>
    /// O traço é aplicado pelo TraitSystem numa entidade que já nasceu, então
    /// o motor precisa reagir a ComponentStartup. Se alguém trocar de volta
    /// para MapInitEvent, este teste cai, e é o ponto: naquele caminho o traço
    /// fica mudo e ninguém percebe olhando o código.
    /// </summary>
    [Test]
    public async Task LigaOSomAoGanharOComponenteDepoisDeNascer()
    {
        var pair = Pair;
        var server = Server;
        var mapa = await pair.CreateTestMap();

        EntityUid pessoa = default;
        await server.WaitPost(() => pessoa = server.EntMan.SpawnAtPosition("MobHuman", mapa.GridCoords));
        await pair.RunTicksSync(5);

        Assert.That(server.EntMan.HasComponent<ParacusiaComponent>(pessoa), Is.False,
            "a pessoa começa sem paracusia, senão o resto do teste não prova nada");

        // De propósito na ordem ruim: adiciona e só depois preenche Sounds.
        // O TraitSystem faz o contrário, copia os dados do YAML antes de
        // adicionar, mas quem mexer por VV ou por outro sistema cai aqui, e
        // nesse caminho o canal de som já ficou mudo em silêncio uma vez.
        await server.WaitPost(() =>
        {
            var alucinacao = server.EntMan.AddComponent<HallucinationComponent>(pessoa);
            alucinacao.Sounds = new SoundCollectionSpecifier("WhiskeyAlucinacaoVozes");
        });
        await pair.RunTicksSync(5);

        Assert.That(server.EntMan.HasComponent<ParacusiaComponent>(pessoa), Is.True,
            "ganhar a alucinação depois de nascer tem que ligar o canal de som");
    }

    /// <summary>
    /// Quem já tinha o traço de paracusia por conta própria não pode perdê-lo
    /// quando a alucinação sai. O EnsureComp devolve verdadeiro quando o
    /// componente já existia, e confundir isso apaga componente dos outros.
    /// </summary>
    [Test]
    public async Task NaoTiraAParacusiaDeQuemJaTinha()
    {
        var pair = Pair;
        var server = Server;
        var mapa = await pair.CreateTestMap();

        EntityUid jaTinha = default;
        EntityUid naoTinha = default;

        var paracusiaSys = server.System<ParacusiaSystem>();

        await server.WaitPost(() =>
        {
            jaTinha = server.EntMan.SpawnAtPosition("MobHuman", mapa.GridCoords);
            naoTinha = server.EntMan.SpawnAtPosition("MobHuman", mapa.GridCoords);

            // Pela API do sistema, e não escrevendo no campo: o componente é
            // [Access(typeof(SharedParacusiaSystem))] e o analisador reprova.
            server.EntMan.AddComponent<ParacusiaComponent>(jaTinha);
            paracusiaSys.SetSounds(jaTinha, new SoundCollectionSpecifier("Paracusia"));
            paracusiaSys.SetTime(jaTinha, 30f, 60f);
            paracusiaSys.SetDistance(jaTinha, 7f);

            foreach (var alvo in new[] { jaTinha, naoTinha })
            {
                var alucinacao = server.EntMan.AddComponent<HallucinationComponent>(alvo);
                alucinacao.Sounds = new SoundCollectionSpecifier("WhiskeyAlucinacaoVozes");
            }
        });
        await pair.RunTicksSync(5);

        Assert.Multiple(() =>
        {
            Assert.That(server.EntMan.HasComponent<ParacusiaComponent>(jaTinha), Is.True);
            Assert.That(server.EntMan.HasComponent<ParacusiaComponent>(naoTinha), Is.True);
        });

        await server.WaitPost(() =>
        {
            server.EntMan.RemoveComponent<HallucinationComponent>(jaTinha);
            server.EntMan.RemoveComponent<HallucinationComponent>(naoTinha);
        });
        await pair.RunTicksSync(5);

        Assert.Multiple(() =>
        {
            Assert.That(server.EntMan.HasComponent<ParacusiaComponent>(jaTinha), Is.True,
                "a paracusia que a pessoa já tinha por traço próprio não pode sumir junto");
            Assert.That(server.EntMan.HasComponent<ParacusiaComponent>(naoTinha), Is.False,
                "a paracusia que a alucinação pôs tem que sair junto com ela");
        });
    }

    /// <summary>
    /// O traço tem que existir e trazer o motor configurado nos dois canais.
    /// Erro de digitação no id do dataset ou da coleção de som passa no linter
    /// e só aparece em jogo, quando não acontece nada.
    /// </summary>
    [Test]
    public async Task OTracoTrazOsDoisCanais()
    {
        var server = Server;
        var protos = server.ProtoMan;

        Assert.That(protos.HasIndex<Content.Shared.Traits.TraitPrototype>(Traco), Is.True);

        var traco = protos.Index<Content.Shared.Traits.TraitPrototype>(Traco);
        var registro = server.EntMan.ComponentFactory.GetComponent<HallucinationComponent>();

        Assert.That(traco.Components.TryGetComponent(
                server.EntMan.ComponentFactory.GetComponentName(registro.GetType()),
                out var bruto),
            Is.True,
            "o traço precisa trazer o HallucinationComponent");

        var alucinacao = (HallucinationComponent) bruto!;

        Assert.Multiple(() =>
        {
            Assert.That(alucinacao.Sounds, Is.Not.Null, "canal de som desligado");
            Assert.That(alucinacao.Messages, Is.Not.Null, "canal de fala desligado");
            Assert.That(alucinacao.MinTimeBetweenMessages, Is.LessThan(alucinacao.MaxTimeBetweenMessages));
            Assert.That(alucinacao.MinTimeBetweenSounds, Is.LessThan(alucinacao.MaxTimeBetweenSounds));
        });

        Assert.That(protos.HasIndex<Content.Shared.Dataset.LocalizedDatasetPrototype>(alucinacao.Messages!.Value),
            Is.True,
            "o dataset de frases apontado pelo traço precisa existir");
    }
}
