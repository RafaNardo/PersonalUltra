# Milestone proposta — catálogo remoto e Exercise Catalog Factory

> Atualização de escopo em 2026-08-24: a primeira validação de imagens foi
> deliberadamente reduzida ao fluxo `images plan → generate → approve → upload`,
> limitado a dez PNGs 1024×1024. Processamento avançado, integração no app e
> publicação em lote abaixo continuam como possibilidades futuras, não como
> dependências do piloto. O README da Factory contém os comandos atuais.

Status: **plano aprovado; `PU-ECF-001` a `PU-ECF-004` e o adapter privado de
bucket implementados sem chamadas OpenAI pagas; o smoke real do bucket passou
em 2026-08-24**. Revisão humana, geração paga e alterações no produto continuam
pendentes.

Esta milestone substitui somente a decisão anterior de construir a factory em
outro repositório TypeScript. Os contratos de revisão, idempotência, retomada e
exportação descritos neste diretório continuam válidos.

## Resultado esperado

Ao final:

- uma CLI .NET 10 executável sob demanda vive dentro de `PersonalUltra.sln`;
- uma lista canônica revisada alimenta metadados e ilustrações por meio da API
  da OpenAI;
- toda geração é retomável, versionada e protegida por orçamento;
- somente imagens aprovadas são publicadas no bucket privado do Railway;
- o catálogo guarda uma referência imutável, nunca uma URL assinada expirada;
- TrainerApi e StudentApi resolvem referências remotas em URLs GET assinadas;
- o Expo baixa e mantém cache em disco, sem empacotar as 200–300 novas imagens;
- o seed é determinístico, idempotente e não altera snapshots históricos;
- nenhum conteúdo mockado entra no produto.

## Decisões propostas

### Aplicação da factory

Criar `tools/PersonalUltra.ExerciseCatalogFactory`, uma aplicação Console .NET
10 adicionada à solution. Ela não é uma API, não atende tráfego do produto e
não acessa PostgreSQL. Portanto, não cria uma terceira API surface nem um
microserviço.

Estrutura inicial:

```text
tools/PersonalUltra.ExerciseCatalogFactory/
  Cli/
  Configuration/
  Domain/
  Intake/
  Normalization/
  Providers/Text/
  Providers/Image/
  Media/
  Review/
  Publishing/S3/
  Exporters/PersonalUltra/
  Verification/
  prompts/
  schemas/
  inputs/
tests/PersonalUltra.ExerciseCatalogFactory.Tests/
```

`workspace/`, respostas brutas, imagens em processamento e `outputs/` não são
commitados. O input canônico aprovado, schemas, prompts e exemplos de config
são versionados.

### OpenAI

- usar Image API para cada arte independente;
- manter provider e model ID fora do domínio e registrados no manifesto;
- modelo inicial proposto: `gpt-image-2`;
- metadados usam Structured Outputs e ainda passam por validação local e humana;
- `doctor` verifica chave, acesso ao modelo e eventual verificação da organização
  antes da primeira chamada paga;
- a chave permanece como `ai-api-key` para ser copiada do RedAI sem exibição;
- `--dry-run` é o padrão; `--max-items`, `--max-cost` e confirmação explícita
  são obrigatórios para comandos pagos.

A documentação oficial recomenda a Image API para uma geração isolada por
prompt e informa que `gpt-image-2` suporta tamanho e qualidade configuráveis.

### Custo proposto

Para o lote final, usar `1024x1536`, qualidade média e saída WebP/PNG conforme
o teste visual. A estimativa oficial atual de `gpt-image-2` médio em retrato é
aproximadamente US$ 0,041 por imagem, sem contar o pequeno custo de texto/input.

```text
10 imagens piloto       ≈ US$ 0,41
200 imagens finais      ≈ US$ 8,20
232 imagens finais      ≈ US$ 9,51
```

As dez imagens piloto contam dentro das 232 quando aprovadas. US$ 11 podem
cobrir uma geração média de 232 imagens, mas deixam pouca margem
para rejeições, regenerações e enriquecimento. O lote completo só será liberado
depois do piloto. A execução terá teto inicial de US$ 10,00 e reserva mínima de
US$ 1,00. Rascunhos que precisarem apenas validar composição podem usar qualidade
baixa antes da versão média, sempre contabilizados no run.

### Direção visual Personal Ultra

Criar `styleVersion: personal-ultra-exercise-v1`, sem alterar silenciosamente o
guia anterior:

