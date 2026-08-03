# SPDX-FileCopyrightText: 2026 punkzebub <punkzebub@gmail.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

## Interface
cargo-console-menu-title = Console de requisições da Carga
cargo-console-menu-flavor-left = Peça ainda mais caixas de pizza que o normal!
cargo-console-menu-flavor-right = v2.1
cargo-console-menu-account-name-label = Conta:{" "}
cargo-console-menu-account-name-none-text = Nenhuma
cargo-console-menu-account-name-format = [bold][color={$color}]{$name}[/color][/bold] [font="Monospace"]\[{$code}\][/font]
cargo-console-menu-shuttle-name-label = Nome da nave:{" "}
cargo-console-menu-shuttle-name-none-text = Nenhum
cargo-console-menu-points-label = Saldo:{" "}
cargo-console-menu-points-amount = ${$amount}
cargo-console-menu-shuttle-status-label = Estado da nave:{" "}
cargo-console-menu-shuttle-status-away-text = Ausente
cargo-console-menu-order-capacity-label = Capacidade de pedidos:{" "}
cargo-console-menu-order-capacity-number = {$count}/{$capacity}
cargo-console-menu-call-shuttle-button = Ativar telepad
cargo-console-menu-permissions-button = Permissões
cargo-console-menu-categories-label = Categorias:{" "}
cargo-console-menu-search-bar-placeholder = Pesquisar
cargo-console-menu-requests-label = Solicitações
cargo-console-menu-orders-label = Pedidos
cargo-console-menu-populate-categories-all-text = Todas
cargo-console-menu-order-row-title = {$productName} (x{$orderAmount} por {$orderPrice}$)
cargo-console-menu-populate-orders-cargo-order-row-product-name-text = Solicitado por: {$orderRequester} de [color={$accountColor}]{$account}[/color]
cargo-console-menu-order-row-product-description = Motivo: {$orderReason}
cargo-console-menu-order-row-button-approve = Aprovar
cargo-console-menu-order-row-button-cancel = Cancelar
cargo-console-menu-order-row-alerts-reason-absent = Motivo não informado
cargo-console-menu-order-row-alerts-requester-unknown = Desconhecido
cargo-console-menu-tab-title-orders = Pedidos
cargo-console-menu-tab-title-funds = Transferências
cargo-console-menu-account-action-transfer-limit = [bold]Limite de transferência:[/bold] ${$limit}
cargo-console-menu-account-action-transfer-limit-unlimited-notifier = [color=gold](Ilimitado)[/color]
cargo-console-menu-account-action-select = [bold]Operação da conta:[/bold]
cargo-console-menu-account-action-amount = [bold]Valor:[/bold] $
cargo-console-menu-account-action-button = Transferir
cargo-console-menu-toggle-account-lock-button = Alternar limite de transferência
cargo-console-menu-account-action-option-withdraw = Sacar em espécie
cargo-console-menu-account-action-option-transfer = Transferir fundos para {$code}

# Pedidos
cargo-console-order-not-allowed = Acesso não permitido
cargo-console-station-not-found = Nenhuma estação disponível
cargo-console-invalid-product = ID de produto inválido
cargo-console-too-many = Pedidos aprovados em excesso
cargo-console-snip-snip = Pedido reduzido à capacidade disponível
cargo-console-insufficient-funds = Fundos insuficientes (necessários {$cost})
cargo-console-unfulfilled = Sem espaço para atender ao pedido
cargo-console-trade-station = Enviado para {$destination}
cargo-console-unlock-approved-order-broadcast = [bold]{$productName} x{$orderAmount}[/bold], ao custo de [bold]{$cost}[/bold], foi aprovado por [bold]{$approver}[/bold]
cargo-console-fund-withdraw-broadcast = [bold]{$name} sacou {$amount} spesos de {$name1} \[{$code1}\]
cargo-console-fund-transfer-broadcast = [bold]{$name} transferiu {$amount} spesos de {$name1} \[{$code1}\] para {$name2} \[{$code2}\][/bold]
cargo-console-fund-transfer-user-unknown = Desconhecido
cargo-console-paper-reason-default = Nenhum
cargo-console-paper-approver-default = Desconhecido
cargo-console-paper-print-name = Pedido nº {$orderNumber}
cargo-console-paper-print-text = [head=2]Pedido nº {$orderNumber}[/head]
    {"[bold]Item:[/bold]"} {$itemName} (x{$orderQuantity})
    {"[bold]Solicitado por:[/bold]"} {$requester}

    {"[head=3]Informações do pedido[/head]"}
    {"[bold]Pagador[/bold]:"} {$account} [font="Monospace"]\[{$accountcode}\][/font]
    {"[bold]Aprovado por:[/bold]"} {$approver}
    {"[bold]Motivo:[/bold]"} {$reason}

# Console da nave de Carga
cargo-shuttle-console-menu-title = Console da nave de Carga
cargo-shuttle-console-station-unknown = Desconhecida
cargo-shuttle-console-shuttle-not-found = Não encontrada
cargo-shuttle-console-organics = Formas de vida orgânica detectadas na nave
cargo-no-shuttle = Nenhuma nave de Carga encontrada!

# Console de distribuição de verbas
cargo-funding-alloc-console-menu-title = Console de distribuição de verbas
cargo-funding-alloc-console-label-account = [bold]Conta[/bold]
cargo-funding-alloc-console-label-code = [bold] Código [/bold]
cargo-funding-alloc-console-label-balance = [bold] Saldo [/bold]
cargo-funding-alloc-console-label-cut = [bold] Divisão da receita (%) [/bold]
cargo-funding-alloc-console-label-primary-cut = Parcela da Carga sobre verbas de fontes sem cofre (%):
cargo-funding-alloc-console-label-lockbox-cut = Parcela da Carga sobre vendas de cofres (%):
cargo-funding-alloc-console-label-help-non-adjustible = A Carga recebe {$percent}% do lucro de vendas sem cofre. O restante é dividido conforme abaixo:
cargo-funding-alloc-console-label-help-adjustible = As verbas restantes de fontes sem cofre são distribuídas conforme abaixo:
cargo-funding-alloc-console-button-save = Salvar alterações
cargo-funding-alloc-console-label-save-fail = [bold]Divisão de receita inválida![/bold] [color=red]({$pos ->
    [1] +
    *[-1] -
}{$val}%)[/color]

# Comprovante de aquisição
cargo-acquisition-slip-body = [head=3]Detalhes do bem[/head]
    {"[bold]Produto:[/bold]"} {$product}
    {"[bold]Descrição:[/bold]"} {$description}
    {"[bold]Custo unitário:[/bold]"} ${$unit}
    {"[bold]Quantidade:[/bold]"} {$amount}
    {"[bold]Custo:[/bold]"} ${$cost}

    {"[head=3]Detalhes da compra[/head]"}
    {"[bold]Solicitante:[/bold]"} {$orderer}
    {"[bold]Motivo:[/bold]"} {$reason}
