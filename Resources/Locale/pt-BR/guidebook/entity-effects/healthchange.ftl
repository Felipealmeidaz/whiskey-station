# SPDX-FileCopyrightText: 2026 punkzebub <punkzebub@gmail.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

health-change-display =
    { $deltasign ->
        [-1] [color=green]{NATURALFIXED($amount, 2)}[/color] {$kind}
       *[1] [color=red]{NATURALFIXED($amount, 2)}[/color] {$kind}
    }
