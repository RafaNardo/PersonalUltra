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
export type WorkoutTemplate = { id: string; name: string; notes: string; exerciseCount?: number; updatedAt?: string; exercises?: WorkoutExercise[] };
export type WorkoutExercise = { exerciseId: string; name: string; sequence: number; sets: number; repetitionsMin: number; repetitionsMax: number; restSeconds: number; notes?: string };
export type WorkoutExerciseInput = Omit<WorkoutExercise, 'name'>;
export type TrainerStudentWorkoutSummary = { id: string; name: string; notes: string; recommendedDay: number; isRecommended: boolean; exerciseCount: number; createdAt: string };
export type TrainerStudentWorkout = Omit<TrainerStudentWorkoutSummary, 'exerciseCount'> & { studentId: string; exercises: Array<{ id: string; exerciseId?: string; name: string; primaryMuscleGroup?: string; equipment?: string; imageRef?: string; instructions?: string; sequence: number; sets: number; repetitionsMin: number; repetitionsMax: number; restSeconds: number; notes: string }> };
export type TrainerExerciseCatalogItem = { id: string; name: string; slug: string; primaryMuscleGroup: string; equipment?: string; imageRef: string; instructions?: string; isActive: boolean };
export type TrainerNutrition = { id: string; name: string; notes: string; meals: Array<{ id: string; name: string; sequence: number; notes: string; foods: Array<{ foodName: string; quantityGrams: number }> }> };

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${apiUrl}${path}`, { ...options, headers: { Accept: 'application/json', ...(options.body ? { 'Content-Type': 'application/json' } : {}), Authorization: 'Bearer personal-ultra-demo-trainer', ...options.headers } });
  } catch {
    throw new ApiError(0, 'Sem conexão com a API do Trainer.');
  }
  if (!response.ok) throw new ApiError(response.status, 'Não foi possível carregar os dados do Trainer.');
  return response.json() as Promise<T>;
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
  createTemplate: (input: { name: string; notes?: string; exercises: WorkoutExerciseInput[] }) => request<WorkoutTemplate>('/training/templates/', { method: 'POST', body: JSON.stringify(input) }),
  duplicateTemplate: (id: string) => request<WorkoutTemplate>(`/training/templates/${id}/duplicate`, { method: 'POST' }),
  applyTemplate: (id: string, studentId: string, recommendedDay = 1, isRecommended = false) => request(`/training/templates/${id}/apply`, { method: 'POST', body: JSON.stringify({ studentId, recommendedDay, isRecommended }) }),
  studentWorkouts: async (studentId: string) => (await request<{ workouts: TrainerStudentWorkoutSummary[] }>(`/students/${studentId}/workouts`)).workouts,
  studentWorkout: (studentId: string, workoutId: string) => request<TrainerStudentWorkout>(`/students/${studentId}/workouts/${workoutId}`),
  exerciseCatalog: ({ search, muscleGroup }: { search?: string; muscleGroup?: string } = {}) => {
    const query = new URLSearchParams();
    const normalizedSearch = search?.trim();
    const normalizedMuscleGroup = muscleGroup?.trim();
    if (normalizedSearch) query.set('search', normalizedSearch);
    if (normalizedMuscleGroup) query.set('muscleGroup', normalizedMuscleGroup);
    const suffix = query.toString();
    return request<TrainerExerciseCatalogItem[]>(`/training/exercises/${suffix ? `?${suffix}` : ''}`);
  },
  trainingHistory: (studentId: string) => request<{ sessions: Array<{ sessionId: string; workoutName: string; status: string; startedAt: string; completedAt?: string; completedSets: number }> }>(`/students/${studentId}/training-history`),
  nutrition: (studentId: string) => request<TrainerNutrition | null>(`/students/${studentId}/nutrition`),
  saveNutrition: (studentId: string, input: { name: string; notes?: string; meals: Array<{ name: string; sequence: number; notes?: string; foods: Array<{ foodName: string; quantityGrams: number }> }> }) => request<TrainerNutrition>(`/students/${studentId}/nutrition`, { method: 'PUT', body: JSON.stringify(input) }),
  weight: (studentId: string) => request<Array<{ id: string; weightKg: number; recordedAt: string }>>(`/students/${studentId}/progress/weight`),
};
