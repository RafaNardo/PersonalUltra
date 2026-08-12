import Storage from 'expo-sqlite/kv-store';
import { create } from 'zustand';
import { createJSONStorage, persist } from 'zustand/middleware';
import type { InviteSession } from './api';

type InviteState = { session?: InviteSession; save: (session: InviteSession) => void; clear: () => void };

export const useInviteSessionStore = create<InviteState>()(persist((set) => ({ session: undefined, save: (session) => set({ session }), clear: () => set({ session: undefined }) }), { name: 'personal-ultra-invite-session', storage: createJSONStorage(() => Storage) }));
