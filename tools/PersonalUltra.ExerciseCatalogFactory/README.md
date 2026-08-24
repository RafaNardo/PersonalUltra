# Personal Ultra Exercise Catalog Factory

Ferramenta sob demanda. Intake e dry-runs continuam locais e não chamam OpenAI,
PostgreSQL, APIs ou app. Os adapters OpenAI e S3 só podem ser acionados por
comandos explícitos protegidos pelos limites descritos abaixo.

## Uso local

```powershell
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- init
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- import --file tools/PersonalUltra.ExerciseCatalogFactory/Inputs/v1/exercise-inventory-v1.csv
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- status
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- doctor
```

Para retomar um run após reiniciar o processo:

```powershell
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- import --resume <runId>
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- intake --run <runId>
```

Os artefatos ficam em `tools/PersonalUltra.ExerciseCatalogFactory/workspace/`,
independentemente do diretório de onde a CLI é chamada, e são ignorados pelo Git.
A baseline sempre opera em dry-run, copia a fonte imutavelmente para o run e
confere SHA-256 antes de uma retomada.

O intake v1 materializa os 232 candidatos em
`normalization/catalog.normalized.v1.json` e gera
`normalization/intake-report.v1.md`. Reaberturas íntegras são cache hit. As 28
identidades legadas permanecem congeladas; duplicatas, colisões, aliases
prováveis e impactos de taxonomia são reportados para gate humano, sem IA,
rede, custo ou resolução silenciosa.

## Enriquecimento estruturado

Defina explicitamente `OpenAI:MetadataModel` em `appsettings.json` ou pela
variável `PERSONAL_ULTRA_FACTORY_OpenAI__MetadataModel`. Não existe model default:
`doctor` fica `BLOCKED` enquanto essa decisão não estiver configurada. Até o
dry-run exige o model para que o hash e a estimativa representem o run real.

O plano é local, não exige chave e não altera o manifesto:

```powershell
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- metadata enrich --run <runId> --max-items 10 --max-cost 1.00
```

Somente `--execute` permite chamadas à Responses API. O teto reserva o pior
caso de retries antes da primeira chamada; cada item recebe checkpoint próprio.
A tentativa `started` e sua idempotency key determinística são persistidas antes
da chamada. Em uma interrupção, a retomada recupera o artefato atômico já salvo.
Uma tentativa `started` sem artefato nunca é repetida
automaticamente: o comando bloqueia sem chamada. Somente a confirmação conjunta
`--execute --retry-uncertain` encerra essa tentativa como `failed_uncertain` e
autoriza uma tentativa nova, com nova idempotency key e nova reserva de custo;
isso pode representar uma segunda cobrança. O model ID, temperatura, versão do
prompt, estimativa por tentativa e máximo de tentativas ficam em
`appsettings.json`. Respostas passam pelo JSON Schema estrito e pela taxonomia
local. Campos `lockedFields` são reaplicados e verificados. Todas as propostas
terminam em `metadata_review`; confiança alta nunca significa aprovação.

```powershell
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- metadata enrich --run <runId> --max-items 10 --max-cost 1.00 --execute
```

Itens bloqueados pelo intake não são enviados à OpenAI. O comando não registra
chaves, headers, payload bruto nem mensagens potencialmente sensíveis do provider.

`doctor` separa readiness local de integrações pendentes e não acessa serviços
externos. O diagnóstico específico do bucket é somente leitura:

```powershell
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- bucket doctor
```

O smoke é dry-run por padrão e não faz chamadas externas:

```powershell
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- bucket smoke
```

Somente `--execute` permite criar o objeto exclusivo
`smoke/<runId>/<guid>.txt`. O fluxo faz PUT, HEAD, GET autenticado, GET por URL
assinada, DELETE exatamente dessa chave e confirma `NotFound` em `finally`.
Ele nunca lista objetos, remove prefixos, imprime secrets ou mostra a URL
assinada/query:

```powershell
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- bucket smoke --execute
```

Railway usa virtual-hosted style nos buckets atuais; somente buckets antigos
indicados dessa forma na aba Credentials exigem `ForcePathStyle: true`.

## Geração enxuta de imagens

Este fluxo lê diretamente o catálogo canônico versionado. Ele não cria painel,
CRUD de metadados, contact sheet, seed ou integração com API/mobile.

