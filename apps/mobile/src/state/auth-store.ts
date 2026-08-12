import Storage from 'expo-sqlite/kv-store';
import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';

type AuthState = { accessToken?: string; memberName?: string; hasHydrated: boolean; markHydrated: () => void; signIn: (token: string, name: string) => void; signOut: () => void };

export const useAuthStore = create<AuthState>()(persist((set) => ({
  accessToken: undefined,
  memberName: undefined,
  hasHydrated: false,
  markHydrated: () => set({ hasHydrated: true }),
  signIn: (accessToken, memberName) => set({ accessToken, memberName }),
  signOut: () => set({ accessToken: undefined, memberName: undefined }),
}), {
  name: 'personal-ultra-auth',
  storage: createJSONStorage(() => Storage),
  onRehydrateStorage: () => (state) => state?.markHydrated(),
}));
