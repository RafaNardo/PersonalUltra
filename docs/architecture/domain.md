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

`TrainerPrescriptionSettings` guarda somente os padrões editáveis usados como ponto de partida ao criar uma nova prescrição (`Sets`, faixa de repetições e descanso). Ela pertence ao Trainer, não ao catálogo global, e nunca altera retroativamente modelos ou treinos já salvos.

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

When a template is applied or a catalog exercise is prescribed directly to a Student, `StudentWorkoutExercise` becomes the Student-owned prescription snapshot. It should preserve enough catalog context for stable historical/display behavior even if the catalog later changes. At minimum preserve `ExerciseId` plus the display/media fields required by the Student UX as snapshot values where appropriate.

`WorkoutSessionExercise` is another execution/history snapshot and must not depend on current template content to reconstruct a past session.

The core remains relational. Do not denormalize the entire training domain. Snapshot only where immutability/history or Student-owned editing requires it.

Templates are accelerators, not the primary UX object. A Trainer must also be able to build/edit a Student workout from the catalog and add multiple exercises.

See `docs/design/trainer-training-refactor.md` for the approved Trainer flow and V1 non-goals.

## Nutrition
- `NutritionPlan`
- `Meal`
- `Food`
- `MealFood`

## Progress
- `WeightEntry` apenas na V1.

## Engagement
- `TrainerMessage`: TrainerId, StudentId, Message, StartsAt, ExpiresAt?, CreatedAt.

## Coach
- `Conversation`
- `CoachMessage`

Coach V1 é read-only.
