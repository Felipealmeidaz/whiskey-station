using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared._Whiskey.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server._Whiskey.Translation;

/// <summary>
/// Ponto único por onde o jogo pede tradução.
/// </summary>
/// <remarks>
/// <para>
/// Guarda o provedor ativo e resolve o problema central de encaixar algo
/// assíncrono num jogo que roda em tick: a tradução acontece fora da thread do
/// jogo, e o resultado só é entregue de volta dentro do <see cref="Update"/>.
/// Nada de mexer em entidade fora da thread do jogo, que é como se corrompe
/// estado de forma difícil de reproduzir.
/// </para>
/// <para>
/// Enquanto o provedor for <see cref="NullTranslationProvider"/>, tudo isso
/// roda e não traduz nada. É de propósito: o encanamento fica pronto, revisado
/// e testado antes de existir motor, e a escolha do motor pode ser feita depois
/// com o dado da medição na mão.
/// </para>
/// </remarks>
// Partial porque o analisador RA0049 exige isso de qualquer classe que use
// [Dependency], e a falha também só aparece no build em Release.
public sealed partial class TranslationSystem : EntitySystem
{
    /// <summary>
    /// Teto de traduções em voo ao mesmo tempo. Existe para que spam ou motor
    /// lento não acumulem fila infinita: passou do teto, o pedido é recusado na
    /// hora e o jogador recebe o texto original em vez de esperar para sempre.
    /// </summary>
    private const int MaxPendentes = 32;

    /// <summary>
    /// Tempo máximo esperando o motor. Melhor entregar sem traduzir do que
    /// deixar o jogador falando com o vazio.
    /// </summary>
    private static readonly TimeSpan Prazo = TimeSpan.FromSeconds(5);

    // Sem readonly de propósito: o analisador RA0051 do engine recusa campo de
    // [Dependency] marcado como readonly, e isso só aparece no build em Release.
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IHttpClientHolder _http = default!;

    private ITranslationProvider _provider = new NullTranslationProvider();

    /// <summary>
    /// Traduções em andamento, e quem está esperando cada uma.
    /// </summary>
    /// <remarks>
    /// Serve para não pedir duas vezes a mesma coisa. Uma fala de rádio é
    /// entregue ouvinte por ouvinte, então cinco russos no mesmo canal pediriam
    /// cinco traduções idênticas do mesmo texto. Com isto, o primeiro pedido
    /// vira trabalho e os outros só entram na fila do resultado.
    ///
    /// Só é tocado dentro de <see cref="Translate"/> e <see cref="Update"/>, os
    /// dois na thread do jogo, por isso não precisa de trava.
    /// </remarks>
    private readonly Dictionary<(string Texto, string De, string Para), List<Action<TranslationResult>>> _emVoo = new();

    /// <summary>
    /// Resultados esperando para serem entregues na thread do jogo.
    /// </summary>
    private readonly ConcurrentQueue<((string Texto, string De, string Para) Chave, TranslationResult Resultado)> _prontos = new();

    private int _pendentes;

    private ISawmill _sawmill = default!;

    public string ProviderName => _provider.Name;

    public bool CanTranslate => _provider.CanTranslate;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("whiskey.translation");

