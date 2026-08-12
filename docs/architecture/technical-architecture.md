# Technical Architecture v0.1

## Decisões
- React Native + Expo SDK 54 + TypeScript
- ASP.NET Core + C#
- PostgreSQL + EF Core
- Modular Monolith
- REST `/api/v1`
- TanStack Query
- Zustand
- SQLite offline
- LLM via backend
- pgvector futuro
- Hangfire
- OpenTelemetry

## Backend modules
Members, Plans, Training, Nutrition, Progress, Coaching, Methodology.

## Infra MVP
Uma API, um Postgres, object storage, jobs na mesma aplicação, sem Redis e sem microsserviços.

## Eventos
WorkoutCompleted, PainReported, PlanReviewDue, WeightLogged, PersonalRecordAchieved.

## Segurança
HTTPS, tokens, signed URLs, autorização por MemberId, audit log e chave de LLM apenas no backend.
