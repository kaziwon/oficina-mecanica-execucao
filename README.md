# Oficina Mecanica - Execucao - Fase 4

Microsservico responsavel pelo catalogo de servicos e pecas, estoque, fila de
trabalho e diagnostico. Usa MongoDB proprio e nao acessa o banco de Ordens.

## Escopo deste primeiro incremento

- Login administrativo com JWT.
- CRUD de servicos e pecas/insumos, com desativacao logica.
- Entrada e saida de estoque com atualizacao atomica e protecao de saldo.
- Registro de uma execucao vinculada ao ID de uma OS e consulta da fila.
- Inicio e conclusao do diagnostico, com historico e controle de versao.
- Copia dos nomes e precos dos itens indicados no diagnostico.
- Swagger, healthchecks e logs JSON com correlacao.
- Testes unitarios e de integracao com MongoDB real, CI e cobertura.

O fluxo disponivel nesta etapa e:

```text
AguardandoDiagnostico -> EmDiagnostico -> AguardandoLiberacao
```

Os proximos estados, incluindo reparo e finalizacao, serao conectados a liberacao
da Saga apos aprovacao e pagamento. O primeiro trecho de comunicacao com Ordens
ja registra a entrada automaticamente por mensagens. Nao ha integracao com
Cobranca nesta etapa. A criacao manual permanece disponivel para administradores.
Execucao nunca acessa o banco de Ordens; confirmacoes e recusas voltam por eventos.

## Primeiro contrato de integracao

O [contrato v1 de abertura de OS e entrada em Execucao](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/01-abertura-os.md)
fica documentado em Ordens, que coordenara a Saga. Ele define os comandos de
registro e compensacao e as respostas de Execucao, com repeticao segura e
tratamento de falhas. O consumidor e o publicador desse trecho estao implementados;
o [guia local em Ordens](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/02-executar-abertura.md)
explica como executar os dois servicos juntos, sem AWS.

O documento da execucao guarda tambem a Saga, as respostas e a identificacao dos
comandos processados. A entrada e sua resposta sao gravadas juntas, sem exigir
transacao entre colecoes ou replica set. Um indice unico impede reutilizar um
messageId em outra OS. Uma versao desatualizada nao pode sobrescrever a resposta
ou seu estado de publicacao.

Cancelar antes do registro cria uma marca `Cancelada` no mesmo documento, fora
da fila operacional. Um registro atrasado nao pode reativar essa entrada.
Depois do inicio do diagnostico, a compensacao automatica e recusada.

## Diagnostico integrado com a Saga

Para uma entrada recebida de Ordens, iniciar e concluir diagnostico grava tambem
eventos pendentes no mesmo documento MongoDB. O worker envia `DiagnosticoIniciado`
e `DiagnosticoConcluido` a Ordens. A conclusao inclui nomes, precos e quantidades
copiados do catalogo; alteracoes futuras no catalogo nao mudam esse evento.
Uma atualizacao concorrente ou repeticao HTTP nao grava uma segunda conclusao.
Entradas manuais sem Saga continuam independentes e nao inventam uma Saga.

Ordens recebe a conclusao e solicita o orcamento a Cobranca. Execucao nao acessa
o MySQL dos outros servicos nem chama Cobranca diretamente. As datas e os IDs
de correlacao acompanham as mensagens. A publicacao tambem incrementa a versao
do documento, impedindo que uma gravacao antiga desfaca a confirmacao de envio.

O [guia central de diagnostico e orcamento](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/03-diagnostico-orcamento.md)
explica o contrato, o Compose integrado e os scripts HTTP dos tres servicos.
Isso nao inicia pagamento nem libera o reparo ainda.

## Clean Architecture

```text
src/
  OficinaMecanica.Execucao.Domain/             Entidades e regras de transicao
  OficinaMecanica.Execucao.Application/        Use cases, inputs, outputs e contratos
  OficinaMecanica.Execucao.InterfaceAdapters/  Controllers Clean, gateways e presenters
  OficinaMecanica.Execucao.Infrastructure/     Driver MongoDB, serializacao e JWT
  OficinaMecanica.Execucao.Api/                HTTP, validacoes de entrada e composicao
tests/
  OficinaMecanica.Execucao.UnitTests/
  OficinaMecanica.Execucao.IntegrationTests/
```

Domain, Application e InterfaceAdapters nao possuem pacotes externos. O driver
MongoDB e a configuracao BSON ficam nas camadas externas. As entidades nao usam
atributos do MongoDB. Inputs, outputs, mappers e use cases ficam em pastas
separadas, com uma classe por arquivo.

```text
Controller HTTP -> Controller Clean -> Use case -> Interface do gateway
                                                        |
                              Gateway -> Interface da source -> Source Mongo -> MongoDB
```

Cada source trata sua colecao, sem repositorio generico. O `Program.cs` e
`DependencyInjection.cs` registram e conectam as implementacoes.

## MongoDB

O banco exclusivo de desenvolvimento se chama `execucao` e contem:

