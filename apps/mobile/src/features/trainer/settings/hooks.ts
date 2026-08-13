import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { trainerClient, type TrainerPrescriptionSettings } from '@/src/api/trainer-client';

const queryKey = ['trainer', 'settings', 'prescription'] as const;

export function useTrainerPrescriptionSettings(enabled = true) {
  return useQuery({ queryKey, queryFn: trainerClient.prescriptionSettings, enabled });
}

export function useUpdateTrainerPrescriptionSettings() {
  const client = useQueryClient();
  return useMutation({
    mutationFn: (input: Omit<TrainerPrescriptionSettings, 'isCustomized'>) => trainerClient.updatePrescriptionSettings(input),
    onSuccess: (settings) => client.setQueryData(queryKey, settings),
  });
}
