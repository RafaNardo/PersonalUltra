# Contratos de dados

Os exemplos são normativos quanto à intenção, não quanto à biblioteca de schema. A implementação deve materializá-los em JSON Schema/Zod ou equivalente e versioná-los.

## Entrada mínima CSV

```csv
external_key,name,primary_muscle_group,equipment,notes
bench-press-barbell,Supino reto com barra,Peito,Barra,
romanian-deadlift,Levantamento terra romeno,Pernas,Barra,
cable-row,Remada baixa,Costas,Cabo,"ênfase em postura estável"
```

Somente `name` é obrigatório na importação inicial. `external_key` é fortemente recomendado. Campos presentes podem ser marcados como `locked` num JSON complementar para impedir alteração pela IA.

## Entrada JSON rica

```json
{
  "schemaVersion": 1,
  "source": "catalogo-2026-08",
  "items": [
    {
      "externalKey": "bench-press-barbell",
      "name": "Supino reto com barra",
      "aliases": ["Supino com barra"],
      "primaryMuscleGroup": "Peito",
      "equipment": "Barra",
      "instructionsHint": "escápulas retraídas e pés apoiados",
      "visualHint": "atleta no banco horizontal, barra acima do peito",
      "lockedFields": ["name", "primaryMuscleGroup", "equipment"]
    }
  ]
}
```

## Taxonomia

Config separada e versionada:

```json
{
  "taxonomyVersion": "personal-ultra-v1",
  "primaryMuscleGroups": ["Peito", "Costas", "Ombros", "Braços", "Pernas", "Glúteos"],
  "equipment": ["Barra", "Halter", "Halteres", "Cabo", "Máquina", "Elástico", "Caneleira", "Peso corporal"],
  "unknownValuePolicy": "needs_review"
}
```

Para 200 exercícios, essa lista provavelmente precisará crescer. A factory deve emitir um relatório de valores fora da taxonomia; nunca deve forçar uma classificação incorreta apenas para passar no schema.

## Item canônico no manifesto

```json
{
  "externalKey": "bench-press-barbell",
  "source": {
    "file": "inputs/exercises.csv",
    "row": 2,
    "sourceHash": "sha256:..."
  },
  "identity": {
    "slug": "supino-reto-com-barra",
    "targetId": "10000000-0000-0000-0000-000000000001",
    "assetName": "supino-reto-com-barra"
  },
  "content": {
    "name": "Supino reto com barra",
    "aliases": ["Supino com barra"],
    "primaryMuscleGroup": "Peito",
    "equipment": "Barra",
    "instructions": "Mantenha as escápulas retraídas e os pés apoiados no chão.",
    "visualDescription": "Atleta executando supino reto com barra em banco horizontal."
  },
  "media": {
    "imageRef": "assets/training/supino-reto-com-barra.png",
    "masterPath": "masters/supino-reto-com-barra/v1.png",
    "exportPath": "assets/training/supino-reto-com-barra.png",
    "width": 1024,
    "height": 1536,
    "sha256": "...",
    "contentVersion": 1
  },
  "versions": {
    "metadataPromptVersion": "metadata-v1",
    "imagePromptVersion": "exercise-art-v1",
    "styleVersion": "personal-ultra-illustration-v1",
    "targetProfileVersion": "personal-ultra-v1"
  },
  "reviews": {
    "metadata": { "status": "approved", "reviewer": "...", "reviewedAt": "...", "contentHash": "..." },
    "visual": { "status": "approved", "reviewer": "...", "reviewedAt": "...", "contentHash": "..." },
    "biomechanics": { "status": "approved", "reviewer": "...", "reviewedAt": "...", "contentHash": "..." }
  },
  "state": "approved"
}
```

## Saída estruturada esperada da IA de texto

```json
{
  "canonicalName": "Supino reto com barra",
  "aliases": ["Supino com barra"],
  "primaryMuscleGroup": "Peito",
  "equipment": "Barra",
  "instructions": "Mantenha as escápulas retraídas e os pés apoiados no chão.",
  "visualDescription": "Pessoa em banco horizontal segurando barra alinhada ao peito.",
  "ambiguities": [],
  "confidence": {
    "primaryMuscleGroup": "high",
    "equipment": "high"
  }
}
```

Regras:

- `primaryMuscleGroup` e `equipment` devem pertencer à taxonomia ou retornar ambiguidade;
- instruções devem ser curtas, descritivas e sem prescrição de carga;
- não inventar contraindicação, diagnóstico ou promessa de segurança;
- confidence nunca aprova conteúdo automaticamente;
- schema inválido é retry técnico limitado, depois `failed_terminal`.

## Registro de chamada de provider

```json
{
  "stage": "image",
  "itemKey": "bench-press-barbell",
  "provider": "openai",
  "model": "configured-at-run-time",
  "idempotencyKey": "ecf-image-<hash-deterministico>",
  "requestId": "provider-response-id",
  "attempt": 1,
  "startedAt": "2026-08-14T12:00:00Z",
  "finishedAt": "2026-08-14T12:00:18Z",
  "inputHash": "sha256:...",
  "promptVersion": "exercise-art-v1",
  "status": "succeeded",
  "cost": { "currency": "USD", "estimated": null, "observed": null }
}
```

Não registrar token, Authorization header ou bytes/base64 no JSON de log.
Uma tentativa persistida como `started` sem artefato não pode ser redisparada
automaticamente. Se o operador confirmar uma nova tentativa, a anterior passa a
`failed_uncertain`, continua contabilizada e a nova recebe outro número,
idempotency key e reserva de custo.

## Decisão de revisão

```json
{
  "itemKey": "bench-press-barbell",
  "stage": "visual",
  "decision": "rejected",
  "reasonCode": "equipment",
  "notes": "A pegada e o suporte do banco não representam o exercício.",
  "reviewer": "reviewer-id",
  "reviewedAt": "2026-08-14T13:00:00Z",
  "artifactHash": "sha256:..."
}
```

## Pacote Personal Ultra

O exporter deve produzir ao menos:

```text
outputs/<runId>/personal-ultra/
  assets/training/*.png
  backend/ExerciseCatalogSeed.generated.cs
  mobile/exercise-media.generated.ts
  catalog-manifest.json
  integration-report.md
```

### Regras do seed gerado

- uma entrada por slug aprovado;
- GUID explícito e determinístico;
- `Name` até 200;
- `Slug` até 200;
- `PrimaryMuscleGroup` até 100;
- `Equipment` até 100;
- `ImageRef` até 2000;
- `Instructions` até 4000;
- `IsActive = true` para novos itens;
- ordem estável por slug ou regra registrada;
- escape correto de strings C#;
- nenhuma prescrição (`sets`, reps, descanso ou carga) no catálogo.

### Regras do media registry

Cada `ImageRef` precisa ter uma entrada literal:

```ts
'assets/training/supino-reto-com-barra.png':
  require('../../../assets/training/supino-reto-com-barra.png')
```

Gerar path relativo com base no target profile. Não montar o argumento de `require` dinamicamente.

## Relatório final

O relatório precisa listar:

- contagem por estado;
- itens novos, alterados, cache hits, rejeitados e pendentes;
- duplicidades/collisions;
- grupos/equipamentos fora da taxonomia atual;
- custo e duração quando conhecidos;
- arquivos a adicionar/alterar;
- comandos de validação do target;
- avisos que bloqueiam aplicação.
