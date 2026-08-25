import { ApiError } from './shared-http';

const baseUrl = (process.env.EXPO_PUBLIC_TRAINER_API_URL ?? 'https://trainer-api-production-b0f7.up.railway.app').replace(/\/$/, '');
const apiUrl = `${baseUrl}/api/v1`;

export type TrainerDashboard = {
  trainerName: string;
  activeStudents: number;
  pendingAnamneses: number;
  completedAnamneses: number;
  recentStudents: Array<{
    studentId: string;
    firstName: string;
    lastName: string;
    email?: string;
    phone?: string;
    anamnesisStatus: 'NotStarted' | 'InProgress' | 'Completed';
    startedAt: string;
  }>;
  recentActivities: Array<{
    studentId: string;
    studentName: string;
    type: 'AnamnesisCompleted';
    occurredAt: string;
  }>;
};

export type TrainerStudent = TrainerDashboard['recentStudents'][number];
export type TrainerMessage = { id: string; studentId: string; message: string; startsAt: string; expiresAt?: string; createdAt: string };
export type TrainerAnamnesis = { goal: string; experienceLevel: string; trainingDaysPerWeek: number; sessionDurationMinutes: number; trainingLocation: string; equipmentNotes: string; heightCm: number; weightKg: number; healthConditions: string; movementRestrictions: string; currentPainDescription: string; nutritionPreferences: string; nutritionRestrictions: string; completedAt: string };
export type StudentInvite = { id: string; token: string; inviteCode: string; inviteUrl: string; email?: string; expiresAt: string; replacedPendingInvite: boolean };
export type ExerciseTrackingMode = 'Repetitions' | 'Duration';
export type WorkoutTemplate = { id: string; name: string; notes: string; exerciseCount?: number; updatedAt?: string; muscleGroups?: string[]; exercises?: WorkoutExercise[] };
export type WorkoutExercise = { exerciseId: string; name: string; primaryMuscleGroup: string; equipment?: string; imageRef: string; imageUrl?: string; instructions?: string; sequence: number; sets: number; repetitionsMin: number; repetitionsMax: number; restSeconds: number; notes?: string; trackingMode: ExerciseTrackingMode; targetDurationSeconds?: number };
export type WorkoutExerciseInput = Omit<WorkoutExercise, 'name'>;
export type TrainerStudentWorkoutSummary = { id: string; name: string; notes: string; suggestedOrder: number; exerciseCount: number; createdAt: string };
export type TrainerStudentWorkoutExercise = { id: string; exerciseId?: string; name: string; primaryMuscleGroup?: string; equipment?: string; imageRef?: string; imageUrl?: string; instructions?: string; sequence: number; sets: number; repetitionsMin: number; repetitionsMax: number; restSeconds: number; notes: string; trackingMode: ExerciseTrackingMode; targetDurationSeconds?: number };
export type TrainerStudentWorkout = Omit<TrainerStudentWorkoutSummary, 'exerciseCount'> & { studentId: string; exercises: TrainerStudentWorkoutExercise[] };
export type TrainerStudentWorkoutExerciseInput = { id?: string; exerciseId?: string; sequence: number; sets: number; repetitionsMin: number; repetitionsMax: number; restSeconds: number; notes?: string; trackingMode: ExerciseTrackingMode; targetDurationSeconds?: number };
export type TrainerExerciseCatalogItem = { id: string; name: string; slug: string; primaryMuscleGroup: string; equipment?: string; imageRef: string; imageUrl?: string; instructions?: string; isActive: boolean; defaultTrackingMode: ExerciseTrackingMode; defaultDurationSeconds?: number };
export type NutritionQuantityUnit = 'g' | 'ml' | 'unidade' | 'fatia' | 'colher' | 'dose' | 'porção';
export type TrainerNutritionFood = { id: string; foodName: string; quantity: number; unit: NutritionQuantityUnit; sequence: number };
export type TrainerNutritionMeal = { id: string; name: string; sequence: number; notes: string; foods: TrainerNutritionFood[] };
export type TrainerNutrition = { id: string; name: string; notes: string; updatedAt: string; responsibleTrainerName: string; meals: TrainerNutritionMeal[] };
export type TrainerNutritionInput = { name: string; notes?: string; meals: Array<{ name: string; sequence: number; notes?: string; foods: Array<{ foodName: string; quantity: number; unit: NutritionQuantityUnit; sequence: number }> }> };
export type NutritionTemplate = { id: string; name: string; notes: string; createdAt: string; updatedAt: string; mealCount?: number; foodCount?: number; meals?: TrainerNutritionMeal[] };
export type AppliedNutritionTemplate = { id: string; studentId: string; name: string; updatedAt: string; mealCount: number };
export type TrainerPrescriptionSettings = { sets: number; repetitionsMin: number; repetitionsMax: number; restSeconds: number; isCustomized: boolean };

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${apiUrl}${path}`, { ...options, headers: { Accept: 'application/json', ...(options.body ? { 'Content-Type': 'application/json' } : {}), Authorization: 'Bearer personal-ultra-demo-trainer', ...options.headers } });
  } catch {
    throw new ApiError(0, 'Sem conexão com a API do Trainer.');
  }
  const payload = await response.text();
  let body: unknown;
  try { body = payload ? JSON.parse(payload) : undefined; } catch { throw new ApiError(response.status, 'A API do Trainer retornou uma resposta inválida.'); }
  if (!response.ok) {
    const error = body as { code?: string; message?: string } | undefined;
    throw new ApiError(response.status, error?.message ?? 'Não foi possível carregar os dados do Trainer.', error?.code);
  }
  return body as T;
}

export const trainerClient = {
  demoIdentity: async () => {
    const response = await fetch(`${baseUrl}/api/v1/demo/identity`, { headers: { Authorization: 'Bearer personal-ultra-demo-trainer' } });
    if (!response.ok) throw new ApiError(response.status, 'Não foi possível carregar a identidade demo do Trainer.');
    return response.json() as Promise<{ actor: 'trainer'; id: string; name: string }>;
  },
  health: async () => {
    const response = await fetch(`${baseUrl}/health`);
    if (!response.ok) throw new ApiError(response.status, 'Não foi possível acessar a API do Trainer.');
    return response.json() as Promise<{ actor: 'trainer' }>;
  },
  dashboard: () => request<TrainerDashboard>('/dashboard'),
  students: async () => (await request<{ students: TrainerStudent[] }>('/students')).students,
  student: (studentId: string) => request<TrainerStudent>(`/students/${studentId}`),
  createMessage: (studentId: string, message: string) => request<TrainerMessage>(`/students/${studentId}/messages`, { method: 'POST', body: JSON.stringify({ message, startsAt: null, expiresAt: null }) }),
  anamnesis: (studentId: string) => request<TrainerAnamnesis>(`/students/${studentId}/anamnesis`),
  createStudentInvite: (email?: string) => request<StudentInvite>('/student-invites', { method: 'POST', body: JSON.stringify({ email: email?.trim() || null }) }),
  templates: () => request<WorkoutTemplate[]>('/training/templates/'),
  template: (id: string) => request<WorkoutTemplate>(`/training/templates/${id}`),
  createTemplate: (input: { name: string; notes?: string; exercises: Array<Pick<WorkoutExerciseInput, 'exerciseId' | 'sequence' | 'sets' | 'repetitionsMin' | 'repetitionsMax' | 'restSeconds' | 'notes' | 'trackingMode' | 'targetDurationSeconds'>> }) => request<WorkoutTemplate>('/training/templates/', { method: 'POST', body: JSON.stringify(input) }),
  updateTemplate: (id: string, input: { name: string; notes?: string; exercises: Array<Pick<WorkoutExerciseInput, 'exerciseId' | 'sequence' | 'sets' | 'repetitionsMin' | 'repetitionsMax' | 'restSeconds' | 'notes' | 'trackingMode' | 'targetDurationSeconds'>> }) => request<WorkoutTemplate>(`/training/templates/${id}`, { method: 'PUT', body: JSON.stringify(input) }),
  deleteTemplate: (id: string) => request<void>(`/training/templates/${id}`, { method: 'DELETE' }),
  duplicateTemplate: (id: string) => request<WorkoutTemplate>(`/training/templates/${id}/duplicate`, { method: 'POST' }),
  applyTemplate: (id: string, studentId: string) => request<{ id: string; studentId: string; name: string; suggestedOrder: number; exerciseCount: number }>(`/training/templates/${id}/apply`, { method: 'POST', body: JSON.stringify({ studentId }) }),
  studentWorkouts: async (studentId: string) => (await request<{ workouts: TrainerStudentWorkoutSummary[] }>(`/students/${studentId}/workouts`)).workouts,
  createStudentWorkout: (studentId: string, input: { name: string; notes?: string }) => request<TrainerStudentWorkout>(`/students/${studentId}/workouts`, { method: 'POST', body: JSON.stringify(input) }),
  studentWorkout: (studentId: string, workoutId: string) => request<TrainerStudentWorkout>(`/students/${studentId}/workouts/${workoutId}`),
  reorderStudentWorkouts: (studentId: string, workoutIds: string[]) => request<{ workouts: TrainerStudentWorkoutSummary[] }>(`/students/${studentId}/workouts/order`, { method: 'PUT', body: JSON.stringify({ workoutIds }) }),
  updateStudentWorkout: (studentId: string, workoutId: string, input: { name: string; exercises: TrainerStudentWorkoutExerciseInput[] }) => request<TrainerStudentWorkout>(`/students/${studentId}/workouts/${workoutId}`, { method: 'PUT', body: JSON.stringify(input) }),
  deleteStudentWorkout: (studentId: string, workoutId: string) => request<void>(`/students/${studentId}/workouts/${workoutId}`, { method: 'DELETE' }),
  exerciseCatalog: ({ search, muscleGroup }: { search?: string; muscleGroup?: string } = {}) => {
    const query = new URLSearchParams();
    const normalizedSearch = search?.trim();
    const normalizedMuscleGroup = muscleGroup?.trim();
    if (normalizedSearch) query.set('search', normalizedSearch);
    if (normalizedMuscleGroup) query.set('muscleGroup', normalizedMuscleGroup);
    const suffix = query.toString();
    return request<TrainerExerciseCatalogItem[]>(`/training/exercises/${suffix ? `?${suffix}` : ''}`);
  },
  trainingHistory: (studentId: string) => request<{ sessions: Array<{ sessionId: string; workoutName: string; status: string; startedAt: string; completedAt?: string; completedSets: number; exercises: Array<{ name: string; sequence: number; trackingMode: ExerciseTrackingMode; confirmedWithoutDetails: boolean; sets: Array<{ setNumber: number; weightKg?: number; repetitions?: number; durationSeconds?: number; completedAt: string }> }> }> }>(`/students/${studentId}/training-history`),
  nutrition: async (studentId: string) => (await request<TrainerNutrition | null>(`/students/${studentId}/nutrition`)) ?? null,
  saveNutrition: (studentId: string, input: TrainerNutritionInput) => request<TrainerNutrition>(`/students/${studentId}/nutrition`, { method: 'PUT', body: JSON.stringify(input) }),
  nutritionTemplates: () => request<NutritionTemplate[]>('/nutrition/templates'),
  nutritionTemplate: (id: string) => request<NutritionTemplate>(`/nutrition/templates/${id}`),
  createNutritionTemplate: (input: TrainerNutritionInput) => request<NutritionTemplate>('/nutrition/templates', { method: 'POST', body: JSON.stringify(input) }),
  updateNutritionTemplate: (id: string, input: TrainerNutritionInput) => request<NutritionTemplate>(`/nutrition/templates/${id}`, { method: 'PUT', body: JSON.stringify(input) }),
  deleteNutritionTemplate: (id: string) => request<void>(`/nutrition/templates/${id}`, { method: 'DELETE' }),
  duplicateNutritionTemplate: (id: string) => request<NutritionTemplate>(`/nutrition/templates/${id}/duplicate`, { method: 'POST' }),
  applyNutritionTemplate: (studentId: string, templateId: string, replaceExisting = false) => request<AppliedNutritionTemplate>(`/students/${studentId}/nutrition/from-template/${templateId}?replaceExisting=${replaceExisting}`, { method: 'POST' }),
  weight: (studentId: string) => request<Array<{ id: string; weightKg: number; recordedAt: string }>>(`/students/${studentId}/progress/weight`),
  prescriptionSettings: () => request<TrainerPrescriptionSettings>('/settings/prescription'),
  updatePrescriptionSettings: (input: Omit<TrainerPrescriptionSettings, 'isCustomized'>) => request<TrainerPrescriptionSettings>('/settings/prescription', { method: 'PUT', body: JSON.stringify(input) }),
};