        // Reage a mudança em execução, então dá para ligar, apontar para outro
        // endereço ou desligar sem reiniciar a rodada.
        _cfg.OnValueChanged(WhiskeyCVars.TranslationEnabled, _ => MontarProvedor(), true);
        _cfg.OnValueChanged(WhiskeyCVars.TranslationUrl, _ => MontarProvedor());
        _cfg.OnValueChanged(WhiskeyCVars.TranslationSecret, _ => MontarProvedor());
    }

    /// <summary>
    /// Escolhe o provedor conforme a configuração atual.
    /// </summary>
    private void MontarProvedor()
    {
        if (!_cfg.GetCVar(WhiskeyCVars.TranslationEnabled))
        {
            SetProvider(new NullTranslationProvider());
            return;
        }

        var url = _cfg.GetCVar(WhiskeyCVars.TranslationUrl);

        if (string.IsNullOrWhiteSpace(url))
        {
            _sawmill.Warning("Tradução ligada mas sem endereço configurado, seguindo sem traduzir.");
            SetProvider(new NullTranslationProvider());
            return;
        }

        SetProvider(new HttpTranslationProvider(
            _http.Client,
            url,
            _cfg.GetCVar(WhiskeyCVars.TranslationSecret)));
    }

    /// <summary>
    /// O idioma que a estação fala, que é para onde o tradutor manda.
    /// </summary>
    public string IdiomaDaEstacao => _cfg.GetCVar(WhiskeyCVars.TranslationStationLanguage);

    /// <summary>
    /// Letras que existem em português e não em inglês. Achar qualquer uma
    /// delas já resolve, do mesmo jeito que o cirílico resolve o russo.
    /// </summary>
    private const string AcentosPortugueses = "ãõçâêôáíóúàü";

    /// <summary>
    /// Palavras curtas e comuns de cada idioma.
    /// </summary>
    /// <remarks>
    /// São de propósito as mais banais, e não vocabulário de estação: palavra
    /// comum aparece em quase toda frase, e é isso que faz a contagem
    /// funcionar em fala de jogo, que é curta. As versões sem acento estão na
    /// lista porque muita gente digita "nao" e "voce" no calor do momento.
    /// </remarks>
    private static readonly HashSet<string> PalavrasPortugues = new(StringComparer.OrdinalIgnoreCase)
    {
        "que", "nao", "não", "para", "com", "uma", "por", "mais", "voce", "você",
        "esta", "está", "isso", "aqui", "tem", "ele", "ela", "meu", "minha", "seu",
        "sua", "quem", "onde", "porque", "entao", "então", "tambem", "também",
        "muito", "pra", "vou", "vai", "foi", "eu", "nos", "nós", "sim", "obrigado",
        "preciso", "ajuda", "alguem", "alguém",
    };

    private static readonly HashSet<string> PalavrasIngles = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "you", "is", "are", "was", "this", "that", "have", "has",
        "with", "for", "not", "what", "where", "who", "your", "here", "there",
        "they", "them", "will", "can", "just", "get", "got", "need", "help",
        "please", "yes", "someone", "anyone", "going", "about",
    };

    /// <summary>
    /// Descobre em que idioma o texto foi escrito, e diz quando não sabe.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Três sinais, do mais forte para o mais fraco. Cirílico resolve o russo
    /// com certeza, porque não aparece nos outros dois. Acento de português
    /// resolve o português pelo mesmo motivo, já que inglês não usa nenhum
    /// deles. Sobrando os dois que dividem o alfabeto sem acento, vale a
    /// contagem de palavras comuns.
    /// </para>
    /// <para>
    /// Devolver falso quando não sabe é o ponto: frase de uma ou duas palavras
    /// não dá sinal nenhum, e tratar chute como certeza foi exatamente o que
    /// fazia o tradutor de inglês estragar frase que já estava certa.
    /// </para>
    /// </remarks>
    public bool TryDetectarIdioma(string texto, out string idioma)
    {
        idioma = string.Empty;

        foreach (var c in texto)
        {
            if (c >= 'Ѐ' && c <= 'ӿ')
            {
                idioma = "ru";
                return true;
            }
        }

        foreach (var c in texto)
        {
            if (AcentosPortugueses.IndexOf(char.ToLowerInvariant(c)) >= 0)
            {
                idioma = "pt";
                return true;
            }
        }

        var pontosPt = 0;
        var pontosEn = 0;

        foreach (var palavra in SepararPalavras(texto))
        {
            if (PalavrasPortugues.Contains(palavra))
                pontosPt++;
            else if (PalavrasIngles.Contains(palavra))
                pontosEn++;
        }

        if (pontosPt > pontosEn)
        {
            idioma = "pt";
            return true;
        }

        if (pontosEn > pontosPt)
        {
            idioma = "en";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Descobre o idioma, e chuta o da estação quando não dá para saber.
    /// </summary>
    public string DetectarIdioma(string texto)
    {
        return TryDetectarIdioma(texto, out var idioma) ? idioma : IdiomaDaEstacao;
    }

    private static IEnumerable<string> SepararPalavras(string texto)
    {
        var atual = new StringBuilder();

        foreach (var c in texto)
        {
            if (char.IsLetter(c))
            {
                atual.Append(char.ToLowerInvariant(c));
                continue;
            }

            if (atual.Length > 0)
            {
                yield return atual.ToString();
                atual.Clear();
            }
        }

        if (atual.Length > 0)
            yield return atual.ToString();
    }

    /// <summary>
    /// Troca o motor de tradução em execução. Quem chama é responsável por
    /// passar um provedor já configurado.
    /// </summary>
    public void SetProvider(ITranslationProvider provider)
    {
        _provider = provider;
        _sawmill.Info($"Provedor de tradução trocado para '{provider.Name}'.");
    }

    /// <summary>
    /// Pede uma tradução. O <paramref name="aoTerminar"/> é chamado na thread do
    /// jogo, sempre, inclusive quando falha. Nunca recebe nulo: em caso de erro
    /// vem o texto original.
    /// </summary>
    public void Translate(string text, string from, string to, Action<TranslationResult> aoTerminar)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            aoTerminar(TranslationResult.Failed(text, "texto vazio"));
            return;
        }

        if (!_provider.CanTranslate)
        {
            aoTerminar(TranslationResult.Failed(text, "nenhum motor de tradução configurado"));
            return;
        }

        // Já tem alguém pedindo exatamente isto: entra na fila do resultado em
        // vez de gerar trabalho novo.
        var chave = (text, from, to);

        if (_emVoo.TryGetValue(chave, out var esperando))
        {
            esperando.Add(aoTerminar);
            return;
        }

        if (Interlocked.Increment(ref _pendentes) > MaxPendentes)
        {
            Interlocked.Decrement(ref _pendentes);
            _sawmill.Warning($"Tradução recusada: já há {MaxPendentes} em andamento.");
            aoTerminar(TranslationResult.Failed(text, "tradutor ocupado"));
            return;
        }

        _emVoo[chave] = new List<Action<TranslationResult>> { aoTerminar };
        _ = TraduzirAsync(text, from, to, chave);
    }

    private async Task TraduzirAsync(string text, string from, string to, (string, string, string) chave)
    {
        TranslationResult resultado;

        try
        {
            using var cts = new CancellationTokenSource(Prazo);
            resultado = await _provider.TranslateAsync(text, from, to, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            resultado = TranslationResult.Failed(text, $"tempo esgotado após {Prazo.TotalSeconds:F0}s");
        }
        catch (Exception exc)
        {
            // Motor quebrado nunca pode derrubar o servidor nem sumir com a fala.
            _sawmill.Error($"Provedor '{_provider.Name}' lançou exceção: {exc.Message}");
            resultado = TranslationResult.Failed(text, "erro no motor de tradução");
        }
        finally
        {
            Interlocked.Decrement(ref _pendentes);
        }

        // Volta para a thread do jogo em vez de entregar aqui.
        _prontos.Enqueue((chave, resultado));
    }

    public override void Update(float frameTime)
    {
        while (_prontos.TryDequeue(out var item))
        {
            if (!_emVoo.Remove(item.Chave, out var esperando))
                continue;

            foreach (var entregar in esperando)
            {
                try
                {
                    entregar(item.Resultado);
                }
                catch (Exception exc)
                {
                    // Um ouvinte com problema não pode impedir a entrega para
                    // os outros que estavam esperando a mesma tradução.
                    _sawmill.Error($"Falha ao entregar tradução: {exc}");
                }
            }
        }
    }
}
