"""Serviço de tradução do Whiskey. Roda na vodka, o jogo consulta por HTTP.

Não existe modelo direto entre português e russo, então esses pares passam pelo
inglês no meio. Custa duas traduções em vez de uma, e medindo pelo HTTP, que é
como o jogo usa, dá 160 ms no caminho direto e de 200 a 360 ms no que pivota.
Continua irrelevante perto do tempo de uma conversa.

Ouve só em 127.0.0.1 de propósito. O firewall da VPS tem apenas a porta 22
aberta, e um endpoint de tradução sem autenticação exposto para a internet é
convite para virar tradutor público de terceiro. Para o servidor do jogo
alcançar, ou se abre a porta só para o IP dele no ufw, ou se usa túnel por SSH.
"""
import json
import logging
import os
import time
from concurrent.futures import ThreadPoolExecutor
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

import ctranslate2
import transformers

# Onde estão as pastas dos modelos. Configurável porque o serviço não roda
# sempre no mesmo lugar: numa máquina dedicada faz sentido /opt/trad, e na
# própria máquina do jogo ele vive dentro da pasta do usuário, sem root.
RAIZ = os.environ.get("RAIZ", "/opt/trad")

ENDERECO = os.environ.get("ENDERECO", "127.0.0.1")
PORTA = int(os.environ.get("PORTA", "8080"))

# Segredo compartilhado com o servidor do jogo. Vazio desliga a checagem, o que
# só é aceitável enquanto o serviço ouvir apenas em localhost.
SEGREDO = os.environ.get("SEGREDO", "")

# Núcleos por tradução. Um, e não mais, por mais estranho que pareça: acima de
# um as threads brigam entre si e o ganho some. Medido numa máquina de 12
# núcleos, no caso caro que pivota pelo inglês, 4 núcleos com 6 simultâneas
# entregam 17,4 traduções por segundo, e 1 núcleo com 12 simultâneas entregam
# 34,5, ainda por cima com metade da latência sob carga.
NUCLEOS = int(os.environ.get("NUCLEOS", "1"))

# Traduções simultâneas. É esta dimensão que escala, e não a de cima. Para uma
# tradução isolada a diferença entre as duas configurações é de 13 ms, que não
# paga metade da vazão.
SIMULTANEAS = int(os.environ.get("SIMULTANEAS", "12"))

# Teto de caracteres por pedido. Fala de jogo é curta, e texto gigante só serve
# para ocupar a fila.
MAX_CARACTERES = 400

MODELOS = {
    "m-en-ru": "Helsinki-NLP/opus-mt-en-ru",
    "m-ru-en": "Helsinki-NLP/opus-mt-ru-en",
    "m-en-pt": "Helsinki-NLP/opus-mt-en-ROMANCE",
    "m-pt-en": "Helsinki-NLP/opus-mt-ROMANCE-en",
}

PARES = {
    ("en", "ru"): "m-en-ru",
    ("ru", "en"): "m-ru-en",
    ("en", "pt"): "m-en-pt",
    ("pt", "en"): "m-pt-en",
}

# Pares sem modelo direto passam pelo inglês.
PIVOS = {
    ("pt", "ru"): ("pt", "en", "ru"),
    ("ru", "pt"): ("ru", "en", "pt"),
}

# O modelo en-ROMANCE atende vários idiomas latinos de uma vez, então precisa do
# alvo marcado no próprio texto, senão ele escolhe sozinho e sai em italiano.
#
# A marca tem que ser exatamente uma das que o tokenizador conhece, e a lista
# está em tok.supported_language_codes. Marca desconhecida não dá erro: vira
# <unk> em silêncio e o modelo escolhe o idioma que quiser, que foi como isto
# passou despercebido na primeira rodada. Entre as duas de português, pt_br é a
# brasileira, e a diferença aparece no vocabulário ("minha roupa dura" contra "o
# meu fato duro").
PREFIXO_ALVO = {"m-en-pt": ">>pt_br<<"}

