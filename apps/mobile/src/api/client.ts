import type { CoachAction, CoachConversation, CompleteSet, CompleteSetInput, CompleteWorkout, DevLogin, ExerciseAlternative, FoodAlternative, Home, InitialPlan, Meal, NutritionToday, OnboardingProfile, PainReport, ProgressSummary, ResolveCoachAction, SaveOnboardingProfile, TrainingPlan, TrainingToday, WeightEntry } from '@/src/api/types';

const baseUrl = (process.env.EXPO_PUBLIC_API_URL ?? 'https://svr-method-production.up.railway.app').replace(/\/$/, '');
const apiUrl = `${baseUrl}/api/v1`;

export class ApiError extends Error {
  constructor(public readonly status: number, message: string, public readonly code?: string) { super(message); }
}

async function request<T>(path: string, options: RequestInit = {}, token?: string): Promise<T> {
  let response: Response;
  try {
    response = await fetch(`${apiUrl}${path}`, { ...options, headers: { Accept: 'application/json', ...(options.body ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...options.headers } });
  } catch {
    throw new ApiError(0, 'Sem conexão com o servidor.');
  }

  if (!response.ok) {
    const error = await response.json().catch(() => null) as { code?: string; message?: string } | null;
    throw new ApiError(response.status, error?.message ?? 'Não foi possível concluir a solicitação.', error?.code);
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export const api = {
  devLogin: (email: string) => request<DevLogin>('/auth/dev-login', { method: 'POST', body: JSON.stringify({ email }) }),
  bootstrap: (token: string) => request<{ member: DevLogin['member']; activePlan?: Home['activePlan']; nextRoute: string }>('/bootstrap', {}, token),
  onboardingProfile: (token: string) => request<OnboardingProfile>('/onboarding/profile', {}, token),
  saveOnboardingProfile: (token: string, input: SaveOnboardingProfile) => request<OnboardingProfile>('/onboarding/profile', { method: 'PUT', body: JSON.stringify(input) }, token),
  completeOnboarding: (token: string) => request<OnboardingProfile>('/onboarding/complete', { method: 'POST' }, token),
  initialPlan: (token: string) => request<InitialPlan>('/plans/initial', {}, token),
  provisionInitialPlan: (token: string) => request<InitialPlan>('/plans/initial', { method: 'POST' }, token),
  home: (token: string) => request<Home>('/home', {}, token),
  today: (token: string) => request<TrainingToday>('/training/today', {}, token),
  trainingPlan: (token: string) => request<TrainingPlan>('/training/plan', {}, token),
  startWorkout: (token: string, sessionId: string) => request<{ id: string; status: string; startedAt: string; wasAlreadyStarted: boolean }>(`/training/sessions/${sessionId}/start`, { method: 'POST' }, token),
  completeSet: (token: string, sessionId: string, sessionExerciseId: string, input: CompleteSetInput) => request<CompleteSet>(`/training/sessions/${sessionId}/exercises/${sessionExerciseId}/sets`, { method: 'POST', body: JSON.stringify(input) }, token),
  completeWorkout: (token: string, sessionId: string) => request<CompleteWorkout>(`/training/sessions/${sessionId}/complete`, { method: 'POST' }, token),
  progress: (token: string) => request<ProgressSummary>('/progress/summary', {}, token),
  weights: (token: string) => request<WeightEntry[]>('/progress/weight', {}, token),
  addWeight: (token: string, weightKg: number) => request<WeightEntry>('/progress/weight', { method: 'POST', body: JSON.stringify({ weightKg }) }, token),
  resetDemo: (token: string) => request<void>('/demo/reset', { method: 'POST' }, token),
  resetCurrentMemberDemo: (token: string) => request<void>('/demo/member-reset', { method: 'POST' }, token),
  nutritionToday: (token: string) => request<NutritionToday>('/nutrition/today', {}, token),
  meal: (token: string, mealId: string) => request<Meal>(`/nutrition/meals/${mealId}`, {}, token),
  completeMeal: (token: string, mealId: string) => request<void>(`/nutrition/meals/${mealId}/complete`, { method: 'POST' }, token),
  foodAlternatives: (token: string, mealId: string, foodId: string) => request<FoodAlternative[]>(`/nutrition/meals/${mealId}/foods/${foodId}/alternatives`, {}, token),
  substituteFood: (token: string, mealId: string, foodId: string, replacementId: string) => request<void>(`/nutrition/meals/${mealId}/foods/${foodId}/substitute`, { method: 'POST', body: JSON.stringify({ foodId: replacementId }) }, token),
  coachConversation: (token: string) => request<CoachConversation>('/coach/conversation', {}, token),
  sendCoachMessage: (token: string, content: string) => request<CoachConversation>('/coach/messages', { method: 'POST', body: JSON.stringify({ content }) }, token),
  reportPain: (token: string, area: string, side: string, intensity: number, context: string) => request<PainReport>('/health/pain-reports', { method: 'POST', body: JSON.stringify({ area, side, intensity, context }) }, token),
  exerciseAlternatives: (token: string, sessionId: string, exerciseId: string) => request<ExerciseAlternative[]>(`/training/sessions/${sessionId}/exercises/${exerciseId}/alternatives`, {}, token),
  proposeExerciseSubstitution: (token: string, sessionId: string, exerciseId: string, replacementId: string) => request<CoachAction>(`/training/sessions/${sessionId}/exercises/${exerciseId}/substitution-proposals`, { method: 'POST', body: JSON.stringify({ exerciseId: replacementId }) }, token),
  coachActions: (token: string) => request<CoachAction[]>('/coach/actions', {}, token),
  confirmCoachAction: (token: string, actionId: string) => request<ResolveCoachAction>(`/coach/actions/${actionId}/confirm`, { method: 'POST' }, token),
  rejectCoachAction: (token: string, actionId: string) => request<ResolveCoachAction>(`/coach/actions/${actionId}/reject`, { method: 'POST' }, token),
};
