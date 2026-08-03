# SPDX-FileCopyrightText: 2026 punkzebub <punkzebub@gmail.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

### Interface

# Exibido na janela de preferências do personagem
humanoid-character-profile-summary =
    Este é {$name}. {$gender ->
        [male] Ele tem
        [female] Ela tem
        [epicene] Elu tem
       *[other] Isso tem
    } {$age} anos.
