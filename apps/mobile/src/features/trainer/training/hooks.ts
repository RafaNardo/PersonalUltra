import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { trainerClient } from '@/src/api/trainer-client';

export function useTrainerTemplates() { return useQuery({ queryKey: ['trainer', 'training', 'templates'], queryFn: trainerClient.templates }); }
export function useCreateTrainerTemplate() { const client = useQueryClient(); return useMutation({ mutationFn: trainerClient.createTemplate, onSuccess: () => client.invalidateQueries({ queryKey: ['trainer', 'training', 'templates'] }) }); }
export function useDuplicateTrainerTemplate() { const client = useQueryClient(); return useMutation({ mutationFn: trainerClient.duplicateTemplate, onSuccess: () => client.invalidateQueries({ queryKey: ['trainer', 'training', 'templates'] }) }); }
export function useApplyTrainerTemplate() { return useMutation({ mutationFn: ({ templateId, studentId }: { templateId: string; studentId: string }) => trainerClient.applyTemplate(templateId, studentId) }); }
export function useTrainerStudentWorkouts(studentId: string) { return useQuery({ queryKey: ['trainer', 'students', studentId, 'workouts'], queryFn: () => trainerClient.studentWorkouts(studentId), enabled: Boolean(studentId) }); }
export function useTrainerStudentWorkout(studentId: string, workoutId: string) { return useQuery({ queryKey: ['trainer', 'students', studentId, 'workouts', workoutId], queryFn: () => trainerClient.studentWorkout(studentId, workoutId), enabled: Boolean(studentId && workoutId) }); }
export function useUpdateTrainerStudentWorkout(studentId: string, workoutId: string) {
  const client = useQueryClient();
  return useMutation({
    mutationFn: (exercises: Parameters<typeof trainerClient.updateStudentWorkout>[2]) => trainerClient.updateStudentWorkout(studentId, workoutId, exercises),
    onSuccess: (workout) => {
      client.setQueryData(['trainer', 'students', studentId, 'workouts', workoutId], workout);
      void client.invalidateQueries({ queryKey: ['trainer', 'students', studentId, 'workouts'], exact: true });
    },
  });
}
export function useTrainerExerciseCatalog(search = '', muscleGroup?: string, enabled = true) {
  const debouncedSearch = useDebouncedValue(search.trim(), 300);
  return useQuery({
    queryKey: ['trainer', 'training', 'exercise-catalog', debouncedSearch, muscleGroup ?? 'all'],
    queryFn: () => trainerClient.exerciseCatalog({ search: debouncedSearch, muscleGroup }),
    placeholderData: (previous) => previous,
    enabled,
  });
}

function useDebouncedValue<T>(value: T, delayMs: number) {
  const [debouncedValue, setDebouncedValue] = useState(value);
  useEffect(() => {
    const timeout = setTimeout(() => setDebouncedValue(value), delayMs);
    return () => clearTimeout(timeout);
  }, [delayMs, value]);
  return debouncedValue;
}
