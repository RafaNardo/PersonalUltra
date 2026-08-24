import { create } from 'zustand';
import type { TrainerExerciseCatalogItem, TrainerStudentWorkout, TrainerStudentWorkoutExercise } from '@/src/api/trainer-client';
import type { ExercisePrescriptionDraft } from './prescription';
import { parseExercisePrescription } from './prescription';

export type WorkoutEditorExercise = Omit<TrainerStudentWorkoutExercise, 'id' | 'sequence'> & {
  clientId: string;
  id?: string;
};

type WorkoutDraft = {
  name: string;
  exercises: WorkoutEditorExercise[];
  dirty: boolean;
};

type WorkoutEditorState = {
  drafts: Record<string, WorkoutDraft>;
  initialize: (key: string, workout: TrainerStudentWorkout) => void;
  resetFromServer: (key: string, workout: TrainerStudentWorkout) => void;
  setName: (key: string, name: string) => void;
  addExercise: (key: string, exercise: TrainerExerciseCatalogItem, prescription: ExercisePrescriptionDraft) => boolean;
  updateExercise: (key: string, clientId: string, prescription: ExercisePrescriptionDraft) => boolean;
  removeExercise: (key: string, clientId: string) => void;
  moveExercise: (key: string, from: number, to: number) => void;
};

let nextDraftExerciseId = 0;

export const useWorkoutEditorStore = create<WorkoutEditorState>((set) => ({
  drafts: {},
  initialize: (key, workout) => set((state) => state.drafts[key]?.dirty ? state : ({ drafts: { ...state.drafts, [key]: fromServer(workout) } })),
  resetFromServer: (key, workout) => set((state) => ({ drafts: { ...state.drafts, [key]: fromServer(workout) } })),
  setName: (key, name) => set((state) => {
    const current = state.drafts[key];
    if (!current || current.name === name) return state;
    return { drafts: { ...state.drafts, [key]: { ...current, name, dirty: true } } };
  }),
  addExercise: (key, exercise, draft) => {
    const prescription = parseExercisePrescription(draft);
    if (!prescription) return false;
    let added = false;
    set((state) => {
      const current = state.drafts[key];
      if (!current || current.exercises.length >= 30) return state;
      added = true;
      const item: WorkoutEditorExercise = {
        clientId: `new-${Date.now()}-${++nextDraftExerciseId}`,
        exerciseId: exercise.id,
        name: exercise.name,
        primaryMuscleGroup: exercise.primaryMuscleGroup,
        equipment: exercise.equipment,
        imageRef: exercise.imageRef,
        imageUrl: exercise.imageUrl,
        instructions: exercise.instructions,
        ...prescription,
      };
      return { drafts: { ...state.drafts, [key]: { ...current, exercises: [...current.exercises, item], dirty: true } } };
    });
    return added;
  },
  updateExercise: (key, clientId, draft) => {
    const prescription = parseExercisePrescription(draft);
    if (!prescription) return false;
    let updated = false;
    set((state) => {
      const current = state.drafts[key];
      if (!current || !current.exercises.some((item) => item.clientId === clientId)) return state;
      updated = true;
      return { drafts: { ...state.drafts, [key]: { ...current, exercises: current.exercises.map((item) => item.clientId === clientId ? { ...item, ...prescription } : item), dirty: true } } };
    });
    return updated;
  },
  removeExercise: (key, clientId) => set((state) => {
    const current = state.drafts[key];
    if (!current) return state;
    const exercises = current.exercises.filter((item) => item.clientId !== clientId);
    if (exercises.length === current.exercises.length) return state;
    return { drafts: { ...state.drafts, [key]: { ...current, exercises, dirty: true } } };
  }),
  moveExercise: (key, from, to) => set((state) => {
    const current = state.drafts[key];
    if (!current || from < 0 || from >= current.exercises.length || to < 0 || to >= current.exercises.length || from === to) return state;
    const exercises = [...current.exercises];
    const [moved] = exercises.splice(from, 1);
    exercises.splice(to, 0, moved);
    return { drafts: { ...state.drafts, [key]: { ...current, exercises, dirty: true } } };
  }),
}));

export function workoutEditorKey(studentId: string, workoutId: string) {
  return `${studentId}:${workoutId}`;
}

function fromServer(workout: TrainerStudentWorkout): WorkoutDraft {
  return {
    name: workout.name,
    exercises: workout.exercises
      .slice()
      .sort((a, b) => a.sequence - b.sequence)
      .map(({ id, sequence: _sequence, ...exercise }) => ({ ...exercise, id, clientId: id })),
    dirty: false,
  };
}
