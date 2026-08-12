# API Contract v0.1

Base: `/api/v1`

## Core
`GET /bootstrap`, `GET /me`, `PATCH /me`

## Onboarding
`GET /onboarding`, `PUT /onboarding`, `POST /onboarding/complete`

## Plans
`POST /plans/generate`, `GET /plans/active`, `GET /plans/active/review`, `POST /plans/active/review/apply`

## Home
`GET /home`

## Training
`GET /training/today`
`POST /training/sessions/{id}/start`
`GET /training/sessions/{sessionId}/exercises/{sessionExerciseId}`
`POST /training/sessions/{sessionId}/exercises/{sessionExerciseId}/sets`
`POST /training/sessions/{sessionId}/exercises/{sessionExerciseId}/complete`
`POST /training/sessions/{sessionId}/exercises/{sessionExerciseId}/alternatives`
`POST /training/sessions/{sessionId}/exercises/{sessionExerciseId}/substitute`
`POST /training/sessions/{id}/complete`
`GET /training/history`
`GET /training/sessions/{id}`

## Health
`GET /health`, `PUT /health`, `POST /health/pain-reports`

## Nutrition
`GET /nutrition/today`
`GET /nutrition/meals/{mealId}`
`POST /nutrition/meals/{mealId}/complete`
`POST /nutrition/meals/{mealId}/foods/{foodId}/alternatives`
`POST /nutrition/meals/{mealId}/foods/{foodId}/substitute`

## Progress / Check-ins
`GET /check-ins/pending`, `POST /check-ins`
`GET /progress/summary`, `GET /progress/weight`, `POST /progress/weight`
`POST /progress/photos/upload-url`, `POST /progress/photos`

## Coach
`GET /coach/conversation`
`POST /coach/messages`
`POST /coach/actions/{actionId}/confirm`
`POST /coach/actions/{actionId}/reject`

## Sync
`POST /sync` com `clientOperationId`.

## Error Contract
`code`, `message`, `details`, `traceId`.

Códigos: VALIDATION_ERROR, NO_ACTIVE_PLAN, WORKOUT_ALREADY_STARTED, WORKOUT_ALREADY_COMPLETED, INVALID_EXERCISE_SUBSTITUTION, PAIN_REVIEW_REQUIRED, PROFESSIONAL_REVIEW_REQUIRED, SAFETY_ACTION_BLOCKED, COACH_ACTION_ALREADY_EXECUTED, SYNC_CONFLICT.
