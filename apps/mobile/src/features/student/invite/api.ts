import { ApiError } from '@/src/api/shared-http';

const baseUrl = (process.env.EXPO_PUBLIC_API_URL ?? 'https://student-api-production-a4fe.up.railway.app').replace(/\/$/, '');
const apiUrl = `${baseUrl}/api/v1`;

export type Invite = { trainerName: string; email?: string; expiresAt: string };
export type InviteSession = { accessToken: string; studentId: string; firstName: string; lastName: string; email: string; phone: string; trainerId: string };
export type AnamnesisAnswers = { goal: string; experienceLevel: string; trainingDaysPerWeek: number; sessionDurationMinutes: number; trainingLocation: string; equipmentNotes: string; heightCm: number; weightKg: number; healthConditions: string; movementRestrictions: string; currentPainDescription: string; nutritionPreferences: string; nutritionRestrictions: string };
export type ActiveTrainerMessage = { id: string; message: string; startsAt: string; expiresAt?: string };

async function request<T>(path: string, options: RequestInit = {}, token?: string): Promise<T> {
  let response: Response;
  try { response = await fetch(`${apiUrl}${path}`, { ...options, headers: { Accept: 'application/json', ...(options.body ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...options.headers } }); }
  catch { throw new ApiError(0, 'Sem conexão com o servidor.'); }
  if (!response.ok) {
    const error = await response.json().catch(() => null) as { message?: string } | null;
    throw new ApiError(response.status, error?.message ?? 'Não foi possível concluir a solicitação.');
  }
  return response.json() as Promise<T>;
}

export const inviteApi = {
  resolve: (token: string) => request<Invite>(`/invite/${token}`),
  accept: (token: string, input: { firstName: string; lastName: string; email?: string; phone: string }) => request<InviteSession>(`/invite/${token}/accept`, { method: 'POST', body: JSON.stringify(input) }),
  saveAnamnesis: (token: string, answers: AnamnesisAnswers) => request<AnamnesisAnswers & { isCompleted: boolean }>('/anamnesis', { method: 'PUT', body: JSON.stringify(answers) }, token),
  completeAnamnesis: (token: string) => request<AnamnesisAnswers & { isCompleted: boolean }>('/anamnesis/complete', { method: 'POST' }, token),
  activeTrainerMessage: (token: string) => request<ActiveTrainerMessage | null>('/home/trainer-message', {}, token),
};
