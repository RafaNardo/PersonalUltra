import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { trainerClient, type TrainerNutritionInput } from '@/src/api/trainer-client';

export const trainerNutritionQueryKey = (studentId: string) => ['trainer', 'students', studentId, 'nutrition'] as const;

export function useTrainerNutrition(studentId: string) {
  return useQuery({
    queryKey: trainerNutritionQueryKey(studentId),
    queryFn: () => trainerClient.nutrition(studentId),
    enabled: Boolean(studentId),
  });
}

export function useSaveTrainerNutrition(studentId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: TrainerNutritionInput) => trainerClient.saveNutrition(studentId, input),
    onSuccess: (nutrition) => {
      queryClient.setQueryData(trainerNutritionQueryKey(studentId), nutrition);
      void queryClient.invalidateQueries({ queryKey: trainerNutritionQueryKey(studentId), exact: true });
    },
  });
}
