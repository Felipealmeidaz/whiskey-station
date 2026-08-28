// SPDX-FileCopyrightText: 2026 HellFire <46168133+TheHellFireo@users.noreply.github.com>
// SPDX-FileCopyrightText: 2026 Whiskey Station Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Whiskey.Chat;

/// <summary>
/// Visual styles understood by the CMSS-style runechat renderer.
/// </summary>
public static class RunechatStyles
{
    public const string Pain = "runechatPain";
    public const string Scream = "runechatScream";

    public static bool IsInterrupting(string? style)
    {
        return style is Pain or Scream;
    }
}
