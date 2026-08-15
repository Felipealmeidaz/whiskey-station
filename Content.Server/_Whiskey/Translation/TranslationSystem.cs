using System.Collections.Concurrent;
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
    /// Resultados esperando para serem entregues na thread do jogo.
    /// </summary>
    private readonly ConcurrentQueue<(Action<TranslationResult> entregar, TranslationResult resultado)> _prontos = new();

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
    /// Se a detecção tem como opinar sobre este idioma.
    /// </summary>
    /// <remarks>
    /// A detecção é por alfabeto, então ela só sabe dizer alguma coisa sobre
    /// idioma escrito em alfabeto diferente. Para russo ela acerta sempre; para
    /// inglês e português, que dividem o mesmo alfabeto, ela não tem base
    /// nenhuma e responder seria chute. Quem chama precisa saber a diferença,
    /// senão trata chute como certeza.
    /// </remarks>
    public bool DeteccaoConfiavel(string idioma)
    {
        return idioma == "ru";
    }

    /// <summary>
    /// Descobre em que idioma o texto foi escrito, até onde dá para descobrir
    /// barato.
    /// </summary>
    /// <remarks>
    /// O alfabeto resolve o russo com certeza, porque cirílico não aparece em
    /// português nem em inglês. Já português e inglês compartilham o alfabeto, e
    /// separar os dois exigiria detector de idioma de verdade, então quem cai
    /// nesse caso recebe o padrão configurado no servidor.
    /// </remarks>
    public string DetectarIdioma(string texto)
    {
        foreach (var c in texto)
        {
            if (c >= 'Ѐ' && c <= 'ӿ')
                return "ru";
        }

        return IdiomaDaEstacao;
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

        if (Interlocked.Increment(ref _pendentes) > MaxPendentes)
        {
            Interlocked.Decrement(ref _pendentes);
            _sawmill.Warning($"Tradução recusada: já há {MaxPendentes} em andamento.");
            aoTerminar(TranslationResult.Failed(text, "tradutor ocupado"));
            return;
        }

        _ = TraduzirAsync(text, from, to, aoTerminar);
    }

    private async Task TraduzirAsync(string text, string from, string to, Action<TranslationResult> aoTerminar)
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
        _prontos.Enqueue((aoTerminar, resultado));
    }

    public override void Update(float frameTime)
    {
        while (_prontos.TryDequeue(out var item))
        {
            try
            {
                item.entregar(item.resultado);
            }
            catch (Exception exc)
            {
                _sawmill.Error($"Falha ao entregar tradução: {exc}");
            }
        }
    }
}
