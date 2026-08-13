export type ActualSetPerformance = {
  weightKg: number;
  repetitions: number;
};

export type ActualSetPerformanceResult =
  | { success: true; value: ActualSetPerformance }
  | { success: false; message: string };

export function parseActualSetPerformance(weight: string, repetitions: string): ActualSetPerformanceResult {
  const normalizedWeight = weight.trim().replace(',', '.');
  if (!normalizedWeight) return { success: false, message: 'Informe a carga realizada. Use 0 para exercícios sem carga.' };

  const weightKg = Number(normalizedWeight);
  if (!Number.isFinite(weightKg) || weightKg < 0) return { success: false, message: 'Informe uma carga válida, igual ou maior que zero.' };

  const normalizedRepetitions = repetitions.trim();
  if (!normalizedRepetitions) return { success: false, message: 'Informe quantas repetições você realizou.' };

  const actualRepetitions = Number(normalizedRepetitions);
  if (!Number.isInteger(actualRepetitions) || actualRepetitions < 1) return { success: false, message: 'As repetições realizadas devem ser um número inteiro maior que zero.' };

  return { success: true, value: { weightKg, repetitions: actualRepetitions } };
}
