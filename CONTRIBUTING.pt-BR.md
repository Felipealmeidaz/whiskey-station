# Como contribuir com a Whiskey Station

Este documento vale para a Whiskey. O [`CONTRIBUTING.md`](CONTRIBUTING.md) em inglês veio do Trauma e continua servindo como referência técnica detalhada, mas quando os dois discordarem, vale este.

Se você é Maintainer, leia também o Regulamento de Maintainers, que trata de responsabilidade, revisão e uso de permissão.

## O básico

Não precisa saber programar para contribuir. Boa parte do conteúdo do jogo é YAML, que é arquivo de texto com uma lista de propriedades: item, receita, roupa, cargo e mapa saem sem escrever uma linha de C#. Tradução, teste e documentação também são contribuição.

Não mande código que você não consegue explicar. Ferramenta de IA pode ajudar, mas quem responde pelo código é você, e vão te pedir para explicar o que ele faz.

## Abrindo uma PR

Preencha o template. Ele existe para você mesmo pegar os erros que mais voltam em revisão, antes de alguém ter que apontar.

Diga **como testou**. "Compila" não é teste. Descreva o que você fez no jogo e o que aconteceu, de um jeito que outra pessoa consiga repetir.

Anexe imagem ou vídeo se a mudança é visível no jogo.

Quando pedirem mudanças, faça, marque os comentários como resolvidos e **teste de novo**. Testar depois de mudar é onde mais aparece bug.

PR grande é mais difícil de revisar e mais difícil de reverter. Quando der para dividir, divida.

## Onde colocar arquivo

Conteúdo novo da Whiskey vai em pasta própria, `_Whiskey`. Conteúdo de outra origem vai na pasta daquela origem.

Quando precisar mexer em arquivo herdado, marque a alteração:

```csharp
// Trauma - explicação curta        // para uma linha

// <Trauma>                          // para bloco
...
// </Trauma>
```

Em YAML use `#` no lugar de `//`. Isso existe para a sincronização futura com o upstream não virar caos.

Arquivo novo precisa de cabeçalho de licença:

```csharp
// SPDX-License-Identifier: AGPL-3.0-or-later
```

## Localização

O inglês é o idioma canônico. Conteúdo novo precisa da chave em `Resources/Locale/en-US`, mesmo que você só vá jogar em português.

Tradução para pt-BR é bem-vinda e não substitui o inglês. Nada pode depender da tradução para funcionar.

Cuidado com o que **não** se traduz: nome de comando de console, palavra de encantamento, símbolo de unidade e gíria que o jogo procura no chat. Se traduzir, quebra.

## Código

Use as APIs do projeto em vez de acessar o `EntityManager` direto: `TryComp`, `HasComp`, `EnsureComp`.

Não use `frameTime` para lógica de jogo. Use `IGameTiming.CurTime` e compare com um instante guardado. Frame varia, e a lógica passa a depender do computador de quem está jogando.

No `EntityQueryEnumerator`, ponha primeiro o componente menos comum. Ele é o que corta a busca.

Componente que pode ser compartilhado vai em `Shared` e precisa ser networkado, salvo razão registrada. Interação deve ser predita, salvo razão registrada.

## Ports de outro fork

Antes de portar, verifique a licença da origem. Nem todo fork usa licença livre, e alguns têm contrato próprio que proíbe uso público. Repositório marcado como `Other` ou `NOASSERTION` no GitHub quase sempre é isso, e aí é preciso ler o arquivo de licença inteiro.

Mantenha a atribuição original nos arquivos portados.

Portar algo que funciona em outro projeto não prova que funciona aqui. Confira se a funcionalidade não depende de sistema, componente ou evento que este fork não tem.

## Changelog

Mudança que o jogador percebe precisa de changelog. Refatoração e mudança interna são dispensadas.

O bloco vai no corpo da PR, fora do comentário, e precisa do `:cl:` para o bot reconhecer.

## Testando

Rode o `YAML Linter` e a build em Release antes de abrir. Tem analisador que só reprova em Release.

Depois de trocar de branch, compile a solução inteira. Cliente e servidor compartilham tipos que trafegam pela rede, e binário de branches diferentes se desentende com erro que não menciona a causa.

Clone sempre com `--recursive`. O motor é submódulo, e sem ele a build falha com um erro que não fala nada sobre submódulo.