```powershell
# Piloto de dez imagens
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images plan --max-items 10 --max-cost 1.00
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images generate --max-items 10 --max-cost 1.00 --execute

# Catálogo completo normalizado (preserva o piloto e exclui needs_review)
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images plan --all --max-cost 5.00
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images generate --all --max-cost 5.00 --execute

# Após revisar os PNGs, informe um slug aprovado por linha
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images approve --file approved-images.txt

# Dry-run e confirmação do upload somente das aprovadas
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images upload
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images upload --execute

# Validar o lote publicado e gerar o seed C# (dry-run primeiro)
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images seed
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images seed --execute

# Substituir os 28 desenhos legados (lote isolado v3, estimativa USD 0,56)
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images plan --batch legacy-v3 --all --max-cost 1.00
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images generate --batch legacy-v3 --all --max-cost 1.00 --execute
# revise workspace/images/v3/files e informe um slug aprovado por linha
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images approve --batch legacy-v3 --file approved-images-v3.txt
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images upload --batch legacy-v3
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images upload --batch legacy-v3 --execute

# Criar WebP 640 px e publicar a entrega leve dos 231 exercícios
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images delivery
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- images delivery --execute
```

O style atual é `personal-ultra-exercise-image-v2`. Seu manifesto fica em
`workspace/images/v2/manifest.v1.json` e os arquivos em
`workspace/images/v2/files/<slug>.png`. Cada sucesso recebe checkpoint imediato.
Reexecutar preserva PNGs íntegros; arquivo ausente, alterado ou sem checkpoint
bloqueia em vez de disparar uma nova cobrança. A estimativa considera apenas
imagens pendentes. Respostas HTTP 429/5xx têm até três tentativas com pausa;
falhas de transporte incertas nunca são repetidas automaticamente. O upload v2
usa `exercise-catalog/v2/<slug>.png` e verifica tamanho e SHA-256 no bucket;
chaves já registradas como enviadas nunca são alteradas.
O exporter exige que todos os 220 itens normalizados estejam aprovados,
publicados, com object key v2 e arquivo local de hash idêntico. Ele gera
`ExerciseCatalogSeed.Generated.cs` de forma determinística, exclui qualquer
slug dos 28 itens legados e nunca escreve diretamente no banco. O seeder do
demo apenas inclui slugs ausentes e não sobrescreve registros existentes.

Os PNGs v2/v3 são masters versionados. O app consome somente os derivados
`media://exercise-catalog/delivery/v1/<slug>.webp`: 640×640, qualidade 78,
gerados localmente sem custo OpenAI e publicados com tamanho/hash verificados.
TrainerApi e StudentApi resolvem a referência para HTTPS assinado, e o Expo
mantém cache em disco usando a referência estável como chave.
Uma interrupção durante a chamada deixa `<slug>.png.pending`; confirme a
cobrança antes de remover conscientemente esse marcador e tentar novamente.

As dez imagens geradas anteriormente com o style v1 permanecem intactas em
`workspace/images/manifest.v1.json` e `workspace/images/files/` apenas como
arquivo/referência local. Os comandos v2 não leem, aprovam, publicam, movem ou
sobrescrevem esse material.

O input curado do lote legado fica em `Inputs/v3/legacy-exercise-images-v3.csv`.
Ele explicita as variações ambíguas (por exemplo afundo estacionário, remada
com triângulo e desenvolvimento sentado) para que o prompt não dependa do nome
curto herdado. Seu manifesto e PNGs ficam exclusivamente em `workspace/images/v3`.

Modelo, qualidade, tamanho, custo estimado por imagem e versão do prompt ficam
em `appsettings.json`. O custo é um teto operacional configurável, não uma
leitura da cobrança real. Referência oficial: [OpenAI Image generation](https://developers.openai.com/api/docs/guides/image-generation).

## User Secrets

```powershell
$factoryProject = 'tools/PersonalUltra.ExerciseCatalogFactory/PersonalUltra.ExerciseCatalogFactory.csproj'
dotnet user-secrets --project $factoryProject set 'ai-api-key' '<OPENAI_API_KEY>'
dotnet user-secrets --project $factoryProject set 'RailwayBucket:BucketName' '<BUCKET_NAME>'
dotnet user-secrets --project $factoryProject set 'RailwayBucket:AccessKeyId' '<ACCESS_KEY_ID>'
dotnet user-secrets --project $factoryProject set 'RailwayBucket:SecretAccessKey' '<SECRET_ACCESS_KEY>'
```

Não coloque valores reais em `appsettings.json`, arquivos `.env`, comandos
versionados ou documentação. O endpoint e demais opções não secretas ficam em
`appsettings.json`.

Para copiar a chave OpenAI já armazenada no RedAI sem exibi-la no terminal,
execute no notebook:

```powershell
./scripts/Copy-RedAiOpenAiSecret.ps1
```

Referências primárias: [Railway Storage Buckets](https://docs.railway.com/storage-buckets)
e [AWS SDK for .NET — presigned URLs](https://docs.aws.amazon.com/code-library/latest/ug/s3_example_s3_Scenario_PresignedUrl_section.html).
