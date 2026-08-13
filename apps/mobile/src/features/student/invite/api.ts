import { ApiError } from '@/src/api/shared-http';

const baseUrl = (process.env.EXPO_PUBLIC_API_URL ?? 'https://student-api-production-a4fe.up.railway.app').replace(/\/$/, '');
const apiUrl = `${baseUrl}/api/v1`;

export type Invite = { trainerName: string; email?: string; expiresAt: string };
export type InviteSession = { accessToken: string; studentId: string; firstName: string; lastName: string; email: string; phone: string; trainerId: string };
export type AnamnesisAnswers = { goal: string; experienceLevel: string; trainingDaysPerWeek: number; sessionDurationMinutes: number; trainingLocation: string; equipmentNotes: string; heightCm: number; weightKg: number; healthConditions: string; movementRestrictions: string; currentPainDescription: string; nutritionPreferences: string; nutritionRestrictions: string };
export type ActiveTrainerMessage = { id: string; message: string; startsAt: string; expiresAt?: string };
export type StudentWorkout = { id: string; name: string; notes: string; recommendedDay: number; isRecommended: boolean; exerciseCount: number; prescribedSets: number; state: 'Recommended' | 'Available' | 'InProgress' | 'Completed'; activeSessionId?: string; lastCompletedAt?: string };
export type StudentTraining = { recommended?: StudentWorkout; available: StudentWorkout[]; history: Array<{ sessionId: string; workoutId: string; workoutName: string; status: string; startedAt: string; completedAt?: string; completedSets: number }> };
export type StudentWorkoutPreview = { id: string; name: string; notes: string; recommendedDay: number; isRecommended: boolean; state: StudentWorkout['state']; activeSessionId?: string; lastCompletedAt?: string; exercises: Array<{ id: string; exerciseId?: string; name: string; primaryMuscleGroup?: string; equipment?: string; imageRef?: string; instructions?: string; sequence: number; sets: number; repetitionsMin: number; repetitionsMax: number; restSeconds: number; notes: string }> };
export type StudentSessionExercise = { id: string; exerciseId?: string; name: string; primaryMuscleGroup?: string; equipment?: string; imageRef?: string; instructions?: string; sequence: number; sets: number; repetitionsMin: number; repetitionsMax: number; restSeconds: number; notes: string; completedSets: number };
export type StudentSession = { sessionId: string; workoutId: string; workoutName: string; status: string; startedAt: string; completedAt?: string; exercises: StudentSessionExercise[] };
export type StudentSessionDetail = StudentSession & { exercises: Array<StudentSessionExercise & { performances: Array<{ setNumber: number; weightKg: number; repetitions: number; completedAt: string }> }> };
export type StudentNutrition = { id: string; name: string; notes: string; meals: Array<{ id: string; name: string; sequence: number; notes: string; foods: Array<{ foodName: string; quantityGrams: number }> }> };
export type StudentWeight = { id: string; weightKg: number; recordedAt: string };
export type CoachAnswer = { answer: string; sources: string[] };
export type StudentBranding = { displayName: string; primaryColor: string; logoUrl?: string };

async function request<T>(path: string, options: RequestInit = {}, token?: string): Promise<T> {
  let response: Response;
  try { response = await fetch(`${apiUrl}${path}`, { ...options, headers: { Accept: 'application/json', ...(options.body ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...options.headers } }); }
  catch { throw new ApiError(0, 'Sem conexão com o servidor.'); }
  const payload = await response.text();
  let body: unknown;
  try { body = payload ? JSON.parse(payload) : undefined; }
  catch { throw new ApiError(response.status, 'A API retornou uma resposta inválida. Tente novamente.'); }
  if (!response.ok) {
    const error = body as { message?: string } | undefined;
    throw new ApiError(response.status, error?.message ?? 'Não foi possível concluir a solicitação.');
  }
  return body as T;
}

export const inviteApi = {
  resolve: (token: string) => request<Invite>(`/invite/${token}`),
  resolveCode: (code: string) => request<Invite>(`/invite/code/${code.replace(/\D/g, '')}`),
  accept: (token: string, input: { firstName: string; lastName: string; email?: string; phone: string }) => request<InviteSession>(`/invite/${token}/accept`, { method: 'POST', body: JSON.stringify(input) }),
  acceptCode: (code: string, input: { firstName: string; lastName: string; email?: string; phone: string }) => request<InviteSession>(`/invite/code/${code.replace(/\D/g, '')}/accept`, { method: 'POST', body: JSON.stringify(input) }),
  studentLogin: (email: string) => request<InviteSession>('/auth/student-login', { method: 'POST', body: JSON.stringify({ email }) }),
  anamnesis: (token: string) => request<AnamnesisAnswers & { isCompleted: boolean }>('/anamnesis', {}, token),
  saveAnamnesis: (token: string, answers: AnamnesisAnswers) => request<AnamnesisAnswers & { isCompleted: boolean }>('/anamnesis', { method: 'PUT', body: JSON.stringify(answers) }, token),
  completeAnamnesis: (token: string) => request<AnamnesisAnswers & { isCompleted: boolean }>('/anamnesis/complete', { method: 'POST' }, token),
  activeTrainerMessage: async (token: string) => (await request<ActiveTrainerMessage | null>('/home/trainer-message', {}, token)) ?? null,
  training: (token: string) => request<StudentTraining>('/training', {}, token),
  trainingPreview: (token: string, workoutId: string) => request<StudentWorkoutPreview>(`/training/${workoutId}`, {}, token),
  startWorkout: (token: string, workoutId: string) => request<StudentSession>(`/training/${workoutId}/start`, { method: 'POST' }, token),
  session: (token: string, sessionId: string) => request<StudentSessionDetail>(`/training/sessions/${sessionId}`, {}, token),
  activeSession: async (token: string, workoutId: string) => {
    const training = await request<StudentTraining>('/training', {}, token);
    const workout = [training.recommended, ...training.available].find((item) => item?.id === workoutId);
    return workout?.activeSessionId ? request<StudentSessionDetail>(`/training/sessions/${workout.activeSessionId}`, {}, token) : undefined;
  },
  completeSet: (token: string, sessionId: string, exerciseId: string, input: { clientOperationId: string; setNumber: number; weightKg: number; repetitions: number }) => request<{ saved: boolean; completedSets: number }>(`/training/sessions/${sessionId}/exercises/${exerciseId}/sets`, { method: 'POST', body: JSON.stringify(input) }, token),
  completeWorkout: (token: string, sessionId: string) => request(`/training/sessions/${sessionId}/complete`, { method: 'POST' }, token),
  nutrition: (token: string) => request<StudentNutrition | null>('/nutrition', {}, token),
  weight: (token: string) => request<StudentWeight[]>('/progress/weight', {}, token),
  addWeight: (token: string, weightKg: number) => request<StudentWeight>('/progress/weight', { method: 'POST', body: JSON.stringify({ weightKg }) }, token),
  coachAnswer: (token: string, question: string) => request<CoachAnswer>(`/coach/answer?question=${encodeURIComponent(question)}`, {}, token),
  branding: (token: string) => request<StudentBranding | null>('/branding', {}, token),
};