- fundo escuro neutro `#080808`, `#151515` e `#222220`;
- luz e detalhes em laranja `#FF6A13`;
- atleta inteiro e equipamento dentro da área segura do card/hero;
- roupa esportiva preta/grafite com painéis laranja;
- alternância determinística entre mulher e homem, com diversidade de biotipos;
- mulher com top e legging/short esportivo; homem com camiseta/regata e short;
- pequeno emblema `U`/`PU` aprovado em uma zona consistente da roupa;
- sem marcas de terceiros, watermark ou texto solto no fundo;
- pose representa uma fase inequívoca do exercício e não combina início/fim.

Modelos de imagem ainda podem deformar letras pequenas. O piloto deve comparar:

1. emblema geométrico simples `U`/`PU` fornecido como referência visual;
2. palavra `ULTRA` curta na roupa;
3. somente color blocking da marca, caso lettering não seja confiável.

Não prometer logo perfeito por prompt. O estilo só é congelado depois que as
dez artes forem vistas nos cards e heroes reais do app.

### Bucket Railway

Railway Buckets são privados e S3-compatible. Não persistir endpoint S3 como
URL pública e não persistir presigned URLs no banco.

Exatamente três valores ficam em .NET User Secrets:

```text
RailwayBucket:BucketName
RailwayBucket:AccessKeyId
RailwayBucket:SecretAccessKey
```

Configuração não secreta:

```json
{
  "RailwayBucket": {
    "EndpointUrl": "https://t3.storageapi.dev",
    "Region": "auto",
    "ForcePathStyle": false,
    "SignedUrlLifetimeMinutes": 360
  }
}
```

O smoke test real confirmará `ForcePathStyle` a partir do estilo informado pelo
bucket. Em Railway, os três segredos entram por Variable References equivalentes.

Chave imutável proposta:

```text
exercise-catalog/<styleVersion>/<slug>/v<contentVersion>/<sha256>.webp
```

O objeto recebe MIME correto e `Cache-Control: public, max-age=31536000,
immutable`. Um artefato aprovado nunca é sobrescrito; correção cria nova versão.

### Referência persistida e URL de transporte

O domínio e os snapshots persistem uma referência provider-neutral:

```text
media://exercise-catalog/<styleVersion>/<slug>/v<contentVersion>/<sha256>.webp
```

TrainerApi e StudentApi compartilham, via Infrastructure, um resolver que:

- mantém `assets/training/...` como fallback local para os 28 assets existentes;
- reconhece `media://...`;
- gera `imageUrl` GET assinada somente na resposta HTTP;
- nunca substitui `ImageRef` persistido por URL temporária.

As duas APIs continuam com endpoints e contratos próprios. Apenas a primitive de
storage/resolução é compartilhada. A configuração do bucket deve existir nos
dois hosts e na factory.

No mobile, uma primitive compartilhada de imagem recebe `imageRef` e `imageUrl`:

- asset local continua usando `require` literal;
- mídia remota usa `expo-image` com cache em disco e `cacheKey = imageRef`;
- placeholder amigável aparece se a imagem nunca foi baixada e o app está offline;
- nenhuma feature Trainer importa Student ou vice-versa.

Os 28 assets atuais permanecem no bundle para não quebrar prescrições e sessões
históricas. As novas centenas ficam somente remotas.

### Transferência segura do secret RedAI

Origem confirmada sem leitura do valor:

```text
C:\git\redai\apps\api\src\RedAI.Api\RedAI.Api.csproj
UserSecretsId: red-ai-api-local
Chave: ai-api-key
```

A factory terá um `UserSecretsId` próprio. Um script PowerShell copiará somente
`ai-api-key` entre os dois stores, em memória, com gravação atômica. Ele não
imprime valor, não o coloca na linha de comando e não copia `AI:Mode`.

O sandbox atual não permite escrever em `%APPDATA%\Microsoft\UserSecrets`; o
script deverá ser executado pelo usuário fora do sandbox. O comando deve imprimir
apenas sucesso/falha e nomes de chave.

## Execução faseada

Cada item segue o loop já adotado: agente implementa, agente/revisor distinto
audita quando o risco justificar, agente principal valida, faz commit intencional
e push, então avança. Itens pagos e gates humanos nunca avançam automaticamente.

### `PU-ECF-000` — Aprovação do plano e catálogo

- revisar este documento e [exercise-inventory-v1.md](exercise-inventory-v1.md);
- resolver duplicidades/nomes ambíguos dos 28 itens atuais;
- aprovar taxonomia, piloto e direção visual;
- rotacionar as credenciais publicadas na conversa;
- fornecer credenciais temporárias novas somente via User Secrets.

Gate: nenhuma implementação nem chamada externa antes da aprovação.

### `PU-ECF-001` — Baseline .NET 10

- Console app e testes na solution;
- comandos `init`, `import`, `status`, `doctor`;
- config validada, UserSecretsId próprio e logs redigidos;
- run ID, retomada e dry-run padrão;
- script seguro de cópia da `ai-api-key`.

