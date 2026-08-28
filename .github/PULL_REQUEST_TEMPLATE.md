<!-- Diretrizes: https://github.com/Whiskey-Station/Whiskey-Station-14/blob/master/CONTRIBUTING.pt-BR.md -->
<!-- ATENÇÃO: todo código enviado para este repositório é licenciado como AGPL-3.0-or-later. -->

## Sobre a PR
<!-- O que você mudou? -->

## Motivo e balanceamento
<!-- Por que a mudança existe, e como ela afeta o jogo.
Se mexe em antagonista, combate, economia, ciência ou progressão, explique o
contrajogo: o que a tripulação pode fazer contra isso. -->

## Plano de teste
<!-- Como você testou, e como outra pessoa reproduz o teste.
"Compila" não é teste. Diga o que você fez no jogo e o que aconteceu. -->

## Mídia
<!-- Anexe imagem ou vídeo se a PR muda algo visível no jogo.
Correção pequena e refatoração estão dispensadas. -->

## Requisitos
<!-- Marque com X dentro dos colchetes, sem espaço: [X] -->
- [ ] Li e estou seguindo as [diretrizes de contribuição](https://github.com/Whiskey-Station/Whiskey-Station-14/blob/master/CONTRIBUTING.pt-BR.md)
- [ ] Testei esta PR e escrevi como reproduzir o teste
- [ ] Anexei mídia, ou a mudança não precisa de demonstração no jogo

### Se a PR tem código ou conteúdo novo
- [ ] Arquivos novos estão em pasta própria (`_Whiskey`, `_Trauma`), não em caminho herdado
- [ ] Arquivos novos têm cabeçalho `SPDX-License-Identifier`
- [ ] Alterações em arquivo herdado estão marcadas com `// Trauma` ou `<Trauma>`
- [ ] Não usei `frameTime` para lógica de jogo, usei `IGameTiming.CurTime`
- [ ] Componente novo em `Shared` está networkado, ou há razão registrada para não estar

### Se a PR é um port de outro fork
- [ ] Verifiquei a licença da origem e ela é compatível
- [ ] Os arquivos portados mantêm a atribuição original
- [ ] Conferi que a funcionalidade não depende de sistema que este fork não tem

<!-- Não seguir os itens acima pode fazer a PR ser devolvida. -->

## Changelog
<!-- Toda mudança que o jogador percebe precisa de changelog.
Tire o bloco abaixo do comentário para ele valer. O :cl: é obrigatório. -->
<!--
:cl:
- add: Adiciona algo novo
- remove: Remove algo
- tweak: Ajusta algo existente
- fix: Corrige um problema
-->
