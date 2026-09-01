# Domain Model

## Core
- `Trainer`
- `TrainerBranding`
- `TrainerPrescriptionSettings`
- `Student`
- `TrainerStudent`
- `StudentInvite`
- `Anamnesis`

`TrainerStudent` deve ser uma entidade explícita mesmo que V1 permita apenas um Trainer ativo por Student.

`TrainerPrescriptionSettings` guarda somente os padrões editáveis usados como ponto de partida ao criar uma nova prescrição (`Sets`, faixa de repetições e descanso). Ela pertence ao Trainer, não ao catálogo global, e nunca altera retroativamente presets ou treinos já salvos. No domínio e na API, o preset continua representado tecnicamente por `WorkoutTemplate`.

## Nutrition

`NutritionPlan` pertence a um Student e contém refeições ordenadas. Além do
nome e das observações, pode guardar metas diárias opcionais inseridas
manualmente pelo Trainer: calorias, proteína, carboidratos e gordura. Esses
campos são referência informativa; o domínio não deriva valores nem registra
adesão.

## Training
- `Exercise`
- `WorkoutTemplate` (pertence ao Trainer)
- `WorkoutTemplateExercise`
- `StudentTrainingPlan`
- `StudentWorkout`
- `RecommendedSchedule`
- `WorkoutSession`
- `WorkoutSessionExercise`
- `SetPerformance`

Aplicar template deve copiar um snapshot editável para o Student. Alterar o template depois não altera alunos existentes.

Excluir um `StudentWorkout` no fluxo do Trainer é uma remoção lógica: ele deixa
de aparecer nas listas Trainer/Student e não pode iniciar novas sessões. O
registro permanece apenas para preservar a relação com sessões e snapshots
históricos; V1 não expõe arquivo de inativos nem reativação.

`StudentWorkout.SuggestedOrder` é a ordem persistida definida pelo Trainer para
os treinos disponíveis de um Student. Ela não representa dia, frequência ou
obrigação. Novos treinos são acrescentados ao fim. O backfill inicial foi
determinístico e os campos transitórios de agenda foram removidos no fechamento
do M3RF; a ordem sugerida é agora a única ordenação prescritiva persistida.

V1 não possui `RecommendedLoad`.

### M3 refactor addendum — catalog-backed prescription

The first M3 implementation stored exercise names directly inside template/workout items. This is insufficient for the intended Trainer UX and for reusing the curated exercise media that already existed in the donor project.

For V1, `Exercise` is the **system catalog source** and is populated only by database seed. There is no catalog admin UI in V1.

Recommended conceptual shape:

```text
Exercise catalog (system / seed)
          |
          v
WorkoutTemplateExercise ----> WorkoutTemplate (Trainer-owned)
          |
          | apply snapshot
          v
StudentWorkoutExercise -----> StudentWorkout (Student-owned prescription)
          |
          | start session snapshot
          v
WorkoutSessionExercise -----> WorkoutSession
          |
          v
SetPerformance
```

`Exercise` should have, at minimum:
- `Id`
- `Name`
- `Slug` or another stable key if useful for seed/assets
- `PrimaryMuscleGroup`
- optional equipment/category metadata
- `ImageRef` estável e provider-neutral (`media://exercise-catalog/delivery/v1/...`); URLs assinadas nunca são persistidas
- optional `Instructions`
- `IsActive`

`ImageUrl` não pertence ao domínio nem aos snapshots. Para referências remotas,
ela é assinada temporariamente pela Infrastructure e adicionada somente aos
contratos HTTP de Trainer/Student.

Video is V2. A nullable `VideoUrl` may exist only if it is cheap and creates no V1 workflow.

`WorkoutTemplateExercise` must reference `ExerciseId` and keep prescription data separate from catalog data:
- `Sequence`
- `Sets`
- `RepetitionsMin`
- `RepetitionsMax`
- `RestSeconds`
- `Notes`

`Exercise.DefaultTrackingMode` define se o catálogo sugere acompanhamento por
`Repetitions` ou `Duration`. `TrackingMode` e `TargetDurationSeconds` são
copiados nos snapshots de preset, prescrição e sessão para que mudanças futuras
no catálogo não reinterpretem um treino existente. `SetPerformance` guarda
carga/repetições ou duração real conforme o modo; os dois formatos não são
misturados no mesmo exercício.

When a template is applied or a catalog exercise is prescribed directly to a Student, `StudentWorkoutExercise` becomes the Student-owned prescription snapshot. It should preserve enough catalog context for stable historical/display behavior even if the catalog later changes. At minimum preserve `ExerciseId` plus the display/media fields required by the Student UX as snapshot values where appropriate.

`WorkoutSessionExercise` is another execution/history snapshot and must not depend on current template content to reconstruct a past session.

`WorkoutSessionExercise.ConfirmedCompletedAt` representa a confirmação explícita
de que o exercício foi realizado sem todos os registros detalhados. Ela nunca
gera performances sintéticas. Assim, histórico e resumo podem distinguir fatos
medidos de uma confirmação manual honesta.

The core remains relational. Do not denormalize the entire training domain. Snapshot only where immutability/history or Student-owned editing requires it.

Templates are accelerators, not the primary UX object. A Trainer must also be able to build/edit a Student workout from the catalog and add multiple exercises.

See `docs/design/trainer-training-refactor.md` for the approved Trainer flow and V1 non-goals.

## Nutrition
- `NutritionPlan`
- `Meal`
- `MealFood`
- `NutritionTemplate` (pertence ao Trainer)
- `NutritionTemplateMeal`
- `NutritionTemplateFood`

Na demo, `MealFood` é um item textual ordenado da prescrição, com quantidade e
unidade. Não existe catálogo global de alimentos, macros ou geração automática.
`NutritionPlan` pertence a um Student, guarda o Trainer responsável atual e a
rastreabilidade mínima de criação/última alteração. Salvar substitui o documento
completo de forma atômica e o Student permanece read-only. A direção de UX e os
non-goals estão em `docs/design/nutrition-experience-review.md`.

Cada preset representa exatamente uma refeição reutilizável, como `Café com
ovos`, e contém seus itens ordenados. Aplicá-lo cria IDs novos e acrescenta um
snapshot independente ao `NutritionPlan`; editar ou excluir o preset não
modifica alunos nem remove refeições existentes. O container técnico
`NutritionTemplate` permanece compatível com a migration já publicada, mas sua
invariante de aplicação é uma única `NutritionTemplateMeal`.

`MealFood.Unit = "livre"` representa porção sem quantidade fixa, como salada à
vontade. O valor decimal continua preenchido com `1` para preservar o schema e
as validações existentes, mas não é exibido ao usuário nessa modalidade.

## Progress
- `WeightEntry` apenas na V1.

## Engagement
- `TrainerMessage`: TrainerId, StudentId, Message, StartsAt, ExpiresAt?, CreatedAt.
- `ChatMessage`: TrainerId, StudentId, Sender (`Student` ou `Trainer`), Content,
  CreatedAt.