log = logging.getLogger("tradutor")


class Motor:
    """Guarda os modelos carregados e sabe montar o caminho entre dois idiomas."""

    def __init__(self) -> None:
        self.tradutores: dict[str, ctranslate2.Translator] = {}
        self.tokenizadores: dict[str, object] = {}
        self.pool = ThreadPoolExecutor(max_workers=SIMULTANEAS)

    def carregar(self) -> None:
        for pasta in MODELOS:
            caminho = os.path.join(RAIZ, pasta)
            if not os.path.isdir(caminho):
                log.warning("modelo %s ausente, os pares que dependem dele vão falhar", pasta)
                continue

            self.tradutores[pasta] = ctranslate2.Translator(
                caminho, device="cpu", inter_threads=SIMULTANEAS, intra_threads=NUCLEOS
            )
            # Carrega o tokenizador da própria pasta do modelo, não do cache do
            # Hugging Face, para o serviço subir sem depender de rede.
            tok = transformers.AutoTokenizer.from_pretrained(caminho)
            self.tokenizadores[pasta] = tok

            # Marca de idioma desconhecida vira <unk> sem reclamar, e aí o modelo
            # escolhe o alvo sozinho. Melhor recusar a subir do que traduzir para
            # o idioma errado durante uma semana sem ninguém perceber.
            marca = PREFIXO_ALVO.get(pasta)
            if marca is not None and tok.convert_tokens_to_ids(marca) == tok.unk_token_id:
                conhecidas = getattr(tok, "supported_language_codes", [])
                raise RuntimeError(
                    f"o modelo {pasta} não reconhece a marca {marca}; "
                    f"as que ele aceita são {conhecidas}"
                )

            log.info("modelo %s carregado", pasta)

    def pares_disponiveis(self) -> list[str]:
        prontos = []
        for (de, para), pasta in PARES.items():
            if pasta in self.tradutores:
                prontos.append(f"{de}-{para}")
        for (de, para), (a, meio, b) in PIVOS.items():
            if PARES[(a, meio)] in self.tradutores and PARES[(meio, b)] in self.tradutores:
                prontos.append(f"{de}-{para}")
        return sorted(prontos)

    def _um_passo(self, texto: str, pasta: str) -> str:
        tradutor = self.tradutores[pasta]
        tok = self.tokenizadores[pasta]

        entrada = texto
        if pasta in PREFIXO_ALVO:
            entrada = f"{PREFIXO_ALVO[pasta]} {texto}"

        fonte = tok.convert_ids_to_tokens(tok.encode(entrada))
        saida = tradutor.translate_batch([fonte])
        return tok.decode(tok.convert_tokens_to_ids(saida[0].hypotheses[0]), skip_special_tokens=True)

    def traduzir(self, texto: str, de: str, para: str) -> str:
        if de == para:
            return texto

        if (de, para) in PARES:
            pasta = PARES[(de, para)]
            if pasta not in self.tradutores:
                raise ValueError(f"modelo de {de} para {para} não está carregado")
            return self._um_passo(texto, pasta)

        if (de, para) in PIVOS:
            a, meio, b = PIVOS[(de, para)]
            primeiro, segundo = PARES[(a, meio)], PARES[(meio, b)]
            if primeiro not in self.tradutores or segundo not in self.tradutores:
                raise ValueError(f"o caminho de {de} para {para} pelo inglês não está carregado")
            return self._um_passo(self._um_passo(texto, primeiro), segundo)

        raise ValueError(f"par não suportado: {de} para {para}")


motor = Motor()


