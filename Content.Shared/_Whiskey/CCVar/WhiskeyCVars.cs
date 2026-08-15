using Robust.Shared.Configuration;

namespace Content.Shared._Whiskey.CCVar;

[CVarDefs]
public sealed partial class WhiskeyCVars
{
    /// <summary>
    ///     Liga a tradução de fala. Desligado, o servidor usa o provedor vazio e
    ///     nada é traduzido, que é o comportamento de sempre.
    /// </summary>
    public static readonly CVarDef<bool> TranslationEnabled =
        CVarDef.Create("whiskey.translation.enabled", false, CVar.SERVERONLY);

    /// <summary>
    ///     Endereço do serviço de tradução, sem barra no fim.
    /// </summary>
    /// <remarks>
    ///     O serviço ouve só em localhost do lado dele, então isto aponta para um
    ///     túnel ou para um IP liberado no firewall só para o servidor do jogo.
    ///     Endpoint de tradução aberto para a internet vira tradutor público de
    ///     terceiro na primeira varredura.
    /// </remarks>
    public static readonly CVarDef<string> TranslationUrl =
        CVarDef.Create("whiskey.translation.url", "http://127.0.0.1:8080", CVar.SERVERONLY);

    /// <summary>
    ///     Segredo compartilhado com o serviço, mandado no cabeçalho X-Segredo.
    ///     Vazio desliga a checagem dos dois lados.
    /// </summary>
    public static readonly CVarDef<string> TranslationSecret =
        CVarDef.Create("whiskey.translation.secret", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     O idioma que a estação fala.
    /// </summary>
    /// <remarks>
    ///     Serve para duas coisas que são a mesma: é o idioma assumido quando não
    ///     dá para descobrir pelo texto, e é o destino de quem usa tradutor. O
    ///     alfabeto separa russo de qualquer idioma latino com certeza, mas
    ///     português e inglês compartilham o alfabeto e distinguir os dois exigiria
    ///     detector de idioma de verdade, então um deles precisa ser o assumido.
    ///     Num servidor brasileiro esse é o português nos dois papéis.
    /// </remarks>
    public static readonly CVarDef<string> TranslationStationLanguage =
        CVarDef.Create("whiskey.translation.station_language", "pt", CVar.SERVERONLY);
}
