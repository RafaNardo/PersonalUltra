# Handoff atual — Personal Ultra

Atualizado em 2026-08-24 para iniciar uma nova thread sem carregar o histórico
completo. Depois de `AGENTS.md`, este é o primeiro documento a ler. Consulte
somente os documentos citados pela tarefa seguinte; não reconstrua milestones já
concluídas a partir da conversa antiga.

## Estado do repositório

- Repositório: `C:\git\PresonalUltra`; branch publicada: `main`.
- Últimos commits funcionais: `3978c54` (presets de alimentação no domínio/API),
  `e70e946` (workspace de alimentação Trainer) e `19b2d32` (guards declarativos
  de sessão Student).
- Stack: .NET 10, EF Core/PostgreSQL, Expo/React Native/Expo Router, TypeScript,
  TanStack Query, Zustand e SQLite offline.
- APIs públicas:
  - Student: `https://student-api-production-a4fe.up.railway.app`
  - Trainer: `https://trainer-api-production-b0f7.up.railway.app`
- As duas APIs compartilham Domain, Application, Infrastructure, DbContext e o
  mesmo PostgreSQL. Não criar outra API, banco ou microserviço.
- O mobile é um único app Expo com árvores `features/trainer`,
  `features/student` e `shared`, preparado para split físico futuro. Não criar
  imports cruzados entre features dos atores.

## Produto e decisões fixadas

- É product demo primeiro: fluxo real, UX clara e validação rápida têm prioridade
  sobre infraestrutura de produção.
- A autenticação atual é deliberadamente demo-only. Não antecipar auth real.
- `preset de treino` é o nome de UI; `WorkoutTemplate`/`templates` continuam como
  nomes técnicos compatíveis no domínio/API.
- Treinos não pertencem a dias da semana e não existe “treino obrigatório” ou
  recomendado. `SuggestedOrder` é somente a ordem organizada pelo Trainer.
- O Student pode escolher qualquer treino e qualquer exercício pendente.
- Exercícios usam `Repetitions` ou `Duration`. Cardio/isometrias seeded começam
  temporizados; Trainer pode configurar modo, blocos e duração.
- Registro e descanso Student usam a mesma tela, mantendo imagem/contexto e
  alternando o painel inferior.
- Conclusão rápida por exercício ou sessão persiste confirmação explícita e nunca
  cria carga, repetição, duração ou `SetPerformance` fictícia.
- Imagens de exercício usam `contain`; precisão visual tem prioridade sobre
  preencher o frame.
- Coach V1 é estritamente read-only.
- Empty states seguem `docs/design/design-system.md` e devem ser acolhedores no
  Student e operacionais no Trainer.

## Entregas recentes publicadas

- Catálogo remoto: 231 exercícios, masters no bucket privado Railway, derivados
  WebP 640×640, referências persistidas `media://` e URLs HTTPS assinadas somente
  nas respostas. Expo usa `expo-image`, cache em disco por `ImageRef` e
  placeholder.
- Os 28 desenhos herdados foram removidos do bundle e substituídos por imagens
  remotas v3. Commit principal: `5b19580`.
- UI Trainer apresenta presets e configuração visual com imagem. Commit
  `dc4d89b`.
- Execução multimodal/fluida M3RX, migration, histórico factual e conclusão sem
  dados sintéticos. Commit `c7b949b`; design em
  `docs/design/training-execution-refinement.md`.
- Gate de regressão API: comando `npm run test:api:regression`, atualmente 105
  testes. Commits-base `ab4c3d4` e `c41ae7b`; cobertura de nutrição em
  `2a1ef83`; detalhes em
  `docs/testing/api-regression.md`.
- Revisão de nutrição: documento multi-refeição/multi-item com quantidade e
  unidade, ordem persistida, autoria/atualização, validação atômica, editor
  Trainer completo e leitura Student read-only. Commits `2a1ef83` e `23e5b4c`;
  direção em `docs/design/nutrition-experience-review.md`.
