using System.Threading;
using System.Threading.Tasks;

namespace Content.Server._Whiskey.Translation;

/// <summary>
/// Contrato de quem sabe traduzir texto. Existe para separar o encanamento do
/// jogo do motor de tradução: o jogo fala com esta interface e nunca com uma
/// API específica.
/// </summary>
/// <remarks>
/// <para>
/// A tradução é assíncrona de propósito. Qualquer motor real, seja API na
/// internet ou modelo rodando em outra máquina, leva tempo, e amarrar isso ao
/// tick do servidor travaria o jogo inteiro enquanto espera resposta.
/// </para>
/// <para>
/// Trocar de motor deve ser trocar a implementação, sem tocar em nada do lado
/// do jogo. É isso que permite começar com <see cref="NullTranslationProvider"/>
/// e decidir depois, com dado na mão, se vale API paga, modelo local ou nada.
/// </para>
/// </remarks>
public interface ITranslationProvider
{
    /// <summary>
    /// Nome curto para aparecer em log e em comando de admin.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Se este provedor realmente traduz. Falso para o provedor vazio, que
    /// devolve o texto original. Serve para o jogo avisar o jogador em vez de
    /// fingir que traduziu.
    /// </summary>
    bool CanTranslate { get; }

    /// <summary>
    /// Traduz <paramref name="text"/> de <paramref name="from"/> para
    /// <paramref name="to"/>, usando códigos de idioma como "pt", "en", "ru".
    /// </summary>
    /// <remarks>
    /// Nunca lança por falha de tradução. Motor fora do ar, tempo esgotado ou
    /// idioma não suportado devolvem <see cref="TranslationResult.Failed"/> com
    /// o texto original intacto. Quem chama nunca fica sem resposta, e no pior
    /// caso o jogador recebe a frase sem traduzir, que é melhor que não receber.
    /// </remarks>
    Task<TranslationResult> TranslateAsync(
        string text,
        string from,
        string to,
        CancellationToken cancel = default);
}

/// <summary>
/// Resultado de uma tradução. Carrega sempre um texto utilizável, mesmo quando
/// falhou, para que nenhum caminho do jogo precise tratar nulo.
/// </summary>
public readonly record struct TranslationResult(string Text, bool Success, string? Error = null)
{
    /// <summary>
    /// Tradução concluída.
    /// </summary>
    public static TranslationResult Ok(string text) => new(text, true);

    /// <summary>
    /// Falhou. Devolve o texto original para que a mensagem chegue mesmo assim.
    /// </summary>
    public static TranslationResult Failed(string original, string error) =>
        new(original, false, error);
}
