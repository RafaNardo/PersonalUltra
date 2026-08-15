# Exercise Catalog Factory

Status: **fundação e contratos v1 implementados**. A aplicação Console .NET 10
já existe em `tools/PersonalUltra.ExerciseCatalogFactory`, com dry-run,
manifesto local versionado, retomada atômica, diagnóstico e testes. Adapters
OpenAI/S3, geração e publicação continuam pendentes e não fazem parte desta
etapa.

## Objetivo

Criar uma ferramenta de linha de comando, executável sob demanda, que receba uma lista de exercícios — inicialmente cerca de 200 — e produza um pacote revisável para alimentar o catálogo do Personal Ultra:

- metadados normalizados e enriquecidos com IA;
- uma ilustração consistente por exercício;
- manifesto versionado e retomável;
- seed determinístico e idempotente;
- registro estático de imagens exigido pelo Expo/Metro;
- relatório de conflitos, rejeições e itens que exigem decisão humana.

A primeira execução deve popular a base. Execuções futuras devem acrescentar ou revisar itens sem regenerar tudo, trocar IDs ou sobrescrever silenciosamente dados já aprovados.

## História de uso

```text
Operador fornece CSV/JSON com ~200 exercícios
  → robô valida e normaliza
  → IA propõe metadados estruturados
  → operador aprova metadados e duplicidades
  → gerador cria as ilustrações pendentes
  → operador revisa anatomia, equipamento e enquadramento
  → exporter gera assets + seed + media registry + relatório
  → pacote é copiado para o Personal Ultra e validado em PR
```

IA auxilia a produção; ela não é autoridade sobre biomecânica, nomenclatura nem publicação. Nenhum item entra no seed final sem aprovação explícita.

## Compatibilidade atual do Personal Ultra

O adapter inicial deve conhecer estes contratos, sem acoplar o núcleo da ferramenta a eles:

- entidade `Exercise`: `Id`, `Name`, `Slug`, `PrimaryMuscleGroup`, `Equipment`, `ImageRef`, `Instructions`, `IsActive`;
- `Slug` é chave única e estável do seed;
- assets ficam em `apps/mobile/assets/training/*.png`;
- `ImageRef` usa `assets/training/<arquivo>.png`;
- o Metro exige um `require(...)` estático por imagem em `exercise-media.ts`;
- o seed atual está em `ExerciseCatalogSeed.cs` e só adiciona slugs ausentes;
- nomes de grupo muscular precisam ser compatíveis com os filtros do app ou gerar um alerta de integração.

O formato visual existente é uma ilustração esportiva estilizada, majoritariamente em 1024 × 1536, com fundo escuro neutro e acentos quentes. “Sprite” neste projeto significa **uma arte raster por exercício**, não um atlas/sprite sheet.

## Princípios

- **Dry-run primeiro:** nenhuma execução escreve no repositório alvo ou no banco por padrão.
- **Artefatos, não mutations:** o robô gera um pacote para revisão/PR; não acessa PostgreSQL diretamente.
- **Determinismo:** slug, ID, nomes de arquivo e ordenação não mudam entre execuções equivalentes.
- **Retomada:** cada etapa grava estado; uma falha no item 137 não repete os 136 anteriores.
- **Versionamento explícito:** schema, pipeline, prompts e estilo visual fazem parte do manifesto.
- **Revisão humana:** duplicidades semânticas, instruções e imagens exigem aprovação.
- **Custos controlados:** orçamento, concorrência, retries e limite máximo de imagens são obrigatórios.
- **Provider isolado:** OpenAI entra por adapters; domínio e manifesto não dependem de um model ID.
- **Segredos fora do Git:** `OPENAI_API_KEY` somente via ambiente/secret store e nunca em logs ou manifests.

## Non-goals

- admin web do catálogo;
- escrita direta no banco de produção;
- geração automática de treinos ou prescrição;
- recomendação de carga;
- diagnóstico clínico ou garantia médica;
- vídeo/animação;
- publicação automática sem revisão;
- scraping não autorizado de imagens, textos ou bases protegidas;
- substituir avaliação de um profissional de educação física.

## Documentos

- [backlog.md](backlog.md): milestones executáveis e seus gates.
- [architecture.md](architecture.md): desenho do robô, estado e comandos.
- [contracts.md](contracts.md): contratos de entrada, manifesto e pacote de saída.
- [image-style-guide.md](image-style-guide.md): consistência visual e QA das artes.
- [validation.md](validation.md): critérios técnicos, humanos e de integração.
- [codex-handoff.md](codex-handoff.md): prompt e sequência para iniciar o outro repositório/thread.
- [personal-ultra-integration-milestone.md](personal-ultra-integration-milestone.md): proposta faseada para factory .NET, bucket privado e consumo remoto no app.
- [exercise-inventory-v1.md](exercise-inventory-v1.md): inventário de 232 candidatos para revisão antes de qualquer geração paga.

## Decisões que precisam ser confirmadas antes da primeira geração paga

1. Taxonomia definitiva de grupos musculares para os 200 itens.
2. Se o personagem visual será único, alternado ou neutro por exercício.
3. Se todo o catálogo novo seguirá 1024 × 1536 ou se haverá uma migração coordenada de proporção.
4. Quem fará a aprovação biomecânica e qual evidência ficará registrada.
5. Política para corrigir um exercício já publicado: preservar slug/ID e gerar nova `contentVersion`, ou criar outro item.
