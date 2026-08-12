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
- Coach model abstraction;
- generic UI primitives.

## Adapt
- Member -> Student;
- Plan -> trainer-owned StudentTrainingPlan;
- WorkoutTemplate -> Trainer template + Student copied workout;
- Home -> actor-specific;
- Progress -> weight only;
- Coach -> read-only;
- Nutrition -> Trainer/Student surfaces.

## Remove from V1
- SVR branding;
- MethodologyVersion/Rule;
- StandardPlanProvisioner;
- automatic progression;
- recommended load;
- CoachAction writes;
- automatic exercise substitution;
- progress photos.

## Add
- Trainer;
- TrainerBranding;
- TrainerStudent;
- StudentInvite;
- Anamnesis;
- Trainer dashboard;
- workout template library/editor;
- TrainerMessage;
- WhatsApp deep link;
- recommended schedule;
- actor-specific API surfaces.

Não copiar o donor repo mecanicamente. Portar capacidades intencionalmente para não carregar premissas do produto antigo.
