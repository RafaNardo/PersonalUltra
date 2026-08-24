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
