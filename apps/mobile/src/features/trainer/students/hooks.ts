import { useMutation, useQuery } from '@tanstack/react-query';
import { trainerClient } from '@/src/api/trainer-client';

export function useTrainerStudents() {
  return useQuery({ queryKey: ['trainer', 'students'], queryFn: trainerClient.students });
}

export function useTrainerStudent(studentId: string) {
  return useQuery({ queryKey: ['trainer', 'students', studentId], queryFn: () => trainerClient.student(studentId), enabled: Boolean(studentId) });
}

export function useCreateTrainerMessage(studentId: string) {
  return useMutation({ mutationFn: (message: string) => trainerClient.createMessage(studentId, message) });
}

export function useTrainerAnamnesis(studentId: string, enabled: boolean) {
  return useQuery({ queryKey: ['trainer', 'students', studentId, 'anamnesis'], queryFn: () => trainerClient.anamnesis(studentId), enabled });
}

export function useCreateStudentInvite() {
  return useMutation({ mutationFn: (email?: string) => trainerClient.createStudentInvite(email) });
}
