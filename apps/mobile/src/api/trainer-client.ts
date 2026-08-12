import { ApiError } from './student-client';

const baseUrl = (process.env.EXPO_PUBLIC_TRAINER_API_URL ?? 'http://localhost:8081').replace(/\/$/, '');

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
};
