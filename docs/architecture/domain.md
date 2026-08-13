# Domain Model

## Core
- `Trainer`
- `TrainerBranding`
- `Student`
- `TrainerStudent`
- `StudentInvite`
- `Anamnesis`

`TrainerStudent` deve ser uma entidade explícita mesmo que V1 permita apenas um Trainer ativo por Student.

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
- `ImageRef` / `ImageUrl`
- optional `Instructions`
- `IsActive`

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
