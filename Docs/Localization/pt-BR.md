<!-- SPDX-FileCopyrightText: 2026 punkzebub <punkzebub@gmail.com> -->
<!-- SPDX-License-Identifier: AGPL-3.0-or-later -->

# Localização PT-BR

O idioma padrão da Whiskey Station é `pt-BR`. A base em inglês, em
`Resources/Locale/en-US`, continua sendo o fallback enquanto a cobertura não
for total.

## Escopo

Traduzimos qualquer texto exibido para jogadores ou equipe: interfaces,
mensagens de jogo, protótipos, comandos, guias e conteúdo administrativo.
Não traduzimos identificadores, nomes de arquivos, rotas de API, nomes de
componentes, chaves de configuração nem texto de log destinado a diagnóstico.

## Estrutura

Cada arquivo em `Resources/Locale/pt-BR` deve espelhar seu correspondente em
`Resources/Locale/en-US`. Preserve o ID da mensagem, atributos, variáveis
(`$variavel`), marcação RichText e expressões Fluent; traduza apenas o texto.
Arquivos da família `_Trauma` permanecem em `_Trauma` também dentro de `pt-BR`.

## Convenções

- Use português brasileiro natural e consistente; prefira imperativo em botões
  (`Salvar`, `Cancelar`, `Desconectar`).
- Mantenha nomes próprios e siglas de jogo quando forem reconhecíveis
  (`NanoTrasen`, `OOC`, `LOOC`, `PDA`).
- Não use `MAKEPLURAL` ou `MANY` em novas mensagens PT-BR. Faça a pluralização
  com seletores Fluent, pois a regra inglesa não é válida para português.
- Antes de enviar uma etapa, execute
  `python3 Tools/localization/audit_ptbr.py` e corrija variáveis divergentes.
  Use `--verbose` para listar cada pendência e `--fail-on-missing` na etapa de
  cobertura total.

## Critério de qualidade contextual

Uma mensagem não é considerada traduzida apenas por estar em português. Antes
de incluí-la, identifique onde ela aparece, quem a lê e qual ação ou estado ela
descreve. Priorize o comportamento real definido pelo código e pelos
protótipos desta fork; use a documentação oficial do SS14 para conferir a
função do sistema quando o contexto não estiver explícito no repositório.

- Preserve nomes de universo e siglas como `NanoTrasen`, `CentComm`,
  `Syndicate`, `ERT`, `PDA`, `OOC` e `LOOC`; traduza a descrição ao redor
  deles.
- Diferencie texto diegético (equipamentos, rádio, cargos e documentos) de
  interface fora de personagem. O primeiro deve soar como parte da estação; o
  segundo deve ser claro, curto e acionável.
- Mantenha termos técnicos que correspondem a mecânicas, como `atmos`,
  `EVA`, `PVS`, `HRTF` e `VSync`, quando a expansão em português prejudicar a
  identificação do jogador. Explique-os somente em dicas ou guias quando o
  original também o fizer.
- Revise frases com gênero, plural, nomes de entidades, formatação RichText e
  seletores Fluent no contexto da chamada. Não transfira construções
  gramaticais inglesas para o português.
- Para cada lote, faça revisão terminológica, auditoria automática e uma
  revisão visual/funcional no cliente antes de qualquer implantação.

## Ordem de trabalho

1. Entrada, lobby, menus, HUD e chat.
2. Interações, inventário, máquinas, medicina, engenharia e pesquisa.
3. Cargos, regras, objetivos, antagonistas, eventos e conteúdo narrativo.
4. Administração, comandos, ferramentas e guia.
5. Literais visíveis em C# e XAML, com uma chave Fluent para cada texto.