Situação: baseline local implementada. `init`, `import`, `import --resume`,
`status` e `doctor` funcionam sem integrações externas. O import copia a fonte
imutavelmente para o run e a retomada confere SHA-256 antes de atualizar o
checkpoint. O diagnóstico separa readiness local de integrações pendentes e os
logs JSONL são redigidos. Ports e providers de metadata, imagem e object storage
entram apenas nas milestones próprias, sem placeholders prematuros.

### `PU-ECF-002` — Contratos e manifesto

- schemas versionados;
- persistência atômica;
- hashes por estágio;
- estados, tentativas, custos e respostas redigidas;
- testes de interrupção/reabertura.

Situação: contratos JSON v1 de input, manifesto, revisão e pacote disponíveis.
O manifesto rejeita schema/estado/propriedade desconhecidos, referências com
aparência de credencial e chaves externas duplicadas. Checkpoints são validados
antes da troca atômica; uma interrupção mantém o último manifesto íntegro. A
reconciliação determinística de hashes invalida apenas artefatos e reviews
downstream. Intake/canonicalização completos permanecem em `PU-ECF-003`.

### `PU-ECF-003` — Intake, identidade e deduplicação

- importar os 232 candidatos;
- preservar os 28 slugs/GUIDs atuais;
- UUID v5 para novos itens;
- relatório de duplicatas e impacto de taxonomia;
- nenhuma IA e nenhum custo.

Gate: usuário aprova o relatório canônico.

Situação: intake local implementado sobre o input canônico v1 com 232
candidatos. A normalização é determinística, itens novos recebem UUID v5 e o
profile congelado preserva exatamente os 28 slugs/GUIDs atuais. Duplicatas,
colisões e ambiguidades documentadas permanecem em `needs_review`; catálogo e
relatório retomáveis registram o impacto da taxonomia sem IA ou custo. O gate
humano continua aberto.

### `PU-ECF-004` — Enriquecimento estruturado

- adapter OpenAI de texto;
- JSON Schema estrito;
- aliases, grupo, equipamento, instrução e descrição visual;
- lotes retomáveis e orçamento;
- propostas permanecem pendentes.

Situação: adapter real da OpenAI Responses API implementado atrás de
`IMetadataProvider`, usando Structured Outputs com JSON Schema estrito v1,
taxonomia allowlist e prompt/model/temperatura versionados. `metadata enrich` é
dry-run por padrão e exige `--max-items`, `--max-cost` e `--execute` para chamar
o provider. O orçamento reserva o pior caso de retries por estágio/item/input
hash e falhas não descartam sucessos anteriores. Cada tentativa recebe
checkpoint `started` antes da chamada e uma idempotency key determinística
persistida; retomadas recuperam primeiro um artefato órfão válido. Uma tentativa
incerta sem artefato nunca é redisparada automaticamente: bloqueia por padrão e só pode ser encerrada e substituída
por nova tentativa/custo com `--execute --retry-uncertain`. Campos travados são
reaplicados e validados; ambiguidades já bloqueadas no intake não são enviadas e
toda proposta termina em `metadata_review`, nunca aprovada automaticamente.
Nenhuma chamada paga foi executada nesta implementação.

### `PU-ECF-005` — Revisão de metadados

- listar, editar, aprovar, rejeitar e mesclar;
- trilha de auditoria;
- aprovação biomecânica separada;
- edição invalida somente derivados necessários.

Gate: os dez itens piloto precisam estar aprovados.

### `PU-ECF-006` — Integração S3 privada

- `AWSSDK.S3` atrás de `IObjectStore`;
- options validation e nunca logar credenciais;
- PUT/HEAD/GET/presign/delete com chaves estritamente delimitadas;
- smoke real cria `smoke/<runId>/<guid>`, confere bytes/hash/MIME, assina GET,
  baixa e remove exatamente esse objeto;
- teste confirma virtual-host/path-style.

Pode ser desenvolvido em paralelo com `PU-ECF-002/003`, mas o smoke real ocorre
somente após rotação das credenciais.

Situação: adapter `AWSSDK.S3` implementado atrás de uma porta interna, com
credenciais opacas, validação estrita de endpoint/região/addressing e object key
exclusiva para smoke. `bucket doctor` é somente leitura; `bucket smoke` é
dry-run sem `--execute` e o modo real valida PUT/HEAD/GET, bytes, SHA-256, MIME,
GET assinado e cleanup exato em `finally`, sem listar/remover prefixos nem
exibir secrets ou URLs assinadas. Em 2026-08-24, após atualização das
credenciais/configuração, o smoke real passou em virtual-hosted style: PUT,
HEAD, GET autenticado, GET assinado, bytes, SHA-256, MIME, DELETE da chave exata
e `NotFound` final foram confirmados. O cleanup terminou sem objeto residual.

