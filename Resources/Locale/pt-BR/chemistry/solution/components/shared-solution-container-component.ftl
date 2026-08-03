# SPDX-FileCopyrightText: 2026 punkzebub <punkzebub@gmail.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

shared-solution-container-component-on-examine-main-text = O recipiente contém { $chemCount ->
    [1] uma substância [color={$color}]{$colorName}, de aspecto {$desc}[/color].
   *[other] uma mistura de substâncias [color={$color}]{$colorName}, de aspecto {$desc}[/color].
}

examinable-solution-has-recognizable-chemicals = Você reconhece {$recognizedString} na solução.
examinable-solution-recognized = [color={$color}]{$chemical}[/color]

examinable-solution-on-examine-volume = A solução contida está { $fillLevel ->
    [exact] com [color=white]{$current}/{$max}u[/color].
   *[other] [bold]{ -solution-vague-fill-level(fillLevel: $fillLevel) }[/bold].
}

examinable-solution-on-examine-volume-no-max = A solução contida está { $fillLevel ->
    [exact] com [color=white]{$current}u[/color].
   *[other] [bold]{ -solution-vague-fill-level(fillLevel: $fillLevel) }[/bold].
}

examinable-solution-on-examine-volume-puddle = A poça está { $fillLevel ->
    [exact] com [color=white]{$current}u[/color].
    [full] enorme e transbordando!
    [mostlyfull] enorme e transbordando!
    [halffull] funda e se espalhando.
    [halfempty] muito funda.
   *[mostlyempty] acumulada no chão.
    [empty] formando várias poças pequenas.
}

-solution-vague-fill-level =
    { $fillLevel ->
        [full] [color=white]cheia[/color]
        [mostlyfull] [color=#DFDFDF]quase cheia[/color]
        [halffull] [color=#C8C8C8]pela metade[/color]
        [halfempty] [color=#C8C8C8]meio vazia[/color]
        [mostlyempty] [color=#A4A4A4]quase vazia[/color]
       *[empty] [color=gray]vazia[/color]
    }
