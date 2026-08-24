import { create } from 'zustand';
import type { StudentSession, StudentSetPerformance } from '@/src/features/student/invite/api';
import type { PendingSetDetail } from '@/src/features/student/offline/training-db';

export type SessionExercise = StudentSession['exercises'][number];
export type ExerciseProgressState = 'completed' | 'current' | 'pending';

type StudentTrainingSessionState = {
  session?: StudentSession;
  studentId?: string;
  isOfflineSnapshot: boolean;
  setSession: (session: StudentSession, isOfflineSnapshot?: boolean, studentId?: string) => void;
  setOfflineSnapshot: (isOfflineSnapshot: boolean) => void;
  updateExerciseProgress: (exerciseId: string, completedSets: number, performance?: StudentSetPerformance) => void;
  clearSession: () => void;
};

export const useStudentTrainingSessionStore = create<StudentTrainingSessionState>((set) => ({
  session: undefined,
  studentId: undefined,
  isOfflineSnapshot: false,
  setSession: (session, isOfflineSnapshot = false, studentId) => set({ session, isOfflineSnapshot, studentId }),
  setOfflineSnapshot: (isOfflineSnapshot) => set({ isOfflineSnapshot }),
  updateExerciseProgress: (exerciseId, completedSets, performance) => set((state) => {
    if (!state.session) return state;
    return {
      ...state,
      session: {
        ...state.session,
        exercises: state.session.exercises.map((exercise) => exercise.id === exerciseId
          ? {
              ...exercise,
              completedSets: Math.max(exercise.completedSets, completedSets),
              isCompleted: Math.max(exercise.completedSets, completedSets) >= exercise.sets,
              performances: performance
                ? [...(exercise.performances ?? []).filter((item) => item.setNumber !== performance.setNumber), performance].sort((left, right) => left.setNumber - right.setNumber)
                : exercise.performances,
            }
          : exercise),
      },
    };
  }),
  clearSession: () => set({ session: undefined, studentId: undefined, isOfflineSnapshot: false }),
}));

/** API sequence is persisted data; this helper only provides a stable view for selection. */
export function orderedExercises(session: StudentSession): SessionExercise[] {
  return [...session.exercises].sort((left, right) => left.sequence - right.sequence);
}

export function currentExercise(session: StudentSession): SessionExercise | undefined {
  return orderedExercises(session).find((exercise) => !isExerciseCompleted(exercise));
}

export function isExerciseCompleted(exercise: SessionExercise): boolean {
  return Boolean(exercise.isCompleted || exercise.confirmedWithoutDetails || exercise.completedSets >= exercise.sets);
}

/** Merge only locally queued facts. Server progress remains the base and can
 * never be made smaller by an offline snapshot or a retry refresh. */
export function withPendingProgress(session: StudentSession, pending: Record<string, number[]> | PendingSetDetail[]): StudentSession {
  const pendingNumbers = Array.isArray(pending)
    ? pending.reduce<Record<string, number[]>>((result, item) => { (result[item.exerciseId] ??= []).push(item.setNumber); return result; }, {})
    : pending;
  const pendingPerformances = Array.isArray(pending) ? pending : [];
  return {
    ...session,
    exercises: session.exercises.map((exercise) => {
      const pendingSetNumbers = new Set(pendingNumbers[exercise.id] ?? []);
      let completedSets = Math.min(exercise.sets, exercise.completedSets);
      // Only merge a contiguous local tail. A stale/future row must never
      // make the UI skip a server-authoritative set after a restart.
      while (completedSets < exercise.sets && pendingSetNumbers.has(completedSets + 1)) completedSets += 1;
      const performances = pendingPerformances.filter((item) => item.exerciseId === exercise.id).reduce((result, item) => result.some((current) => current.setNumber === item.setNumber) ? result : [...result, { setNumber: item.setNumber, weightKg: item.weightKg, repetitions: item.repetitions, durationSeconds: item.durationSeconds, completedAt: item.completedAt }], [...(exercise.performances ?? [])]).sort((left, right) => left.setNumber - right.setNumber);
      return { ...exercise, completedSets, performances, isCompleted: exercise.isCompleted || completedSets >= exercise.sets };
    }),
  };
}

export function nextSetNumber(session: StudentSession): { exerciseId?: string; setNumber?: number } {
  const exercise = currentExercise(session);
  return exercise ? { exerciseId: exercise.id, setNumber: exercise.completedSets + 1 } : {};
}

export function exerciseProgressState(session: StudentSession, exercise: SessionExercise): ExerciseProgressState {
  if (isExerciseCompleted(exercise)) return 'completed';
  return currentExercise(session)?.id === exercise.id ? 'current' : 'pending';
}

export function sessionProgress(session: StudentSession) {
  const totalSets = session.exercises.reduce((total, exercise) => total + exercise.sets, 0);
  const completedSets = session.exercises.reduce((total, exercise) => total + (isExerciseCompleted(exercise) ? exercise.sets : Math.min(exercise.completedSets, exercise.sets)), 0);
  return { completedSets, totalSets, percentage: totalSets === 0 ? 0 : Math.round((completedSets / totalSets) * 100) };
}
