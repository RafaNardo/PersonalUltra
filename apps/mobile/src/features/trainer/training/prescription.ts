export type ExercisePrescriptionDraft = {
  sets: string;
  repetitionsMin: string;
  repetitionsMax: string;
  restSeconds: string;
  notes: string;
};

export type ExercisePrescriptionErrors = Partial<Record<keyof ExercisePrescriptionDraft, string>>;

export const initialExercisePrescription: ExercisePrescriptionDraft = {
  sets: '3',
  repetitionsMin: '8',
  repetitionsMax: '12',
  restSeconds: '60',
  notes: '',
};

export function validateExercisePrescription(draft: ExercisePrescriptionDraft): ExercisePrescriptionErrors {
  const errors: ExercisePrescriptionErrors = {};
  const sets = integer(draft.sets);
  const repetitionsMin = integer(draft.repetitionsMin);
  const repetitionsMax = integer(draft.repetitionsMax);
  const restSeconds = integer(draft.restSeconds);

  if (sets === undefined || sets < 1 || sets > 20) errors.sets = 'Use um valor inteiro entre 1 e 20.';
  if (repetitionsMin === undefined || repetitionsMin < 1 || repetitionsMin > 100) errors.repetitionsMin = 'Use um valor inteiro entre 1 e 100.';
  if (repetitionsMax === undefined || repetitionsMax < 1 || repetitionsMax > 100) errors.repetitionsMax = 'Use um valor inteiro entre 1 e 100.';
  if (repetitionsMin !== undefined && repetitionsMax !== undefined && repetitionsMin > repetitionsMax) errors.repetitionsMax = 'A repetição máxima deve ser maior ou igual à mínima.';
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
    repetitionsMin: Number(draft.repetitionsMin.trim()),
    repetitionsMax: Number(draft.repetitionsMax.trim()),
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