class Handler(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def _responder(self, status: int, corpo: dict) -> None:
        dados = json.dumps(corpo, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(dados)))
        self.end_headers()
        self.wfile.write(dados)

    def _ler_corpo(self) -> bytes:
        """Lê o corpo do pedido, com ou sem Content-Length.

        O BaseHTTPRequestHandler não trata Transfer-Encoding: chunked sozinho, e
        o HttpClient do .NET manda exatamente assim quando o conteúdo não tem
        tamanho calculado de antemão. Sem isto, o corpo era lido como zero byte
        e o serviço respondia "texto vazio" para uma frase que existia, que é o
        pior tipo de erro: a mensagem aponta para o lugar errado.
        """
        if self.headers.get("Transfer-Encoding", "").lower() == "chunked":
            pedacos = []
            total = 0

            while True:
                linha = self.rfile.readline()
                if not linha:
                    break

                cabecalho = linha.strip()
                if not cabecalho:
                    continue

                # O tamanho vem em hexadecimal e pode trazer extensão depois de
                # ponto e vírgula, que é para ignorar.
                tamanho = int(cabecalho.split(b";")[0], 16)
                if tamanho == 0:
                    # Consome o rodapé até a linha em branco que fecha.
                    while self.rfile.readline().strip():
                        pass
                    break

                total += tamanho
                if total > MAX_CARACTERES * 4:
                    raise ValueError("corpo grande demais")

                pedacos.append(self.rfile.read(tamanho))
                self.rfile.read(2)  # o CRLF que fecha o pedaço

            return b"".join(pedacos)

        bruto = self.headers.get("Content-Length")
        if bruto is None:
            raise ValueError("pedido sem Content-Length e sem Transfer-Encoding")

        return self.rfile.read(int(bruto))

    def _autorizado(self) -> bool:
        if not SEGREDO:
            return True
        return self.headers.get("X-Segredo", "") == SEGREDO

    def do_GET(self) -> None:
        if self.path != "/saude":
            self._responder(404, {"erro": "rota desconhecida"})
            return
        self._responder(200, {"ok": True, "pares": motor.pares_disponiveis()})

    def do_POST(self) -> None:
        if self.path != "/traduzir":
            self._responder(404, {"erro": "rota desconhecida"})
            return

        if not self._autorizado():
            self._responder(403, {"erro": "segredo inválido"})
            return

        try:
            pedido = json.loads(self._ler_corpo() or b"{}")
            texto = (pedido.get("texto") or "").strip()
            de = (pedido.get("de") or "").lower()
            para = (pedido.get("para") or "").lower()
        except Exception as exc:
            self._responder(400, {"erro": f"pedido inválido: {exc}"})
            return

        if not texto:
            self._responder(400, {"erro": "texto vazio"})
            return

        if len(texto) > MAX_CARACTERES:
            self._responder(413, {"erro": f"texto acima de {MAX_CARACTERES} caracteres"})
            return

        inicio = time.perf_counter()
        try:
            saida = motor.pool.submit(motor.traduzir, texto, de, para).result(timeout=10)
        except ValueError as exc:
            self._responder(400, {"erro": str(exc)})
            return
        except Exception as exc:
            log.error("falha ao traduzir: %s", exc)
            # O jogo trata falha caindo para o texto original, então o pior que
            # acontece aqui é a frase chegar sem tradução, nunca sumir.
            self._responder(500, {"erro": "falha interna"})
            return

        ms = (time.perf_counter() - inicio) * 1000
        self._responder(200, {"texto": saida, "ms": round(ms)})

    def log_message(self, formato: str, *args) -> None:
        log.info("%s %s", self.address_string(), formato % args)


def main() -> None:
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(message)s")
    log.info("carregando modelos, %d núcleos por tradução, %d simultâneas", NUCLEOS, SIMULTANEAS)
    motor.carregar()
    log.info("pares prontos: %s", ", ".join(motor.pares_disponiveis()) or "nenhum")

    servidor = ThreadingHTTPServer((ENDERECO, PORTA), Handler)
    log.info("ouvindo em %s:%d, segredo %s", ENDERECO, PORTA, "ligado" if SEGREDO else "desligado")
    servidor.serve_forever()


if __name__ == "__main__":
    main()
