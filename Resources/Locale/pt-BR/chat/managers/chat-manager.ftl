# SPDX-FileCopyrightText: 2026 punkzebub <punkzebub@gmail.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

### Interface

chat-manager-max-message-length = Sua mensagem excede o limite de {$maxMessageLength} caracteres
chat-manager-ooc-chat-enabled-message = O chat OOC foi ativado.
chat-manager-ooc-chat-disabled-message = O chat OOC foi desativado.
chat-manager-looc-chat-enabled-message = O chat LOOC foi ativado.
chat-manager-looc-chat-disabled-message = O chat LOOC foi desativado.
chat-manager-dead-looc-chat-enabled-message = Jogadores mortos agora podem usar LOOC.
chat-manager-dead-looc-chat-disabled-message = Jogadores mortos não podem mais usar LOOC.
chat-manager-crit-looc-chat-enabled-message = Jogadores em estado crítico agora podem usar LOOC.
chat-manager-crit-looc-chat-disabled-message = Jogadores em estado crítico não podem mais usar LOOC.
chat-manager-admin-ooc-chat-enabled-message = O chat OOC administrativo foi ativado.
chat-manager-admin-ooc-chat-disabled-message = O chat OOC administrativo foi desativado.
chat-manager-dead-chat-enabled-message = O chat dos mortos foi ativado.
chat-manager-dead-chat-disabled-message = O chat dos mortos foi desativado.

chat-manager-max-message-length-exceeded-message = Sua mensagem excedeu o limite de {$limit} caracteres
chat-manager-no-headset-on-message = Você não está usando um fone de ouvido!
chat-manager-no-radio-key = Nenhuma chave de rádio especificada!
chat-manager-no-such-channel = Não existe canal com a chave '{$key}'!
chat-manager-whisper-headset-on-message = Você não pode sussurrar pelo rádio!

# Aspas duplas Unicode U+201C e U+201D.
chat-manager-speech-double-quote-begin = “
chat-manager-speech-double-quote-end = ”

chat-manager-server-wrap-message = [bold]{$message}[/bold]
chat-manager-sender-announcement = Comando Central
chat-manager-sender-announcement-wrap-message = [font size=14][bold]Comunicado de {$sender}:[/font][font size=12]
                                                {$message}[/bold][/font]

chat-manager-entity-say-wrap-message = [BubbleHeader][bold][Name]{$entityName}[/Name][/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]{ chat-manager-speech-double-quote-begin }[BubbleContent][font="{$fontType}" size={$fontSize}][color={$color}]{$message}[/color][/font][/BubbleContent]{ chat-manager-speech-double-quote-end }[/font]
chat-manager-entity-say-bold-wrap-message = [BubbleHeader][bold][Name]{$entityName}[/Name][/bold][/BubbleHeader] {$verb}, [font={$fontType} size={$fontSize}]{ chat-manager-speech-double-quote-begin }[BubbleContent][font="{$fontType}" size={$fontSize}][bold][color={$color}]{$message}[/color][/bold][/font][/BubbleContent]{ chat-manager-speech-double-quote-end }[/font]
chat-manager-entity-whisper-wrap-message = [font size=11][italic][BubbleHeader][Name]{$entityName}[/Name][/BubbleHeader] sussurra, { chat-manager-speech-double-quote-begin }[BubbleContent][color={$color}][font="{$fontType}"]{$message}[/font][/color][/BubbleContent][font size=11]{ chat-manager-speech-double-quote-end }[/italic][/font]
chat-manager-entity-whisper-unknown-wrap-message = [font size=11][italic][BubbleHeader]Alguém[/BubbleHeader] sussurra, { chat-manager-speech-double-quote-begin }[BubbleContent][color={$color}][font="{$fontType}"]{$message}[/color][/font][/BubbleContent][font size=11]{ chat-manager-speech-double-quote-end }[/italic][/font]

chat-manager-entity-me-wrap-message = [italic]{ PROPER($entity) ->
    *[false] {$entityName} {$message}[/italic]
     [true] {CAPITALIZE($entityName)} {$message}[/italic]
    }

chat-manager-entity-looc-wrap-message = LOOC: [bold]{$entityName}:[/bold] {$message}
chat-manager-send-ooc-wrap-message = OOC: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-ooc-patron-wrap-message = OOC: [bold][color={$patronColor}]{$playerName}[/color]:[/bold] {$message}
chat-manager-send-dead-chat-wrap-message = {$deadChannelName}: [bold][BubbleHeader]{$playerName}[/BubbleHeader][/bold] {$verb}: "[BubbleContent]{$message}[/BubbleContent]"
chat-manager-send-admin-dead-chat-wrap-message = {$adminChannelName}: [bold]([BubbleHeader]{$userName}[/BubbleHeader])[/bold] {$verb}: "[BubbleContent]{$message}[/BubbleContent]"
chat-manager-send-admin-chat-wrap-message = {$adminChannelName}: [bold]{$playerName}:[/bold] {$message}
chat-manager-send-admin-announcement-wrap-message = [bold]{$adminChannelName}: {$message}[/bold]
chat-manager-send-hook-ooc-wrap-message = OOC: [bold](D){$senderName}:[/bold] {$message}
chat-manager-dead-channel-name = MORTOS
chat-manager-admin-channel-name = ADMIN
chat-manager-rate-limited = Você está enviando mensagens rápido demais!
chat-manager-rate-limit-admin-announcement = Aviso de limite de mensagens: { $player }
chat-manager-follow-button = (F)