| Colecao | Conteudo |
|---|---|
| `servicos` | Catalogo de servicos e precos |
| `pecas_insumos` | Catalogo de pecas, precos e saldo |
| `execucoes` | Vinculo com a OS, diagnostico, itens e historico |

Os indices sao criados na inicializacao da API. Nome de servico/peca e unico,
sem diferenciar maiusculas e minusculas, incluindo cadastros desativados. O ID
da OS tambem e unico em `execucoes`. Os IDs sao GUIDs padrao e os valores
monetarios sao armazenados como Decimal128.

O historico e os itens ficam no mesmo documento da execucao. A gravacao exige
a versao que foi lida, impedindo sobrescrever uma alteracao concorrente. Uma
requisicao desatualizada recebe HTTP 409 e deve consultar o registro novamente.

Atualizar nome, descricao ou preco de uma peca nao altera o estoque. O saldo
muda somente pela API de movimentacoes. Concluir o diagnostico registra os
itens necessarios sem retirar pecas; reservas e compensacoes serao implementadas
na etapa da Saga.

## Executar localmente

Com Docker Desktop aberto, na raiz do repositorio:

```bash
docker compose --env-file .env.example up --build
```

Esse comando cria um MongoDB exclusivo com volume persistente, um usuario da
aplicacao com acesso apenas ao banco `execucao` e a API. As credenciais do
`.env.example` sao de desenvolvimento; valores reais podem vir de um `.env`
ignorado pelo Git ou das variaveis do ambiente de deploy.

- Swagger: <http://localhost:18082/swagger/index.html>
- Processo ativo: <http://localhost:18082/health>
- Conectividade MongoDB: <http://localhost:18082/health/ready>
- MongoDB no host: `127.0.0.1:27018`.

API e Mongo usam portas diferentes das de Ordens. Para encerrar os containers
mantendo os dados:

```bash
docker compose --env-file .env.example down
```

O usuario inicial e criado apenas em um volume novo. Alterar as credenciais
no arquivo de ambiente nao altera automaticamente um usuario ja persistido.
Senhas com caracteres reservados em URIs precisam ser codificadas na string
de conexao; o exemplo utiliza senhas alfanumericas para desenvolvimento.

## Primeiro fluxo no Postman

1. `POST /api/auth/login`, sem autenticacao:

```json
{
  "username": "admin",
  "password": "LocalAdminExecucao123"
}
```

Use o token retornado em `Authorization: Bearer TOKEN` nas proximas chamadas.

2. `POST /api/servicos`:

```json
{
  "nome": "Revisao de freios",
  "descricao": "Inspecao e ajuste",
  "preco": 100.15
}
```

3. `POST /api/pecas`:

```json
{
  "nome": "Pastilha de freio",
  "descricao": "Jogo dianteiro",
  "precoUnitario": 80.50,
  "quantidadeEstoque": 10
}
```

4. `POST /api/execucoes`, usando o ID retornado na criacao da OS em Ordens:

```json
{
  "ordemServicoId": "ID_DA_OS",
  "descricaoSolicitacao": "Revisar os freios"
}
```

5. `POST /api/execucoes/ID_DA_EXECUCAO/iniciar-diagnostico`, sem body.

6. `POST /api/execucoes/ID_DA_EXECUCAO/concluir-diagnostico`:

```json
{
  "diagnostico": "Pastilhas desgastadas; substituir o jogo dianteiro",
  "servicoIds": ["ID_DO_SERVICO"],
  "pecasInsumos": [
    { "pecaInsumoId": "ID_DA_PECA", "quantidade": 1 }
  ]
}
```

7. `GET /api/execucoes/ID_DA_EXECUCAO` mostra o historico, os precos copiados
e o status `AguardandoLiberacao`. Uma alteracao posterior no catalogo nao muda
os itens do diagnostico ja registrado.

## Endpoints

| Metodo | Rota | Uso |
|---|---|---|
| POST | `/api/auth/login` | Login administrativo |
| GET / POST | `/api/servicos` | Listar e cadastrar servicos |
| GET / PUT / DELETE | `/api/servicos/{id}` | Consultar, atualizar e desativar servico |
| GET / POST | `/api/pecas` | Listar e cadastrar pecas |
| GET / PUT / DELETE | `/api/pecas/{id}` | Consultar, atualizar e desativar peca |
| POST | `/api/pecas/{id}/movimentacoes` | Entrada ou saida de estoque |
| POST | `/api/execucoes` | Registrar entrada na fila |
| GET | `/api/execucoes?status=EmDiagnostico` | Consultar fila, mais antigas primeiro |
| GET | `/api/execucoes/{id}` | Consultar execucao e historico |
| POST | `/api/execucoes/{id}/iniciar-diagnostico` | Iniciar diagnostico |
| POST | `/api/execucoes/{id}/concluir-diagnostico` | Registrar diagnostico e itens |

