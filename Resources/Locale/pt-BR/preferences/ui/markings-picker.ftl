# SPDX-FileCopyrightText: 2026 punkzebub <punkzebub@gmail.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

markings-search = Pesquisar
-markings-selection = { $selectable ->
    [0] Você não tem mais marcas disponíveis.
    [one] Você pode selecionar mais uma marca.
   *[other] Você pode selecionar mais {$selectable} marcas.
}
markings-limits = { $required ->
    [true] { $count ->
        [-1] Selecione pelo menos uma marca.
        [0] Você não pode selecionar nenhuma marca, mas de alguma forma precisa? Isto é um erro.
        [one] Selecione uma marca.
       *[other] Selecione pelo menos uma marca e até {$count} marcas. { -markings-selection(selectable: $selectable) }
    }
   *[false] { $count ->
        [-1] Selecione qualquer quantidade de marcas.
        [0] Você não pode selecionar nenhuma marca.
        [one] Selecione até uma marca.
       *[other] Selecione até {$count} marcas. { -markings-selection(selectable: $selectable) }
    }
}
markings-reorder = Reordenar marcas

humanoid-marking-modifier-respect-limits = Respeitar limites
humanoid-marking-modifier-respect-group-sex = Respeitar restrições de grupo e sexo
humanoid-marking-modifier-base-layers = Camadas de base
humanoid-marking-modifier-enable = Ativar
humanoid-marking-modifier-prototype-id = ID do protótipo:

# Categorias
markings-organ-Torso = Tronco
markings-organ-Head = Cabeça
markings-organ-ArmLeft = Braço esquerdo
markings-organ-ArmRight = Braço direito
markings-organ-HandRight = Mão direita
markings-organ-HandLeft = Mão esquerda
markings-organ-LegLeft = Perna esquerda
markings-organ-LegRight = Perna direita
markings-organ-FootLeft = Pé esquerdo
markings-organ-FootRight = Pé direito
markings-organ-Eyes = Olhos

markings-layer-Special = Especial
markings-layer-Tail = Cauda
markings-layer-Tail-Moth = Asas
markings-layer-Hair = Cabelo
markings-layer-FacialHair = Pelos faciais
markings-layer-UndergarmentTop = Roupa íntima superior
markings-layer-UndergarmentBottom = Roupa íntima inferior
markings-layer-Chest = Peito
markings-layer-Head = Cabeça
markings-layer-Snout = Focinho
markings-layer-SnoutCover = Focinho (cobertura)
markings-layer-HeadSide = Cabeça (lateral)
markings-layer-HeadTop = Cabeça (superior)
markings-layer-Eyes = Olhos
markings-layer-RArm = Braço direito
markings-layer-LArm = Braço esquerdo
markings-layer-RHand = Mão direita
markings-layer-LHand = Mão esquerda
markings-layer-RLeg = Perna direita
markings-layer-LLeg = Perna esquerda
markings-layer-RFoot = Pé direito
markings-layer-LFoot = Pé esquerdo
markings-layer-Overlay = Sobreposição
markings-layer-TailOverlay = Sobreposição
