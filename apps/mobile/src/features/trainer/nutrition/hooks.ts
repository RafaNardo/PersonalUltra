import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { trainerClient, type NutritionMealTemplateInput, type TrainerNutritionInput } from '@/src/api/trainer-client';

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

const templateKeys = ['trainer', 'nutrition', 'templates'] as const;
export function useNutritionTemplates() { return useQuery({ queryKey: templateKeys, queryFn: trainerClient.nutritionTemplates }); }
export function useNutritionTemplate(id: string, enabled = true) { return useQuery({ queryKey: [...templateKeys, id], queryFn: () => trainerClient.nutritionTemplate(id), enabled: Boolean(id) && enabled }); }
export function useCreateNutritionTemplate() { const client = useQueryClient(); return useMutation({ mutationFn: trainerClient.createNutritionTemplate, onSuccess: () => client.invalidateQueries({ queryKey: templateKeys }) }); }
export function useUpdateNutritionTemplate(id: string) { const client = useQueryClient(); return useMutation({ mutationFn: (input: NutritionMealTemplateInput) => trainerClient.updateNutritionTemplate(id, input), onSuccess: (value) => { client.setQueryData([...templateKeys, id], value); void client.invalidateQueries({ queryKey: templateKeys, exact: true }); } }); }
export function useDeleteNutritionTemplate() { const client = useQueryClient(); return useMutation({ mutationFn: trainerClient.deleteNutritionTemplate, onSuccess: (_, id) => { client.removeQueries({ queryKey: [...templateKeys, id] }); void client.invalidateQueries({ queryKey: templateKeys, exact: true }); } }); }
export function useDuplicateNutritionTemplate() { const client = useQueryClient(); return useMutation({ mutationFn: trainerClient.duplicateNutritionTemplate, onSuccess: () => client.invalidateQueries({ queryKey: templateKeys }) }); }
export function useApplyNutritionTemplate(studentId: string) { const client = useQueryClient(); return useMutation({ mutationFn: (templateId: string) => trainerClient.applyNutritionTemplate(studentId, templateId), onSuccess: () => { void client.invalidateQueries({ queryKey: trainerNutritionQueryKey(studentId), exact: true }); } }); }
