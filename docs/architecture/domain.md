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
