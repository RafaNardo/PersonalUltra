import Storage from 'expo-sqlite/kv-store';
import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

export type DemoRole = 'trainer' | 'student';

type DemoRoleState = { role?: DemoRole; chooseRole: (role: DemoRole) => void };

// Demo composition only. Authorization remains entirely server-side.
export const useDemoRoleStore = create<DemoRoleState>()(persist((set) => ({
  role: undefined,
  chooseRole: (role) => set({ role }),
}), { name: 'personal-ultra-demo-role', storage: createJSONStorage(() => Storage) }));
