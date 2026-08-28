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
