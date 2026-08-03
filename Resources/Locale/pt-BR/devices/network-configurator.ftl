# SPDX-FileCopyrightText: 2026 punkzebub <punkzebub@gmail.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

# Mensagens

network-configurator-device-saved = O dispositivo de rede {$device}, no endereço {$address}, foi salvo!
network-configurator-device-failed = Não foi possível salvar o dispositivo de rede {$device}: nenhum endereço atribuído!
network-configurator-too-many-devices = Este configurador já armazena dispositivos demais!
network-configurator-update-ok = Armazenamento de dispositivos atualizado.
network-configurator-device-already-saved = O dispositivo de rede {$device} já está salvo.
network-configurator-device-access-denied = Acesso negado!
network-configurator-link-mode-started = Vinculação iniciada com o dispositivo: {$device}
network-configurator-link-mode-stopped = Vinculação encerrada.
network-configurator-mode-link = Vincular
network-configurator-mode-list = Lista
network-configurator-switched-mode = Modo alterado para: {$mode}

# Verbos
network-configurator-save-device = Salvar dispositivo
network-configurator-configure = Configurar
network-configurator-switch-mode = Alternar modo
network-configurator-link-defaults = Vincular padrões
network-configurator-start-link = Iniciar vinculação
network-configurator-link = Vincular

# Interface
network-configurator-title-saved-devices = Dispositivos salvos
network-configurator-title-device-configuration = Configuração do dispositivo
network-configurator-ui-clear-button = Limpar
network-configurator-ui-count-label = { $count ->
    [one] {$count} dispositivo
   *[other] {$count} dispositivos
}

network-configurator-text-set = Definir
network-configurator-text-add = Adicionar
network-configurator-text-clear = Limpar
network-configurator-text-copy = Copiar
network-configurator-text-show = Exibir

# Dicas
network-configurator-tooltip-set = Substitui a lista de dispositivos de destino
network-configurator-tooltip-add = Adiciona itens à lista de dispositivos de destino
network-configurator-tooltip-edit = Edita a lista de dispositivos de destino
network-configurator-tooltip-clear = Limpa a lista de dispositivos de destino
network-configurator-tooltip-copy = Copia a lista de dispositivos de destino para a ferramenta em mãos
network-configurator-tooltip-show = Exibe uma visualização holográfica da lista de dispositivos de destino

# Exame
network-configurator-examine-mode-link = [color=red]Vincular[/color]
network-configurator-examine-mode-list = [color=green]Lista[/color]
network-configurator-examine-current-mode = Modo atual: {$mode}
network-configurator-examine-switch-modes = Pressione {$key} para alternar o modo

# Estado do item
network-configurator-item-status-label = Modo: {$mode}
    Alternar: {$keybinding}

# Comando
cmd-clearnetworklinkoverlays-desc = Limpa todas as sobreposições de vínculos de rede.
cmd-clearnetworklinkoverlays-help = Uso: clearnetworklinkoverlays
