# SPDX-FileCopyrightText: 2026 punkzebub <punkzebub@gmail.com>
#
# SPDX-License-Identifier: AGPL-3.0-or-later

### Mensagens internas usadas pelo sistema de localização.

zzzz-fmt-pressure = { TOSTRING($divided, "F1") } { $places ->
    [0] kPa
    [1] MPa
    [2] GPa
    [3] TPa
    [4] PBa
   *[5] ???
}

zzzz-fmt-power-watts = { TOSTRING($divided, "F1") } { $places ->
    [0] W
    [1] kW
    [2] MW
    [3] GW
    [4] TW
   *[5] ???
}

zzzz-fmt-power-joules = { TOSTRING($divided, "F1") } { $places ->
    [0] J
    [1] kJ
    [2] MJ
    [3] GJ
    [4] TJ
   *[5] ???
}

zzzz-fmt-energy-watt-hours = { TOSTRING($divided, "F1") } { $places ->
    [0] Wh
    [1] kWh
    [2] MWh
    [3] GWh
    [4] TWh
   *[5] ???
}

zzzz-fmt-playtime = {$hours}h {$minutes}min
