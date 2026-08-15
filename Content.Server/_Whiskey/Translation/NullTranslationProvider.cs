using System.Threading;
using System.Threading.Tasks;

namespace Content.Server._Whiskey.Translation;

/// <summary>
/// Provedor que não traduz nada: devolve o texto original.
/// </summary>
/// <remarks>
/// É o padrão de propósito. Com ele o encanamento inteiro pode ser escrito,
/// revisado e testado antes de existir motor de tradução, chave de API ou
/// máquina rodando modelo. Também é para onde o servidor cai quando o motor
/// real está desligado ou fora do ar, em vez de derrubar o chat.
/// </remarks>
public sealed class NullTranslationProvider : ITranslationProvider
{
    public string Name => "nenhum";

    public bool CanTranslate => false;

    public Task<TranslationResult> TranslateAsync(
        string text,
        string from,
        string to,
        CancellationToken cancel = default)
    {
        return Task.FromResult(TranslationResult.Failed(text, "nenhum motor de tradução configurado"));
    }
}
