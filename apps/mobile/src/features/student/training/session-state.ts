import { create } from 'zustand';
import type { StudentSession } from '@/src/features/student/invite/api';

export type SessionExercise = StudentSession['exercises'][number];
export type ExerciseProgressState = 'completed' | 'current' | 'pending';

type StudentTrainingSessionState = {
  session?: StudentSession;
  isOfflineSnapshot: boolean;
  setSession: (session: StudentSession, isOfflineSnapshot?: boolean) => void;
  setOfflineSnapshot: (isOfflineSnapshot: boolean) => void;
  updateExerciseProgress: (exerciseId: string, completedSets: number) => void;
  clearSession: () => void;
};

export const useStudentTrainingSessionStore = create<StudentTrainingSessionState>((set) => ({
  session: undefined,
  isOfflineSnapshot: false,
  setSession: (session, isOfflineSnapshot = false) => set({ session, isOfflineSnapshot }),
  setOfflineSnapshot: (isOfflineSnapshot) => set({ isOfflineSnapshot }),
  updateExerciseProgress: (exerciseId, completedSets) => set((state) => {
    if (!state.session) return state;
    return {
      ...state,
      session: {
        ...state.session,
        exercises: state.session.exercises.map((exercise) => exercise.id === exerciseId
          ? { ...exercise, completedSets: Math.max(exercise.completedSets, completedSets) }
          : exercise),
      },
    };
  }),
  clearSession: () => set({ session: undefined, isOfflineSnapshot: false }),
}));

/** API sequence is persisted data; this helper only provides a stable view for selection. */
export function orderedExercises(session: StudentSession): SessionExercise[] {
  return [...session.exercises].sort((left, right) => left.sequence - right.sequence);
}

export function currentExercise(session: StudentSession): SessionExercise | undefined {
  return orderedExercises(session).find((exercise) => exercise.completedSets < exercise.sets);
}

export function exerciseProgressState(session: StudentSession, exercise: SessionExercise): ExerciseProgressState {
  if (exercise.completedSets >= exercise.sets) return 'completed';
  return currentExercise(session)?.id === exercise.id ? 'current' : 'pending';
}

export function sessionProgress(session: StudentSession) {
  const totalSets = session.exercises.reduce((total, exercise) => total + exercise.sets, 0);
  const completedSets = session.exercises.reduce((total, exercise) => total + Math.min(exercise.completedSets, exercise.sets), 0);
  return { completedSets, totalSets, percentage: totalSets === 0 ? 0 : Math.round((completedSets / totalSets) * 100) };
}
