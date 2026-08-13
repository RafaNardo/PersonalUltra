import * as SQLite from 'expo-sqlite';
import { stripLegacyToken, syncSequentially, type SyncResult } from './sync-queue';
// Kept actor-local so offline persistence can outlive the first workout API.
export type CompleteSetInput = { clientOperationId: string; setNumber: number; weightKg: number; repetitions: number; repsInReserve?: number | null };
export type CachedWorkoutSnapshot = {
  sessionId: string;
  workoutId: string;
  workoutName: string;
  status: string;
  exercises: Array<{
    id: string;
    exerciseId?: string;
    name: string;
    primaryMuscleGroup?: string;
    equipment?: string;
    imageRef?: string;
    instructions?: string;
    sequence: number;
    sets: number;
    repetitionsMin: number;
    repetitionsMax: number;
    restSeconds: number;
    notes: string;
    completedSets: number;
  }>;
};

let databasePromise: Promise<SQLite.SQLiteDatabase> | undefined;

async function database() {
  databasePromise ??= SQLite.openDatabaseAsync('personal-ultra.db');
  return databasePromise;
}

export async function initializeTrainingDatabase() {
  const db = await database();
  await db.execAsync(`
    PRAGMA journal_mode = WAL;
    CREATE TABLE IF NOT EXISTS cached_workout (session_id TEXT PRIMARY KEY NOT NULL, payload TEXT NOT NULL, updated_at TEXT NOT NULL);
    CREATE TABLE IF NOT EXISTS cached_exercises (exercise_id TEXT PRIMARY KEY NOT NULL, session_id TEXT NOT NULL, payload TEXT NOT NULL, updated_at TEXT NOT NULL);
    CREATE TABLE IF NOT EXISTS pending_operations (client_operation_id TEXT PRIMARY KEY NOT NULL, operation_type TEXT NOT NULL, payload TEXT NOT NULL, created_at TEXT NOT NULL);
    CREATE TABLE IF NOT EXISTS local_sets (client_operation_id TEXT PRIMARY KEY NOT NULL, session_id TEXT NOT NULL, exercise_id TEXT NOT NULL, payload TEXT NOT NULL, created_at TEXT NOT NULL);
  `);
  // Older demo builds duplicated the Student token in every queued operation.
  // Rewrite compatible payloads once; authentication remains in the session store.
  const legacyRows = await db.getAllAsync<{ client_operation_id: string; payload: string }>("SELECT client_operation_id, payload FROM pending_operations WHERE operation_type = 'complete_set'");
  for (const row of legacyRows) {
    try {
      const normalized = normalizePendingSet(JSON.parse(row.payload));
      await db.runAsync('UPDATE pending_operations SET payload = ? WHERE client_operation_id = ?', JSON.stringify(normalized), row.client_operation_id);
    } catch { /* Keep unreadable legacy rows visible to diagnostics instead of deleting user data. */ }
  }
}

export async function cacheWorkout(session: CachedWorkoutSnapshot) {
  const db = await database();
  const updatedAt = new Date().toISOString();
  await db.runAsync('INSERT OR REPLACE INTO cached_workout (session_id, payload, updated_at) VALUES (?, ?, ?)', session.sessionId, JSON.stringify(session), updatedAt);
  for (const exercise of session.exercises) {
    await db.runAsync('INSERT OR REPLACE INTO cached_exercises (exercise_id, session_id, payload, updated_at) VALUES (?, ?, ?, ?)', exercise.id, session.sessionId, JSON.stringify(exercise), updatedAt);
  }
}

export async function cachedWorkout<T>(workoutId?: string): Promise<T | undefined> {
  const db = await database();
  const rows = await db.getAllAsync<{ payload: string }>('SELECT payload FROM cached_workout ORDER BY updated_at DESC');
  for (const row of rows) {
    const parsed = JSON.parse(row.payload) as T & { workoutId?: string };
    if (!workoutId || parsed.workoutId === workoutId) return parsed;
  }
  return undefined;
}

export async function cachedSession<T extends { sessionId?: string }>(sessionId: string): Promise<T | undefined> {
  const db = await database();
  const row = await db.getFirstAsync<{ payload: string }>('SELECT payload FROM cached_workout WHERE session_id = ?', sessionId);
  if (!row) return undefined;
  return JSON.parse(row.payload) as T;
}

