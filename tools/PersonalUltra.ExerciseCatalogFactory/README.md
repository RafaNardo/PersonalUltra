# Personal Ultra Exercise Catalog Factory

Baseline local da ferramenta sob demanda. Nesta etapa ela não chama OpenAI,
Railway ou PostgreSQL e não modifica as APIs nem o app.

## Uso local

```powershell
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- init
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- import --file docs/projects/exercise-catalog-factory/exercise-inventory-v1.md
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- status
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- doctor
```

Para retomar um run após reiniciar o processo:

```powershell
dotnet run --project tools/PersonalUltra.ExerciseCatalogFactory -- import --resume <runId>
```

Os artefatos ficam em `tools/PersonalUltra.ExerciseCatalogFactory/workspace/`,
independentemente do diretório de onde a CLI é chamada, e são ignorados pelo Git.
A baseline sempre opera em dry-run, copia a fonte imutavelmente para o run e
confere SHA-256 antes de uma retomada.

`doctor` separa readiness local de integrações pendentes. Nesta milestone ele
não tenta acessar OpenAI, Railway ou o target profile.

## User Secrets pendentes

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
