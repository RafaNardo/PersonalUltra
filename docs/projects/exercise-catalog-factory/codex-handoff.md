# Codex handoff — Exercise Catalog Factory

Use este documento numa nova thread e, preferencialmente, num repositório vazio dedicado ao robô.

## Contexto que deve acompanhar o novo projeto

Copiar todo o diretório `docs/projects/exercise-catalog-factory/`. Se o novo agente também puder ler o Personal Ultra, fornecer como referências somente leitura:

- `apps/backend/PersonalUltra.Domain/Domain/TrainingEntities.cs`;
- `apps/backend/PersonalUltra.Infrastructure/Infrastructure/ExerciseCatalogSeed.cs`;
- `apps/backend/PersonalUltra.Infrastructure/Infrastructure/DemoDataSeeder.cs`;
- `apps/mobile/src/shared/training/exercise-media.ts`;
- alguns assets representativos de `apps/mobile/assets/training/`.

Não copiar segredos, `.env`, banco ou dados de Student/Trainer.

## Prompt inicial recomendado

```text
Read every document under docs/projects/exercise-catalog-factory before making changes.

Implement PU-ECF-001 only.

This is a standalone, run-on-demand exercise catalog factory. It will later
accept roughly 200 exercise names, use OpenAI through isolated provider adapters
to propose structured metadata and generate one reviewed illustration per
exercise, then export deterministic seed/assets/static Expo media bindings for
Personal Ultra.

Critical rules:
- dry-run by default;
- never write directly to PostgreSQL;
- never publish or export unreviewed metadata/images;
- preserve stable external keys, slugs and target IDs;
- make runs resumable and idempotent;
- keep OpenAI model IDs/config out of the domain;
- never log or commit API keys;
- do not implement later ECF milestones early.

At the end report files changed, commands/tests run, decisions made and anything
that must be resolved before PU-ECF-002. Do not continue automatically.
```

Depois da fundação, repetir o padrão trocando apenas o ID da milestone. Não pedir que um único agente implemente ECF-001–012 de uma vez.

## Sequência recomendada

```text
ECF-001 baseline CLI
ECF-002 schemas/manifest
ECF-003 normalization/dedupe
ECF-004 OpenAI text enrichment
ECF-005 metadata review
ECF-006 image pilot
ECF-007 batch/media QA
ECF-008 visual/biomechanical review
ECF-009 Personal Ultra exporter
ECF-010 target verifier/apply
ECF-011 incremental rerun
ECF-012 end-to-end rehearsal/runbook
```

## Recomendações de orquestração

- ECF-001–003 em sequência: identidade/manifesto são fundação.
- ECF-004 e o desenho inicial de ECF-006 podem ser pesquisados em paralelo, mas geração paga só após ECF-005 e congelamento do style pilot.
- ECF-007–008 dependem do piloto visual aceito.
- ECF-009–010 dependem de manifesto estável e leitura atual do target.
- ECF-011–012 são gates reais, não documentação opcional.

## O que não assumir

- não assumir que “GPT” significa um model ID fixo; confirmar documentação oficial ao implementar adapters;
- não assumir que 200 nomes são 200 exercícios únicos;
- não assumir que a taxonomia atual cobre todo o lote;
- não assumir que uma imagem bonita é biomecanicamente correta;
- não assumir que criar o PNG basta: o Expo atual exige `require` estático;
- não assumir que um novo run representa o catálogo completo ou autoriza remoções;
- não assumir permissão para usar material de terceiros como referência visual.

## Primeira execução real

Antes do lote completo:

1. importar a lista inteira apenas para relatório de qualidade/duplicidade;
2. resolver taxonomia e conflitos;
3. enriquecer e revisar um lote pequeno;
4. gerar piloto visual de 5–10 itens variados;
5. validar esses assets dentro do Personal Ultra;
6. congelar `styleVersion`;
7. estimar custo e confirmar orçamento;
8. gerar o restante em lotes retomáveis;
9. revisar antes de exportar;
10. aplicar em clone/branch e executar o gate completo.

## Resultado esperado do handoff

Ao concluir ECF-012, o operador consegue adicionar exercícios futuramente repetindo um run incremental. O robô gera um pacote revisável e determinístico; uma PR no Personal Ultra continua sendo o ponto de publicação e validação.

