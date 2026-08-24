export type ExercisePrescriptionDraft = {
  trackingMode: 'Repetitions' | 'Duration';
  sets: string;
  repetitionsMin: string;
  repetitionsMax: string;
  restSeconds: string;
  targetDurationSeconds: string;
  notes: string;
};

export type ExercisePrescriptionErrors = Partial<Record<keyof ExercisePrescriptionDraft, string>>;

export const initialExercisePrescription: ExercisePrescriptionDraft = {
  trackingMode: 'Repetitions',
  sets: '3',
  repetitionsMin: '8',
  repetitionsMax: '12',
  restSeconds: '60',
  targetDurationSeconds: '600',
  notes: '',
};

export function prescriptionDraftFromDefaults(defaults: { sets: number; repetitionsMin: number; repetitionsMax: number; restSeconds: number }): ExercisePrescriptionDraft {
  return {
    trackingMode: 'Repetitions',
    sets: String(defaults.sets),
    repetitionsMin: String(defaults.repetitionsMin),
    repetitionsMax: String(defaults.repetitionsMax),
    restSeconds: String(defaults.restSeconds),
    targetDurationSeconds: '600',
    notes: '',
  };
}

export function validateExercisePrescription(draft: ExercisePrescriptionDraft): ExercisePrescriptionErrors {
  const errors: ExercisePrescriptionErrors = {};
  const sets = integer(draft.sets);
  const repetitionsMin = integer(draft.repetitionsMin);
  const repetitionsMax = integer(draft.repetitionsMax);
  const restSeconds = integer(draft.restSeconds);
  const targetDurationSeconds = integer(draft.targetDurationSeconds);

  if (sets === undefined || sets < 1 || sets > 20) errors.sets = 'Use um valor inteiro entre 1 e 20.';
  if (draft.trackingMode === 'Repetitions') {
    if (repetitionsMin === undefined || repetitionsMin < 1 || repetitionsMin > 100) errors.repetitionsMin = 'Use um valor inteiro entre 1 e 100.';
    if (repetitionsMax === undefined || repetitionsMax < 1 || repetitionsMax > 100) errors.repetitionsMax = 'Use um valor inteiro entre 1 e 100.';
    if (repetitionsMin !== undefined && repetitionsMax !== undefined && repetitionsMin > repetitionsMax) errors.repetitionsMax = 'A repetição máxima deve ser maior ou igual à mínima.';
  } else if (targetDurationSeconds === undefined || targetDurationSeconds < 5 || targetDurationSeconds > 86400) errors.targetDurationSeconds = 'Use uma duração entre 5 segundos e 24 horas.';
  if (restSeconds === undefined || restSeconds < 0 || restSeconds > 900) errors.restSeconds = 'Use um valor inteiro entre 0 e 900 segundos.';
  if (draft.notes.length > 1000) errors.notes = 'Use no máximo 1.000 caracteres.';

  return errors;
}

export function hasPrescriptionErrors(errors: ExercisePrescriptionErrors) {
  return Object.keys(errors).length > 0;
}

export function parseExercisePrescription(draft: ExercisePrescriptionDraft) {
  if (hasPrescriptionErrors(validateExercisePrescription(draft))) return undefined;
  return {
    sets: Number(draft.sets.trim()),
    repetitionsMin: draft.trackingMode === 'Repetitions' ? Number(draft.repetitionsMin.trim()) : 1,
    repetitionsMax: draft.trackingMode === 'Repetitions' ? Number(draft.repetitionsMax.trim()) : 1,
    trackingMode: draft.trackingMode,
    targetDurationSeconds: draft.trackingMode === 'Duration' ? Number(draft.targetDurationSeconds.trim()) : undefined,
    restSeconds: Number(draft.restSeconds.trim()),
    notes: draft.notes.trim(),
  };
}

function integer(value: string) {
  const normalized = value.trim();
  if (!/^\d+$/.test(normalized)) return undefined;
  const parsed = Number(normalized);
  return Number.isSafeInteger(parsed) ? parsed : undefined;
}
