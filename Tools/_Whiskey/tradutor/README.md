# Serviço de tradução

Traduz fala entre português, inglês e russo. Roda **fora do servidor do jogo**,
porque o modelo é C++ e Python e o servidor precisa dos núcleos dele com a
estação cheia.

O jogo fala com ele por HTTP, pelo `HttpTranslationProvider`. Se o serviço
estiver fora do ar, o jogo entrega a fala sem traduzir, nunca engole a mensagem.

## Instalar

Precisa de Python 3.11 ou mais novo.

```bash
python3 -m venv /opt/trad
/opt/trad/bin/pip install ctranslate2 transformers torch sentencepiece
```

Converter os quatro modelos. Não existe modelo direto entre português e russo,
então esses pares passam pelo inglês no meio, e por isso são quatro e não dois:

```bash
for par in en-ru:m-en-ru ru-en:m-ru-en en-ROMANCE:m-en-pt ROMANCE-en:m-pt-en; do
  /opt/trad/bin/ct2-transformers-converter \
    --model "Helsinki-NLP/opus-mt-${par%%:*}" \
    --output_dir "/opt/trad/${par##*:}" \
    --quantization int8
done
```

Salvar o tokenizador dentro de cada pasta, para o serviço subir sem depender de
rede:

```python
from transformers import AutoTokenizer
pares = {
    "m-en-ru": "Helsinki-NLP/opus-mt-en-ru",
    "m-ru-en": "Helsinki-NLP/opus-mt-ru-en",
    "m-en-pt": "Helsinki-NLP/opus-mt-en-ROMANCE",
    "m-pt-en": "Helsinki-NLP/opus-mt-ROMANCE-en",
}
for pasta, hub in pares.items():
    AutoTokenizer.from_pretrained(hub).save_pretrained(f"/opt/trad/{pasta}")
```

Cada modelo ocupa 77 MB.

## Rodar

Como serviço, para subir sozinho depois de reiniciar:

```ini
[Unit]
Description=Serviço de tradução do Whiskey
After=network.target

[Service]
Type=simple
WorkingDirectory=/opt/trad
Environment=ENDERECO=127.0.0.1
Environment=PORTA=8080
Environment=NUCLEOS=1
Environment=SIMULTANEAS=12
ExecStart=/opt/trad/bin/python /opt/trad/servico.py
Restart=always
RestartSec=5
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/trad

[Install]
WantedBy=multi-user.target
```

## Configuração

| Variável | Padrão | Para que serve |
|---|---|---|
| `RAIZ` | `/opt/trad` | Pasta que contém os modelos |
| `ENDERECO` | `127.0.0.1` | Onde ouvir |
| `PORTA` | `8080` | Porta |
| `SEGREDO` | vazio | Exige o cabeçalho `X-Segredo` nos pedidos |
| `NUCLEOS` | `1` | Núcleos por tradução |
| `SIMULTANEAS` | `12` | Traduções ao mesmo tempo |

**Ouvir só em localhost é proposital.** Endpoint de tradução aberto para a
internet vira tradutor público de terceiro na primeira varredura. Para o
servidor do jogo alcançar, ou se libera a porta no firewall só para o IP dele,
ou se usa túnel por SSH:

```bash
ssh -N -L 18080:127.0.0.1:8080 usuario@maquina
```

**Não aumentar `NUCLEOS` achando que melhora.** Medido numa máquina de 12
núcleos, no caso caro que pivota pelo inglês:

| Config | Traduções por segundo | Mediana sob carga |
|---|---|---|
| 4 núcleos, 6 simultâneas | 17,4 | 649 ms |
| 2 núcleos, 12 simultâneas | 28,3 | 352 ms |
| 1 núcleo, 12 simultâneas | 34,5 | 293 ms |

O ganho vem de concorrência, não de núcleo por tradução, porque acima de um as
threads brigam entre si. Para uma tradução isolada a diferença é de 13 ms, que
não paga metade da vazão.

## Onde rodar

