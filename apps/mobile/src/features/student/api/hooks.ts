import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, ApiError } from './client';
import type { CompleteSetInput, SaveOnboardingProfile, TrainingToday } from './types';
import { cachedWorkout, cacheWorkout, clearTrainingData, pendingSets, queueSet, removePendingSet } from '../offline/training-db';
import { useAuthStore } from '../state/auth-store';

const keys = { bootstrap: ['bootstrap'] as const, onboarding: ['onboarding'] as const, home: ['home'] as const, today: ['training', 'today'] as const, trainingPlan: ['training', 'plan'] as const, progress: ['progress'] as const, weights: ['weights'] as const, nutrition: ['nutrition', 'today'] as const, coach: ['coach'] as const };

function token() {
  const accessToken = useAuthStore.getState().accessToken;
  if (!accessToken) throw new Error('Sessão não autenticada.');
  return accessToken;
}

export function useBootstrap() { return useQuery({ queryKey: keys.bootstrap, queryFn: () => api.bootstrap(token()), enabled: Boolean(useAuthStore((state) => state.accessToken)) }); }
export function useOnboardingProfile() { return useQuery({ queryKey: keys.onboarding, queryFn: () => api.onboardingProfile(token()), enabled: Boolean(useAuthStore((state) => state.accessToken)) }); }
export function useSaveOnboardingProfile() { const client = useQueryClient(); return useMutation({ mutationFn: (input: SaveOnboardingProfile) => api.saveOnboardingProfile(token(), input), onSuccess: (data) => client.setQueryData(keys.onboarding, data) }); }
export function useCompleteOnboarding() { const client = useQueryClient(); return useMutation({ mutationFn: () => api.completeOnboarding(token()), onSuccess: (data) => { client.setQueryData(keys.onboarding, data); client.invalidateQueries({ queryKey: keys.bootstrap }); } }); }
export function useHome() { return useQuery({ queryKey: keys.home, queryFn: () => api.home(token()), enabled: Boolean(useAuthStore((state) => state.accessToken)) }); }
export function useTrainingToday() {
  return useQuery({ queryKey: keys.today, queryFn: async () => {
    try {
      const workout = await api.today(token());
      await cacheWorkout(workout);
      return workout;
    } catch (error) {
      if (error instanceof ApiError && error.status === 0) {
        const cached = await cachedWorkout<TrainingToday>();
        if (cached) return cached;
      }
      throw error;
    }
  }, enabled: Boolean(useAuthStore((state) => state.accessToken)) });
}
export function useTrainingPlan() { return useQuery({ queryKey: keys.trainingPlan, queryFn: () => api.trainingPlan(token()), enabled: Boolean(useAuthStore((state) => state.accessToken)) }); }

export function useDevLogin() { return useMutation({ mutationFn: api.devLogin }); }
export function useProgress() { return useQuery({ queryKey: keys.progress, queryFn: () => api.progress(token()), enabled: Boolean(useAuthStore((state) => state.accessToken)) }); }
export function useWeights() { return useQuery({ queryKey: keys.weights, queryFn: () => api.weights(token()), enabled: Boolean(useAuthStore((state) => state.accessToken)) }); }
export function useAddWeight() { const client = useQueryClient(); return useMutation({ mutationFn: (weightKg: number) => api.addWeight(token(), weightKg), onSuccess: () => { client.invalidateQueries({ queryKey: keys.weights }); client.invalidateQueries({ queryKey: keys.progress }); } }); }
export function useResetDemo() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: () => api.resetDemo(token()),
    onSuccess: async () => { await clearTrainingData(); await client.invalidateQueries(); },
  });
}
export function useResetCurrentMemberDemo() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: () => api.resetCurrentMemberDemo(token()),
    onSuccess: async () => {
      await clearTrainingData();
      client.clear();
    },
  });
}
export function useNutritionToday() { return useQuery({ queryKey: keys.nutrition, queryFn: () => api.nutritionToday(token()), enabled: Boolean(useAuthStore((state) => state.accessToken)) }); }
export function useCoachConversation() { return useQuery({ queryKey: keys.coach, queryFn: () => api.coachConversation(token()), enabled: Boolean(useAuthStore((state) => state.accessToken)) }); }
export function useSendCoachMessage() { const client = useQueryClient(); return useMutation({ mutationFn: (content: string) => api.sendCoachMessage(token(), content), onSuccess: (data) => client.setQueryData(keys.coach, data) }); }
export function useStartWorkout() {
  const client = useQueryClient();
  return useMutation({ mutationFn: (sessionId: string) => api.startWorkout(token(), sessionId), onSuccess: () => client.invalidateQueries({ queryKey: keys.today }) });
}

export function useCompleteSet() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: async ({ sessionId, exerciseId, input }: { sessionId: string; exerciseId: string; input: CompleteSetInput }) => {
      try {
        return { queued: false as const, result: await api.completeSet(token(), sessionId, exerciseId, input) };
      } catch (error) {
        if (!(error instanceof ApiError) || error.status !== 0) throw error;
        await queueSet({ token: token(), sessionId, exerciseId, input });
        return { queued: true as const, result: undefined };
      }
    },
    onSuccess: () => client.invalidateQueries({ queryKey: keys.today }),
  });
}

export function useCompleteWorkout() {
  const client = useQueryClient();
  return useMutation({ mutationFn: (sessionId: string) => api.completeWorkout(token(), sessionId), onSuccess: () => { client.invalidateQueries({ queryKey: keys.today }); client.invalidateQueries({ queryKey: keys.home }); } });
}

export async function syncPendingSetOperations() {
  for (const pending of await pendingSets()) {
    try {
      await api.completeSet(pending.token, pending.sessionId, pending.exerciseId, pending.input);
      await removePendingSet(pending.input.clientOperationId);
    } catch (error) {
      if (!(error instanceof ApiError) || error.status !== 0) await removePendingSet(pending.input.clientOperationId);
      break;
    }
  }
}

export function findExercise(workout: TrainingToday | undefined, id: string) { return workout?.exercises.find((exercise) => exercise.id === id); }
