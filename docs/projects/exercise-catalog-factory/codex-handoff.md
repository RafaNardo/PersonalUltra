# Handoff autoritativo — Exercise Catalog Factory

Atualizado em 2026-08-24. Este é o resumo operacional para retomar o trabalho
sem reler o histórico da conversa. As especificações continuam nos demais
arquivos deste diretório; não duplicá-las aqui.

## Fonte de verdade e limites

- Repositório: `C:\git\PresonalUltra`, branch `main`.
- A sequência executável atual é `PU-ECF-001`–`012` de
  [personal-ultra-integration-milestone.md](personal-ultra-integration-milestone.md).
  O [backlog.md](backlog.md) registra o desenho original e usa números diferentes
  a partir de ECF-006; em caso de conflito, prevalece a milestone de integração.
- A factory é uma CLI .NET 10 em
  `tools/PersonalUltra.ExerciseCatalogFactory`; não é API, microserviço nem
  acesso direto ao PostgreSQL.
- Dry-run é o padrão. Não gerar conteúdo pago, publicar imagem, alterar seed ou
  integrar ao app sem passar pelos gates de revisão correspondentes.
- Nunca ler, imprimir, versionar ou pedir secrets na conversa. Eles entram
  somente no User Secrets próprio da factory.
- Preservar `docs/projects/exercise-catalog-factory.zip`: é artefato local do
  usuário, não deve ser commitado.

## Estado implementado

| Item | Estado | Commit |
|---|---|---|
| ECF-001 — CLI, configuração, logs, run/resume e dry-run | concluído | `551997e` |
| ECF-002 — contratos v1, manifesto e checkpoint atômico | concluído | `e57aa2d` |
| ECF-003 — intake determinístico e identidades | código concluído; gate humano aberto | `d4745e0` |
| ECF-004 — enriquecimento OpenAI estruturado e retomável | concluído; nenhuma chamada paga executada | `72e98d3` |
| ECF-006 — adapter e smoke privado S3 | concluído; smoke real aprovado | `d0f940c` |
| ECF-007/009 — geração e publicação visual v2 | 220 imagens aprovadas e publicadas | `acd0b5e` |
| ECF-010 — exporter Personal Ultra | 203 entradas novas geradas; legado preservado | `acd0b5e` |
| ECF-011 — URLs assinadas e cache Expo | concluído; aguardando deploy | próximo commit |
| ECF-012 — apply e validação | PostgreSQL/seed idempotente validados localmente | próximo commit |

O lote visual v2 contém 220 imagens aprovadas e publicadas sob
`exercise-catalog/v2/<slug>.png`. O exporter gerou 203 exercícios novos e o
catálogo resultante contém 231 itens: 28 legados locais mais 203 novos com
referência `media://`. As APIs retornam `imageUrl` assinada sem alterar a
referência persistida; o Expo usa cache em disco por `ImageRef` e placeholder.

O intake contém 232 candidatos, preserva exatamente os 28 GUIDs/slugs legados,
usa UUID v5 para itens novos e produz catálogo/relatório retomáveis. Resultado
atual: 220 normalizados, 12 em `needs_review`, 13 relações de ambiguidade;
17 identidades legadas têm vínculo exato e 11 continuam sem resolução. O
equipamento dos 232 itens não foi inferido porque a fonte não o fornece por
linha; isso pertence ao enriquecimento/review, não a um preenchimento fictício.

## Bloqueios e gates abertos

1. **Catálogo:** aprovar taxonomia e resolver as 11 identidades legadas ambíguas
   listadas em [exercise-inventory-v1.md](exercise-inventory-v1.md), incluindo
   stiff/RDL, afundo, passada, remada baixa, puxada dorsal, desenvolvimento,
   rosca direta, abdução com elástico e agachamento sumô. Não mesclar nem trocar
   IDs por heurística.
2. **Conteúdo:** definir revisor biomecânico. Metadados gerados ficam pendentes
   até aprovação explícita.
3. **Deploy:** antes do push que dispara Railway, configurar em `StudentApi` e
   `TrainerApi` as três Variable References `RailwayBucket__BucketName`,
   `RailwayBucket__AccessKeyId` e `RailwayBucket__SecretAccessKey`. No primeiro
   deploy do banco limpo, subir Student primeiro com seed ativo e Trainer depois
   com seed inativo.

## Retomada imediata

Em 2026-08-24, `bucket doctor` e o smoke real passaram no endpoint
`t3.storageapi.dev`, em virtual-hosted style. Foram validados PUT, HEAD, GET
autenticado, GET assinado, bytes, SHA-256, MIME, DELETE da chave exata e
`NotFound` final; o cleanup foi confirmado e nenhum objeto permaneceu. O erro
anterior `InvalidAccessKeyId` foi resolvido após a atualização da configuração.

Retomada imediata:

1. Configurar as três referências do bucket nas duas APIs Railway.
2. Fazer deploy sequencial sobre o banco vazio e validar 231 exercícios, uma
   `imageUrl` PNG real e os fluxos Trainer/Student.
3. Iniciar a auditoria de nutrição já enfileirada abaixo.

Antes de cada avanço: implementar, revisar, testar, fazer commit intencional e
push. Parar em falha externa, gasto pago ou decisão humana. Os comandos de
fechamento estão em [validation.md](validation.md).

## Decisões já fixadas

- Novas imagens ficam remotas no bucket privado; os 28 assets legados continuam
  no bundle para compatibilidade histórica.
- Persistir `media://exercise-catalog/...`, nunca URL assinada. Infrastructure
  compartilha apenas o resolver; TrainerApi e StudentApi mantêm contratos
  próprios.
- O Expo usa primitive compartilhada, cache em disco e fallback/placeholder
  offline, mantendo separadas as features Trainer e Student.
- Seed/export é determinístico e gera pacote revisável; não escreve no banco.
- Nenhum mock deve preencher lacunas de dados ou chegar ao produto.

## Trabalho posterior enfileirado

Depois de concluir integralmente bucket + factory + integração do catálogo,
revisar o módulo de nutrição nas visões Trainer e Student. A tarefa inclui:

- auditar API, persistência e UI para localizar mocks, dados estáticos e fluxos
  incompletos;
- pesquisar padrões atuais de apps de prescrição alimentar;
- documentar um plano alinhado à UX acolhedora, explicativa e demo-first do app;
- implementar apenas dentro do módulo de nutrição e suas telas Trainer/Student.

Se a revisão exigir mudanças amplas de domínio, infraestrutura compartilhada ou
outros módulos, não executar automaticamente: documentar o impacto e aguardar o
usuário. Essa revisão ainda não começou.
