# Personal Ultra Exercise Catalog Factory

Ferramenta sob demanda. O intake continua local e não chama OpenAI, PostgreSQL,
APIs ou app. O adapter S3 privado pode ser acionado apenas pelos comandos
explícitos descritos abaixo.

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
