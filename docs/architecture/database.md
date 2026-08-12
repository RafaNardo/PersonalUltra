# Database Model v0.1

## Schemas
`auth`, `members`, `methodology`, `plans`, `training`, `nutrition`, `progress`, `coaching`, `audit`.

## Principais tabelas
- auth.users
- members.members
- members.profiles (dados declarados e progresso do onboarding; sem decisão clínica)
- members.health_conditions
- members.pain_reports
- members.movement_restrictions
- methodology.versions
- methodology.rules
- plans.plans
- training.exercises
- training.exercise_alternatives
- training.training_plans
- training.workout_templates
- training.workout_template_exercises
- training.workout_sessions
- training.workout_session_exercises
- training.set_performances
- training.progression_decisions
- nutrition.nutrition_plans
- nutrition.foods
- nutrition.meal_templates
- nutrition.meal_template_foods
- nutrition.daily_logs
- progress.weight_entries
- progress.measurements
- progress.progress_photos
- progress.check_ins
- coaching.conversations
- coaching.messages
- coaching.tool_calls
- coaching.actions
- coaching.safety_decisions
- audit.logs

## Regras
UUID como PK; timestamptz/DateTimeOffset para instantes; numeric para peso/carga; enums string no MVP; JSONB só para regras/metadata/snapshots; histórico preserva snapshots; Plan e Methodology versionados.