## Verbos de fala do chat
chat-speech-verb-suffix-exclamation = !
chat-speech-verb-suffix-exclamation-strong = !!
chat-speech-verb-suffix-question = ?
chat-speech-verb-suffix-stutter = -
chat-speech-verb-suffix-mumble = ..
chat-speech-verb-name-none = Nenhum
chat-speech-verb-name-default = Padrão
chat-speech-verb-default = diz
chat-speech-verb-name-exclamation = Exclamação
chat-speech-verb-exclamation = exclama
chat-speech-verb-name-exclamation-strong = Grito
chat-speech-verb-exclamation-strong = grita
chat-speech-verb-name-question = Pergunta
chat-speech-verb-question = pergunta
chat-speech-verb-name-stutter = Gagueira
chat-speech-verb-stutter = gagueja
chat-speech-verb-name-mumble = Murmúrio
chat-speech-verb-mumble = murmura

chat-speech-verb-name-arachnid = Aracnídeo
chat-speech-verb-insect-1 = chia
chat-speech-verb-insect-2 = chilreia
chat-speech-verb-insect-3 = estala
chat-speech-verb-name-moth = Mariposa
chat-speech-verb-winged-1 = esvoaça
chat-speech-verb-winged-2 = bate as asas
chat-speech-verb-winged-3 = zumbe
chat-speech-verb-name-slime = Slime
chat-speech-verb-slime-1 = chacoalha
chat-speech-verb-slime-2 = borbulha
chat-speech-verb-slime-3 = escorre
chat-speech-verb-name-plant = Diona
chat-speech-verb-plant-1 = farfalha
chat-speech-verb-plant-2 = balança
chat-speech-verb-plant-3 = range
chat-speech-verb-name-robotic = Robótico
chat-speech-verb-robotic-1 = declara
chat-speech-verb-robotic-2 = bipa
chat-speech-verb-robotic-3 = faz bip-bop
chat-speech-verb-name-reptilian = Reptiliano
chat-speech-verb-reptilian-1 = sibila
chat-speech-verb-reptilian-2 = resfolega
chat-speech-verb-reptilian-3 = bufa
chat-speech-verb-name-skeleton = Esqueleto / Plasmaman
chat-speech-verb-skeleton-1 = chacoalha
chat-speech-verb-skeleton-2 = costela
chat-speech-verb-skeleton-3 = ossifica
chat-speech-verb-skeleton-4 = estala
chat-speech-verb-skeleton-5 = crepita
chat-speech-verb-name-vox = Vox
chat-speech-verb-vox-1 = guincha
chat-speech-verb-vox-2 = grita
chat-speech-verb-vox-3 = grasna
chat-speech-verb-name-canine = Canino
chat-speech-verb-canine-1 = late
chat-speech-verb-canine-2 = au-aua
chat-speech-verb-canine-3 = uiva
chat-speech-verb-name-goat = Cabra
chat-speech-verb-goat-1 = bale
chat-speech-verb-goat-2 = grunhe
chat-speech-verb-goat-3 = berra
chat-speech-verb-name-sheep = Ovelha
chat-speech-verb-sheep-1 = bale
chat-speech-verb-sheep-2 = faz béé
chat-speech-verb-name-small-mob = Rato
chat-speech-verb-small-mob-1 = guincha
chat-speech-verb-small-mob-2 = pia
chat-speech-verb-name-large-mob = Carpa
chat-speech-verb-large-mob-1 = ruge
chat-speech-verb-large-mob-2 = rosna
chat-speech-verb-name-monkey = Macaco
chat-speech-verb-monkey-1 = guincha
chat-speech-verb-monkey-2 = grita
chat-speech-verb-name-cluwne = Cluwne
chat-speech-verb-name-parrot = Papagaio
chat-speech-verb-parrot-1 = grasna
chat-speech-verb-parrot-2 = canta
chat-speech-verb-parrot-3 = chilreia
chat-speech-verb-cluwne-1 = ri baixinho
chat-speech-verb-cluwne-2 = gargalha
chat-speech-verb-cluwne-3 = ri
chat-speech-verb-name-ghost = Fantasma
chat-speech-verb-ghost-1 = reclama
chat-speech-verb-ghost-2 = suspira
chat-speech-verb-ghost-3 = cantarola
chat-speech-verb-ghost-4 = resmunga
chat-speech-verb-name-electricity = Eletricidade
chat-speech-verb-electricity-1 = crepita
chat-speech-verb-electricity-2 = zumbe
chat-speech-verb-electricity-3 = guincha
chat-speech-verb-vulpkanin-1 = ruge
chat-speech-verb-vulpkanin-2 = late
chat-speech-verb-vulpkanin-3 = rosna
chat-speech-verb-vulpkanin-4 = gania
chat-speech-verb-vulpkanin = Vulpkanin
chat-speech-verb-name-wawa = Wawa
chat-speech-verb-wawa-1 = entoa
chat-speech-verb-wawa-2 = declara
chat-speech-verb-wawa-3 = proclama
chat-speech-verb-wawa-4 = pondera
