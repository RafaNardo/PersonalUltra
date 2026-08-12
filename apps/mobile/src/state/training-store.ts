import { create } from 'zustand';

type TrainingState = { sessionId?: string; restSeconds: number; setActiveSession: (id?: string) => void; setRestSeconds: (seconds: number) => void };

export const useTrainingStore = create<TrainingState>((set) => ({
  sessionId: undefined,
  restSeconds: 0,
  setActiveSession: (sessionId) => set({ sessionId }),
  setRestSeconds: (restSeconds) => set({ restSeconds }),
}));