### `PU-ECF-007` — Prompt visual e piloto pago

- Image API com prompt versionado;
- gerar os dez itens em dois lotes de cinco;
- primeiro lote precisa ser revisado antes do segundo;
- originais, derivados, usage, custo e SHA-256 registrados;
- sem upload de imagem não aprovada.

Gate: usuário revisa as dez imagens dentro do app e congela `styleVersion`.

### `PU-ECF-008` — Processamento e review visual

- WebP/PNG final, dimensões, compressão e safe crop;
- contact sheet/galeria local;
- aprovação visual e biomecânica independentes;
- regeneração somente do item rejeitado;
- versões anteriores preservadas.

### `PU-ECF-009` — Lote retomável e publicação

- concorrência conservadora e circuit breaker;
- teto de custo e relatório observado;
- upload somente após todas as aprovações do item;
- re-download e SHA-256 após upload;
- manifesto guarda object key, nunca segredo ou URL assinada.

Gate: confirmar orçamento restante antes de gerar além do piloto.

### `PU-ECF-010` — Exporter Personal Ultra

- seed C# gerado, ordenado e determinístico;
- wrapper manual combina legado e gerado;
- IDs/slugs existentes preservados;
- `ImageRef = media://...` para itens novos;
- exporter gera pacote, não escreve no banco.

Situação: concluído para o lote v2. `images seed` valida em dry-run os 220
itens normalizados aprovados/publicados e seus hashes locais; `--execute` gera
203 entradas novas ordenadas em `ExerciseCatalogSeed.Generated.cs`. O wrapper
preserva os 28 IDs/slugs/assets locais e o seeder inclui somente slugs ausentes,
sem sobrescrever linhas ou snapshots. As referências `media://` ainda não são
resolvidas pelas APIs/mobile; isso permanece exclusivamente em `PU-ECF-011`.

### `PU-ECF-011` — Consumo remoto nas APIs e Expo

- resolver compartilhado em Infrastructure;
- `imageUrl` nos contratos específicos de Trainer/Student;
- URLs assinadas renováveis;
- primitive compartilhada com cache em disco;
- legado local preservado;
- URLs expiradas renovam por refetch sem alterar snapshots.

### `PU-ECF-012` — Apply e validação ponta a ponta

- verificar e aplicar pacote em branch limpa;
- seed duas vezes sem duplicar;
- Trainer pesquisa/filtra e prescreve item novo;
- Student visualiza, executa e retoma;
- imagem aparece em preview, sessão e resumo;
- cenário offline continua funcional com placeholder/cache;
- run incremental valida cache hit, item novo, rename, duplicata e regeneração;
- runbook final documenta uma próxima carga.

## Validação mínima

Por milestone:

- testes unitários/golden files da factory;
- teste de idempotência, retomada, orçamento e redaction;
- `git diff --check`.

No fechamento:

```powershell
dotnet build PersonalUltra.sln --no-restore
dotnet test tests/PersonalUltra.Api.IntegrationTests/PersonalUltra.Api.IntegrationTests.csproj --no-build
dotnet test tests/PersonalUltra.ExerciseCatalogFactory.Tests/PersonalUltra.ExerciseCatalogFactory.Tests.csproj
npm run mobile:typecheck
npx expo export --platform ios --output-dir .expo-export-catalog-check
```

Também executar o smoke real do bucket, verificar URLs assinadas nas duas APIs e
validar o piloto em card pequeno e hero sem usar dados mockados.

## Bloqueios antes da execução

1. As credenciais coladas na conversa estão comprometidas e devem ser rotacionadas.
2. Confirmar que `t3.storageapi.dev` usa virtual-hosted style ou path-style.
3. Aprovar a nova taxonomia de 12 grupos e o impacto nos filtros Trainer.
4. Resolver os itens ambíguos descritos no inventário.
5. Definir quem dará aprovação biomecânica das instruções e imagens.
6. Aprovar `U/PU`, `ULTRA` ou apenas color blocking após o primeiro lote de cinco.
7. Confirmar que a conta OpenAI tem acesso a `gpt-image-2` e organização verificada.

## Referências oficiais consultadas

- [OpenAI Image generation](https://developers.openai.com/api/docs/guides/image-generation)
- [OpenAI API pricing](https://developers.openai.com/api/docs/pricing)
- [Railway Storage Buckets](https://docs.railway.com/storage-buckets)
- [Railway uploads, exports and assets](https://docs.railway.com/guides/storage-buckets-guide)
