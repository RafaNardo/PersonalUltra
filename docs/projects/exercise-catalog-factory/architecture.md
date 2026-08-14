# Arquitetura — Exercise Catalog Factory

## Forma do produto

Ferramenta CLI standalone, pensada para execução manual esporádica. Uma API/web UI não é necessária para V1. TypeScript/Node é a recomendação inicial pela integração simples com schemas, SDKs de IA, CSV e geração de arquivos TypeScript; a escolha pode mudar sem alterar os contratos deste diretório.

```text
Input adapter (CSV/JSON)
        ↓
Canonical catalog + job manifest
        ↓
Normalization → Text enrichment → Metadata review
        ↓
Image prompt → Image provider → Image processing → Visual review
        ↓
Target exporter → Package verifier → Personal Ultra PR
```

## Módulos

### `domain`

Tipos canônicos, schemas, regras de estado, taxonomias e cálculo de hashes. Não conhece OpenAI, filesystem do Personal Ultra, C# ou Expo.

### `intake`

Importa CSV/JSON, preserva a origem e produz itens canônicos. Deve aceitar uma lista mínima apenas com nomes, mas aproveitar hints fornecidos para reduzir custo e ambiguidade.

### `normalization`

Gera slug e asset name, normaliza espaços/acentos, detecta colisões e cria candidatos de duplicidade. Não usa IA para decisões determinísticas.

### `providers/text`

Porta para enriquecimento estruturado. O adapter OpenAI recebe um item e a taxonomia permitida, devolvendo apenas JSON validável. Provider/model IDs são configuração do run, não parte do domínio.

### `providers/image`

Porta para geração de uma arte. Recebe prompt final, formato desejado e contexto aprovado. Retorna bytes/URL temporária e metadados de rastreio. URLs temporárias devem ser baixadas imediatamente; o manifesto guarda hashes e paths locais, não depende da URL.

### `media`

Valida, converte e otimiza imagens. Produz master, arquivo de integração e thumbnail/contact sheet. Não altera semanticamente uma imagem sem criar uma nova versão derivada.

### `review`

Aplica decisões humanas e mantém trilha de auditoria. V1 pode operar via CLI + JSON/Markdown/HTML estático. A camada não deve exigir um serviço online.

### `exporters`

Adapters específicos por target. O primeiro é `personal-ultra`, responsável por C#, PNGs e registry Expo. Exporter não chama provider de IA.

### `verification`

Valida o workspace da factory e, em modo somente leitura, o repositório alvo. Após um `apply` explicitamente autorizado, executa ou instrui os comandos de validação do target.

## Estrutura sugerida do novo repositório

```text
src/
  cli/
  domain/
  intake/
  normalization/
  providers/
    text/
    image/
  media/
  review/
  exporters/
    personal-ultra/
  verification/
schemas/
prompts/
  metadata/
  image/
fixtures/
tests/
docs/
factory.config.example.json
```

Dados de execução não devem ser commitados por padrão:

```text
workspace/<runId>/
  source/
  manifest.json
  provider-responses/
  masters/
  derived/
  reviews/
  logs/
outputs/<runId>/
```

## Máquina de estados por item

```text
imported
  → normalized
  → metadata_pending
  → metadata_generated
  → metadata_review
  → metadata_approved
  → image_pending
  → image_generated
  → image_review
  → approved
  → exported
```

Estados laterais:

- `needs_review`: colisão/ambiguidade que impede avanço;
- `rejected`: requer edição ou regeneração explícita;
- `failed_retryable`: falha transitória de provider/rede;
- `failed_terminal`: schema, política ou limite inválido;
- `deprecated`: item existente preservado, mas excluído de novos exports conforme política explícita.

Transições precisam ser persistidas atomicamente. O processo deve calcular o próximo estágio pelo manifesto, nunca pela simples existência de um arquivo.

## Identidade e idempotência

### Chave externa

`externalKey` é a identidade do item na factory. O operador deve fornecê-la quando possível. Se ausente, a primeira importação deriva uma chave do nome; essa chave é congelada no manifesto e não muda quando o display name for corrigido.

### Slug

Slug é a identidade estável no Personal Ultra. Uma mudança de slug é uma operação de migração explícita, não consequência automática de renomear.

### GUID

O target profile deve:

1. importar e preservar GUIDs de slugs já existentes;
2. gerar GUID determinístico para itens novos, preferencialmente UUID v5 com namespace fixo do projeto e slug;
3. rejeitar qualquer colisão slug/GUID.

Não continuar o padrão numérico manual atual para centenas de itens sem um allocator determinístico e testado.

### Hash de estágio

Cada resultado derivado guarda um hash das entradas relevantes:

```text
metadataInputHash = hash(source fields + taxonomy + metadataPromptVersion)
imageInputHash    = hash(approved metadata + imagePromptVersion + styleVersion + image config)
exportInputHash   = hash(approved item + targetProfileVersion)
```

Se o hash não mudou e o artefato passou integridade, o estágio é cache hit.

## Versões

O manifesto e o relatório devem registrar:

- `schemaVersion` — formato do manifesto;
- `pipelineVersion` — comportamento da ferramenta;
- `metadataPromptVersion`;
- `imagePromptVersion`;
- `styleVersion`;
- `taxonomyVersion`;
- `targetProfileVersion`;
- provider/model/config usados em cada chamada;
- checksums dos artefatos.

Versão não significa regenerar tudo automaticamente. O comando `plan` deve mostrar o impacto antes de invalidar resultados.

## Concorrência, retries e custo

- concorrência configurável e conservadora por provider;
- exponential backoff com jitter para falhas transitórias;
- máximo de tentativas por item;
- checkpoint após cada resultado válido;
- limite de itens e custo estimado antes da confirmação;
- custo observado quando o provider disponibilizar usage;
- circuit breaker quando a taxa de falha ou rejeição exceder o limite;
- `Ctrl+C` encerra após persistir respostas já recebidas.

## Segurança e privacidade

- nenhuma chave no config/manifest/log;
- nunca aceitar chave na linha de comando, pois pode parar no histórico;
- redact de headers e respostas sensíveis;
- inputs devem conter somente catálogo, sem dados de Student/Trainer;
- prompts e outputs podem ser enviados ao provider, portanto a origem precisa permitir esse uso;
- guardar provider responses somente pelo período necessário e com configuração de retenção.

## Integração Personal Ultra

O adapter deve gerar pacote, não mutation direta:

```text
personal-ultra/
  assets/training/<asset>.png
  backend/ExerciseCatalogSeed.generated.cs
  mobile/exercise-media.generated.ts
  catalog-manifest.json
  integration-report.md
```

Na aplicação ao target, o integrador decide se substitui o arquivo atual por um arquivo gerado completo ou se conecta os arquivos gerados a wrappers manuais pequenos. O gerado deve conter cabeçalho “do not edit manually” e ordenação estável.

O verifier precisa conhecer uma limitação específica do Expo: `require('../../../assets/training/foo.png')` deve ser literal em build time. Não é suficiente gerar `ImageRef` dinamicamente.

## Decisões proibidas sem revisão

- mapear grupo desconhecido para o grupo “mais parecido”;
- mesclar exercícios por similaridade de texto;
- trocar slug/ID de item existente;
- sobrescrever imagem aprovada;
- remover seed/asset ausente no input mais recente;
- publicar instruções ou imagens sem aprovação;
- aplicar no banco ou criar migration automaticamente.