Para movimentar estoque, envie `{ "quantidade": 3 }` para entrada ou
`{ "quantidade": -2 }` para saida. Saldo insuficiente retorna 409; quantidade
zero retorna 400. O PUT de peca recebe somente nome, descricao e precoUnitario.
Todas as rotas de negocio exigem JWT administrativo. Campos JSON desconhecidos
sao rejeitados, incluindo indicadores de pagamento enviados pelo chamador.

## Testes e CI

Testes unitarios nao precisam de MongoDB, Docker ou AWS:

```bash
dotnet test tests/OficinaMecanica.Execucao.UnitTests --configuration Release
```

Para executar tambem a integracao com MongoDB real:

```bash
docker compose --env-file .env.example up -d mongo
export MONGODB_TEST_CONNECTION_STRING='mongodb://execucao_root:LocalMongoRoot123@127.0.0.1:27018/?authSource=admin'
dotnet test OficinaMecanica.Execucao.slnx --configuration Release --collect:"XPlat Code Coverage" --results-directory TestResults
```

Os testes criam bancos isolados com prefixo `execucao_test_` e os removem ao
terminar. A conexao de testes precisa poder criar e remover esses bancos. Sem
a variavel, os testes Mongo sao explicitamente marcados como ignorados.

O GitHub Actions configura essa variavel e inicia um Mongo temporario no proprio
runner, executando a integracao real. A pipeline faz build, testes e publica a
cobertura como artefato. Nao publica imagem, nao faz deploy e nao usa a AWS.

O CI tambem sobe ElasticMQ e define `SQS_TEST_ENDPOINT`, verificando o SDK SQS
sem chamar a AWS. Para testar esse transporte localmente com o ambiente integrado:

```bash
export SQS_TEST_ENDPOINT=http://127.0.0.1:9324
dotnet test OficinaMecanica.Execucao.slnx --configuration Release
unset SQS_TEST_ENDPOINT
```

`Messaging:Enabled` e falso por padrao: iniciar apenas este servico nao publica
mensagens por acidente. O Compose integrado habilita os workers dos dois lados.
As credenciais `local/local` do simulador nao sao credenciais AWS.

O job de testes usa `ubuntu-22.04` para manter um kernel compativel com MongoDB 8.
Containers compartilham o kernel do runner; o bloqueio de inicializacao em
kernels recentes e acompanhado no [SERVER-121912](https://jira.mongodb.org/browse/SERVER-121912).
Essa escolha preserva os testes com MongoDB real, sem desativar verificacoes do banco.

## Recusa de orcamento

O cancelamento por recusa de orcamento ja esta integrado: Ordens envia
CancelarExecucaoPorRecusaOrcamento para a fila de comandos. Somente uma execucao
diagnosticada em AguardandoLiberacao pode ser cancelada. Estado e resposta sao
persistidos no mesmo documento; repeticoes nao duplicam efeitos. Se o reparo
ja foi liberado/iniciado, a recusa exige intervencao na Saga. Nao apaga
diagnostico, itens ou historico e nao devolve estoque que nao foi reservado.

O [guia central da resposta ao orcamento](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/04-resposta-orcamento.md)
inclui o contrato e o teste HTTP. Aprovacao ainda nao libera o reparo: falta
confirmar o pagamento e receber a autorizacao da Saga, conforme a etapa abaixo.

## Liberacao apos pagamento

Ordens envia LiberarExecucao somente apos receber PagamentoConfirmado. Execucao
valida o vinculo com a entrada original, os IDs e o valor pago contra a copia
dos itens do diagnostico, sem consultar o banco de Cobranca. O preco atual do
catalogo nao altera esse valor. O comando pertence a integracao interna; nenhum
body HTTP permite informar pagamento confirmado ou forcar a liberacao.

A autorizacao, o status AguardandoExecucao, o historico e a resposta
ExecucaoLiberada sao salvos juntos no documento Mongo, com controle de versao.
Reentregas nao duplicam a autorizacao. A Saga so muda a OS para
AguardandoExecucao depois de receber essa resposta. Se a regra de negocio
recusar, responde LiberacaoExecucaoRecusada e Ordens exige intervencao, sem
apagar o pagamento confirmado ou solicitar outra cobranca.

GET /api/execucoes/{id} agora mostra orcamentoId, pagamentoId e liberadaEm.
GET /api/execucoes?status=AguardandoExecucao consulta a fila autorizada.
Esta etapa nao reserva/consome estoque e nao inicia o reparo. Execucoes
historicas nao sao liberadas retroativamente.

O [guia central de liberacao](https://github.com/kaziwon/oficina-mecanica-ordens/blob/main/docs/integracao/06-liberacao-execucao.md)
inclui o contrato, os estados intermediarios e o teste HTTP automatizado.

## Proximos incrementos

- Reserva de pecas e liberacao da reserva com idempotencia.
- Eventos de reparo e coordenacao das proximas etapas por Ordens.
- Inicio e finalizacao dos reparos usando a autorizacao persistida da Saga.
- Qualidade com Sonar, verificacao de 80% e BDD distribuido.
- Kubernetes, Terraform, CD e observabilidade distribuida no New Relic.

Esta base e independente dos repositorios da Fase 3.
