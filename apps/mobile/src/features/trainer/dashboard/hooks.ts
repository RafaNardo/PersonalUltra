import { useQuery } from '@tanstack/react-query';
import { trainerClient } from '@/src/api/trainer-client';

export function useTrainerDashboard() {
  return useQuery({ queryKey: ['trainer', 'dashboard'], queryFn: trainerClient.dashboard });
}