- Presets de alimentação Trainer-owned com CRUD/duplicação e aplicação por
  snapshot, mais bottom nav e seção `Alimentação` no detalhe do aluno. Commits
  `3978c54` e `e70e946`.
- Telas Student sem sessão usam `Redirect` declarativo; não chamar
  `router.replace` durante render. Correção `19b2d32`.

## Validação padrão

Para mudança somente de API/domínio:

```powershell
npm run test:api:regression
```

Para mudança mobile, acrescentar:

```powershell
npm run mobile:typecheck
cd apps/mobile
npx expo export --platform ios --output-dir .expo-export-validation
```

O último gate conhecido passou com 105/105 testes de API, 136/136 testes da
Factory, build .NET sem warnings, typecheck e export iOS. As duas APIs públicas
responderam health 200 e os contratos públicos retornaram `trackingMode`.

O workflow `.github/workflows/api-regression.yml` está pronto, mas somente com
`workflow_dispatch`: o GitHub bloqueou o primeiro job antes de executá-lo por
uma pendência de billing da conta. Não reativar gatilhos automáticos até o
Actions ser desbloqueado, para não produzir falhas falsas em todo push.

## Exercise Catalog Factory

A Factory .NET 10 em `tools/PersonalUltra.ExerciseCatalogFactory` está funcional
e já cumpriu geração, aprovação, upload, seed/export, URLs assinadas, cache e
substituição dos assets legados. O handoff técnico detalhado está em
`docs/projects/exercise-catalog-factory/codex-handoff.md`.

O intake original ainda registra 12 itens `needs_review` e 11 identidades
legadas ambíguas. Não resolver, mesclar IDs nem gastar em nova geração sem tarefa
e aprovação explícitas. Secrets permanecem somente em User Secrets/Railway e
nunca devem ser lidos ou impressos.

## Revisão de nutrição concluída

- O fluxo não contém mocks: Trainer e Student usam o mesmo plano persistido no
  `DbContext` compartilhado.
- Salvar é uma substituição integral e atômica que disponibiliza a versão ao
  Student; rascunho/versionamento não foram introduzidos.
- `MealFood` é um item textual ordenado, sem catálogo global, macros ou geração.
- `NutritionTemplate` replica essa estrutura para reutilização pelo Trainer;
  aplicar cria um snapshot independente e substituir exige confirmação.
- Atribuição identifica o Trainer responsável sem alegar credencial clínica.
- A revisão jurídica/produto continua obrigatória antes de produção, conforme
  `docs/product/nutrition-note.md`.
- A lacuna explícita restante no gate é Weight API.

## Arquivos locais do usuário

No momento do handoff existem estes itens untracked. Preservar e não incluir em
commits sem pedido explícito:

- `approved-images-v2.txt`
- `approved-images-v3.txt`
- `apps/api/TrainerApi/Properties/`
- `apps/backend/PersonalUltra.Application/Properties/`
- `apps/backend/PersonalUltra.Infrastructure/Properties/`
- `docs/projects/exercise-catalog-factory.zip`

## Forma de trabalhar daqui em diante

- Manter mudanças pequenas e diretamente ligadas à task.
- Usar o gate automatizado em vez de reinspecionar toda a conversa ou repetir
  manualmente regras já cobertas.
- Não abrir novas milestones enterprise nem adicionar mocks transitórios.
- Para cada entrega: implementar, revisar o diff, executar os gates relevantes,
  commitar intencionalmente e fazer push quando solicitado.
- Relatar qualquer bloqueio real; não parar por detalhes que podem ser
  descobertos no repositório.

## Prompt curto para a nova thread

```text
Read AGENTS.md and docs/delivery/current-codex-handoff.md completely. Treat the
handoff as the authoritative current state and do not reconstruct completed work
from older milestone histories. The nutrition review is complete; wait for the
next scoped product task. Preserve the listed untracked user files.
```
