import { ApiError } from '@/src/api/shared-http';

const baseUrl = (process.env.EXPO_PUBLIC_API_URL ?? 'https://student-api-production-a4fe.up.railway.app').replace(/\/$/, '');
const apiUrl = `${baseUrl}/api/v1`;

export type Invite = { trainerName: string; email?: string; expiresAt: string };
export type InviteSession = { accessToken: string; studentId: string; firstName: string; lastName: string; email: string; phone: string; trainerId: string };
export type AnamnesisAnswers = { goal: string; experienceLevel: string; trainingDaysPerWeek: number; sessionDurationMinutes: number; trainingLocation: string; equipmentNotes: string; heightCm: number; weightKg: number; healthConditions: string; movementRestrictions: string; currentPainDescription: string; nutritionPreferences: string; nutritionRestrictions: string };
export type ActiveTrainerMessage = { id: string; message: string; startsAt: string; expiresAt?: string };
export type StudentWorkout = { id: string; name: string; notes: string; recommendedDay: number; isRecommended: boolean; exerciseCount: number };
export type StudentTraining = { recommended?: StudentWorkout; available: StudentWorkout[]; history: Array<{ sessionId: string; workoutId: string; workoutName: string; status: string; startedAt: string; completedAt?: string; completedSets: number }> };
export type StudentSession = { sessionId: string; workoutId: string; workoutName: string; status: string; exercises: Array<{ id: string; name: string; sequence: number; sets: number; repetitions: number; completedSets: number }> };

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
  startWorkout: (token: string, workoutId: string) => request<StudentSession>(`/training/${workoutId}/start`, { method: 'POST' }, token),
  completeSet: (token: string, sessionId: string, exerciseId: string, input: { setNumber: number; weightKg: number; repetitions: number }) => request<{ saved: boolean }>(`/training/sessions/${sessionId}/exercises/${exerciseId}/sets`, { method: 'POST', body: JSON.stringify(input) }, token),
  completeWorkout: (token: string, sessionId: string) => request(`/training/sessions/${sessionId}/complete`, { method: 'POST' }, token),
};
