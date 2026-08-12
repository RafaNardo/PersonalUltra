import { useQuery } from '@tanstack/react-query';
import { trainerClient } from '@/src/api/trainer-client';

export function useTrainerStudents() {
  return useQuery({ queryKey: ['trainer', 'students'], queryFn: trainerClient.students });
}

export function useTrainerStudent(studentId: string) {
  return useQuery({ queryKey: ['trainer', 'students', studentId], queryFn: () => trainerClient.student(studentId), enabled: Boolean(studentId) });
}
