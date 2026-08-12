import { useQuery } from '@tanstack/react-query';
import { trainerClient } from '@/src/api/trainer-client';

export function useTrainerStudents() {
  return useQuery({ queryKey: ['trainer', 'students'], queryFn: trainerClient.students });
}
