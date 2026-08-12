import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { trainerClient } from '@/src/api/trainer-client';

export function useTrainerTemplates() { return useQuery({ queryKey: ['trainer', 'training', 'templates'], queryFn: trainerClient.templates }); }
export function useCreateTrainerTemplate() { const client = useQueryClient(); return useMutation({ mutationFn: trainerClient.createTemplate, onSuccess: () => client.invalidateQueries({ queryKey: ['trainer', 'training', 'templates'] }) }); }
export function useDuplicateTrainerTemplate() { const client = useQueryClient(); return useMutation({ mutationFn: trainerClient.duplicateTemplate, onSuccess: () => client.invalidateQueries({ queryKey: ['trainer', 'training', 'templates'] }) }); }
export function useApplyTrainerTemplate() { return useMutation({ mutationFn: ({ templateId, studentId }: { templateId: string; studentId: string }) => trainerClient.applyTemplate(templateId, studentId) }); }
