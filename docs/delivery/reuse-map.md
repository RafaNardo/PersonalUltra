# Reuse Map — svr-method -> Personal Ultra

## Keep / Port
- Expo setup;
- Expo Router;
- TanStack Query;
- Zustand;
- React Hook Form + Zod;
- SQLite/offline workout sync;
- rest timer;
- workout execution;
- SetPerformance;
- Exercise catalog/assets;
- nutrition UI primitives;
- API conventions;
- Postgres + EF;
- dev seed/reset patterns;
- human chat persistence;
- generic UI primitives.

## Adapt
- Member -> Student;
- Plan -> trainer-owned StudentTrainingPlan;
- WorkoutTemplate -> Trainer template + Student copied workout;
- Home -> actor-specific;
- Progress -> weight only;
- Chat -> conversa humana persistida;
- Nutrition -> Trainer/Student surfaces.

## Remove from V1
- SVR branding;
- MethodologyVersion/Rule;
- StandardPlanProvisioner;
- automatic progression;
- recommended load;
- Coach e qualquer resposta automática;
- automatic exercise substitution;
- progress photos (removed in `PU-M0-006`).

## Add
- Trainer;
- TrainerBranding;
- TrainerStudent;
- StudentInvite;
- Anamnesis;
- Trainer dashboard;
- workout template library/editor;
- TrainerMessage;
- ChatMessage;
- WhatsApp deep link;
- recommended schedule;
- actor-specific API surfaces.

Não copiar o donor repo mecanicamente. Portar capacidades intencionalmente para não carregar premissas do produto antigo.