export type PendingSet = { sessionId: string; exerciseId: string; input: CompleteSetInput };

export function normalizePendingSet(value: unknown): PendingSet {
  const pending = stripLegacyToken(value as Partial<PendingSet> & { token?: string });
  if (!pending.sessionId || !pending.exerciseId || !pending.input?.clientOperationId) throw new Error('Invalid pending set payload.');
  return { sessionId: pending.sessionId, exerciseId: pending.exerciseId, input: pending.input };
}

export async function queueSet(pending: PendingSet) {
  const db = await database();
  const createdAt = new Date().toISOString();
  const payload = JSON.stringify(pending);
  await db.withTransactionAsync(async () => {
    await db.runAsync('INSERT OR REPLACE INTO pending_operations (client_operation_id, operation_type, payload, created_at) VALUES (?, ?, ?, ?)', pending.input.clientOperationId, 'complete_set', payload, createdAt);
    await db.runAsync('INSERT OR REPLACE INTO local_sets (client_operation_id, session_id, exercise_id, payload, created_at) VALUES (?, ?, ?, ?, ?)', pending.input.clientOperationId, pending.sessionId, pending.exerciseId, JSON.stringify(pending.input), createdAt);
  });
}

export async function pendingSets(): Promise<PendingSet[]> {
  const db = await database();
  const rows = await db.getAllAsync<{ payload: string }>("SELECT payload FROM pending_operations WHERE operation_type = 'complete_set' ORDER BY created_at");
  return rows.map((row) => normalizePendingSet(JSON.parse(row.payload)));
}

export async function pendingSetNumbers(sessionId: string): Promise<Record<string, number>> {
  const db = await database();
  const rows = await db.getAllAsync<{ exercise_id: string; payload: string }>('SELECT exercise_id, payload FROM local_sets WHERE session_id = ? ORDER BY created_at', sessionId);
  return rows.reduce<Record<string, number>>((result, row) => {
    const input = JSON.parse(row.payload) as CompleteSetInput;
    result[row.exercise_id] = Math.max(result[row.exercise_id] ?? 0, input.setNumber);
    return result;
  }, {});
}

export async function pendingSetCount(sessionId: string): Promise<number> {
  const db = await database();
  const row = await db.getFirstAsync<{ count: number }>('SELECT COUNT(*) AS count FROM local_sets WHERE session_id = ?', sessionId);
  return row?.count ?? 0;
}

export async function updateCachedExerciseProgress(sessionId: string, exerciseId: string, completedSets: number) {
  const db = await database();
  const row = await db.getFirstAsync<{ payload: string }>('SELECT payload FROM cached_workout WHERE session_id = ?', sessionId);
  if (!row) return;
  const snapshot = JSON.parse(row.payload) as CachedWorkoutSnapshot;
  const exercise = snapshot.exercises.find((item) => item.id === exerciseId);
  if (!exercise || exercise.completedSets >= completedSets) return;
  exercise.completedSets = completedSets;
  const updatedAt = new Date().toISOString();
  await db.withTransactionAsync(async () => {
    await db.runAsync('UPDATE cached_workout SET payload = ?, updated_at = ? WHERE session_id = ?', JSON.stringify(snapshot), updatedAt, sessionId);
    await db.runAsync('UPDATE cached_exercises SET payload = ?, updated_at = ? WHERE exercise_id = ? AND session_id = ?', JSON.stringify(exercise), updatedAt, exerciseId, sessionId);
  });
}

export async function removePendingSet(clientOperationId: string) {
  const db = await database();
  await db.runAsync('DELETE FROM pending_operations WHERE client_operation_id = ?', clientOperationId);
  await db.runAsync('DELETE FROM local_sets WHERE client_operation_id = ?', clientOperationId);
}

export async function clearTrainingData() {
  const db = await database();
  await db.execAsync('DELETE FROM pending_operations; DELETE FROM local_sets; DELETE FROM cached_exercises; DELETE FROM cached_workout;');
}

export async function syncPendingSets(send: (pending: PendingSet) => Promise<void>): Promise<SyncResult> {
  const pending = await pendingSets();
  return syncSequentially(pending, send, (item) => removePendingSet(item.input.clientOperationId));
}
