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
    anamnesisStatus: 'NotStarted' | 'InProgress' | 'Completed';
    startedAt: string;
  }>;
};

export type TrainerStudent = TrainerDashboard['recentStudents'][number];
export type TrainerMessage = { id: string; studentId: string; message: string; startsAt: string; expiresAt?: string; createdAt: string };
export type TrainerAnamnesis = { goal: string; experienceLevel: string; trainingDaysPerWeek: number; sessionDurationMinutes: number; trainingLocation: string; equipmentNotes: string; heightCm: number; weightKg: number; healthConditions: string; movementRestrictions: string; currentPainDescription: string; nutritionPreferences: string; nutritionRestrictions: string; completedAt: string };

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
};