Duas opções, e o jogo não se importa com a escolha: o endereço é um CVar, então
dá para trocar depois sem mexer em código.

**Na própria máquina do jogo.** Menos peça para manter, sem rede no caminho e
sem depender de outra máquina existir. Não precisa de root: o `uv` instala um
Python próprio dentro da pasta do usuário, e o serviço roda com
`tradutor-usuario.service`, que está nesta pasta. É a opção usada hoje.

O medo natural é o tradutor roubar CPU do jogo, e ele é tratado por dois
ajustes que estão no arquivo do serviço. `SIMULTANEAS=2` limita o tradutor a
dois núcleos, aconteça o que acontecer. `Nice=15` diz ao sistema que o jogo tem
prioridade, então quando os dois quiserem CPU ao mesmo tempo o tradutor espera.
A tradução também é assíncrona do lado do jogo, então o servidor nunca fica
parado esperando resposta.

O custo que sobra é memória: cerca de 1,9 GB com os quatro modelos carregados.

**Em máquina separada.** Faz sentido quando a máquina do jogo está apertada de
memória, ou quando várias coisas vão usar o mesmo tradutor. Aí a configuração
sobe para `SIMULTANEAS=12`, que rende 34,5 traduções por segundo, e entra o
trabalho de rede da seção abaixo.

## Colocar em produção

O padrão ouve só em `127.0.0.1`, o que serve para testar mas **não funciona**
com o servidor do jogo em outra máquina. Para valer em produção, os quatro
passos, e os dois primeiros andam juntos:

1. Definir `SEGREDO` no serviço. Sem ele, abrir a porta deixa qualquer um usar o
   tradutor.
2. Trocar `ENDERECO` para o IP da rede interna.
3. Liberar a porta **só para o IP do servidor do jogo**:

   ```bash
   ufw allow from IP-DO-SERVIDOR to any port 8080 proto tcp
   ```

4. Ligar no config do servidor do jogo:

   ```
   whiskey.translation.enabled = true
   whiskey.translation.url = "http://IP-DO-SERVICO:8080"
   whiskey.translation.secret = "o mesmo segredo do passo 1"
   ```

Se o serviço cair ou ficar inalcançável, o jogo **não quebra**: o provedor
devolve o texto original e a fala sai sem traduzir. Nunca some mensagem.

### Alternativa: túnel, sem abrir porta nenhuma

Quando as duas máquinas não compartilham rede privada, ou quando não dá para
mexer no firewall, o túnel resolve sem expor nada. Ele sai de dentro para fora,
pela porta 22 que já está aberta, e mantém o `ENDERECO` em `127.0.0.1`.

Na máquina do jogo:

```bash
# 1. gerar chave, se ainda não existir
ssh-keygen -t ed25519 -N "" -f ~/.ssh/id_ed25519

# 2. mostrar a chave pública, que precisa ser autorizada no tradutor
cat ~/.ssh/id_ed25519.pub

# 3. conferir que a porta local está livre
ss -ltn | grep 18080
```

Na máquina do tradutor, autorizar a chave do passo 2 em
`~/.ssh/authorized_keys`.

De volta na máquina do jogo, instalar `tunel-tradutor.service` (está nesta
pasta) em `/etc/systemd/system/`, ajustar o `User` e o endereço, e ligar:

```bash
systemctl daemon-reload
systemctl enable --now tunel-tradutor.service
```

Aí o `url` do CVar aponta para `http://127.0.0.1:18080`, porque do ponto de
vista do servidor do jogo o tradutor virou localhost. Isso também dispensa o
segredo compartilhado, já que nada fica alcançável de fora.

## Rotas

`GET /saude` devolve os pares prontos.

`POST /traduzir` com `{"texto": "...", "de": "pt", "para": "ru"}` devolve
`{"texto": "...", "ms": 123}`.

Aceita `Content-Length` e `Transfer-Encoding: chunked`. O chunked não é luxo: é
como o `HttpClient` do .NET manda quando o corpo não tem tamanho calculado de
antemão.
