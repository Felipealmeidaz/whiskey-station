using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server._Whiskey.Translation;

/// <summary>
/// Fala com o serviço de tradução que roda fora do jogo, por HTTP.
/// </summary>
/// <remarks>
/// <para>
/// O modelo de tradução é C++ e Python, e o jogo é C#. Rodar os dois no mesmo
/// processo significaria disputar núcleo com o servidor justo quando ele mais
/// precisa, com a estação cheia. Por isso a tradução vive em outra máquina e a
/// conversa é por HTTP.
/// </para>
/// <para>
/// A rede não é o gargalo aqui: são 2,76 ms medidos até a VPS, contra 160 a 360
/// ms do próprio modelo. Pares entre português e russo custam o dobro porque
/// não existe modelo direto e a frase passa pelo inglês no meio.
/// </para>
/// <para>
/// Nunca lança por falha de tradução, conforme o contrato de
/// <see cref="ITranslationProvider"/>. Serviço fora do ar devolve o texto
/// original, e quem chamou entrega a fala sem traduzir em vez de engolir a
/// mensagem.
/// </para>
/// </remarks>
public sealed class HttpTranslationProvider : ITranslationProvider
{
    private readonly HttpClient _http;
    private readonly string _url;
    private readonly string _secret;

    public HttpTranslationProvider(HttpClient http, string url, string secret)
    {
        _http = http;
        _url = url.TrimEnd('/');
        _secret = secret;
    }

    public string Name => "http";

    public bool CanTranslate => !string.IsNullOrWhiteSpace(_url);

    public async Task<TranslationResult> TranslateAsync(
        string text,
        string from,
        string to,
        CancellationToken cancel = default)
    {
        try
        {
            // Serializa antes e manda como StringContent de propósito. O
            // JsonContent não sabe o tamanho do corpo de antemão, então o
            // HttpClient manda em pedaços, com Transfer-Encoding: chunked, e
            // servidor HTTP simples não é obrigado a entender esse formato.
            // Com o corpo pronto na mão, o Content-Length vai junto e o pedido
            // é o mais simples possível de atender.
            var corpoEnviado = JsonSerializer.Serialize(new PedidoTraducao(text, from, to));

            using var pedido = new HttpRequestMessage(HttpMethod.Post, $"{_url}/traduzir")
            {
                Content = new StringContent(corpoEnviado, Encoding.UTF8, "application/json"),
            };

            if (!string.IsNullOrEmpty(_secret))
                pedido.Headers.Add("X-Segredo", _secret);

            using var resposta = await _http.SendAsync(pedido, cancel).ConfigureAwait(false);

            if (!resposta.IsSuccessStatusCode)
            {
                // O corpo traz o motivo em texto curto, e é ele que aparece no log
                // e no comando de admin. Sem isso, "falhou" não ajuda ninguém a
                // descobrir se foi par não suportado, frase grande demais ou
                // segredo errado.
                var motivo = await resposta.Content.ReadAsStringAsync(cancel).ConfigureAwait(false);
                return TranslationResult.Failed(text, $"serviço respondeu {(int) resposta.StatusCode}: {motivo}");
            }

            var corpo = await resposta.Content
                .ReadFromJsonAsync<RespostaTraducao>(cancel)
                .ConfigureAwait(false);

            if (corpo is null || string.IsNullOrWhiteSpace(corpo.Texto))
                return TranslationResult.Failed(text, "serviço devolveu resposta vazia");

            return TranslationResult.Ok(corpo.Texto);
        }
        catch (TaskCanceledException)
        {
            // Deixa subir como cancelamento para o TranslationSystem distinguir
            // tempo esgotado de erro do serviço.
            throw;
        }
        catch (HttpRequestException exc)
        {
            return TranslationResult.Failed(text, $"não consegui falar com o serviço: {exc.Message}");
        }
    }

    private sealed record PedidoTraducao(
        [property: JsonPropertyName("texto")] string Texto,
        [property: JsonPropertyName("de")] string De,
        [property: JsonPropertyName("para")] string Para);

    private sealed record RespostaTraducao(
        [property: JsonPropertyName("texto")] string Texto,
        [property: JsonPropertyName("ms")] int Ms);
}
