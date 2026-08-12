import * as SQLite from 'expo-sqlite';
import type { CompleteSetInput } from '@/src/api/types';

let databasePromise: Promise<SQLite.SQLiteDatabase> | undefined;

async function database() {
  databasePromise ??= SQLite.openDatabaseAsync('svr-method.db');
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
}

export async function cacheWorkout(session: { id: string; exercises: { id: string }[] }) {
  const db = await database();
  const updatedAt = new Date().toISOString();
  await db.runAsync('INSERT OR REPLACE INTO cached_workout (session_id, payload, updated_at) VALUES (?, ?, ?)', session.id, JSON.stringify(session), updatedAt);
  for (const exercise of session.exercises) {
    await db.runAsync('INSERT OR REPLACE INTO cached_exercises (exercise_id, session_id, payload, updated_at) VALUES (?, ?, ?, ?)', exercise.id, session.id, JSON.stringify(exercise), updatedAt);
  }
}

export async function cachedWorkout<T>(): Promise<T | undefined> {
  const db = await database();
  const row = await db.getFirstAsync<{ payload: string }>('SELECT payload FROM cached_workout ORDER BY updated_at DESC LIMIT 1');
  return row ? JSON.parse(row.payload) as T : undefined;
}

export type PendingSet = { token: string; sessionId: string; exerciseId: string; input: CompleteSetInput };

export async function queueSet(pending: PendingSet) {
  const db = await database();
  const createdAt = new Date().toISOString();
  const payload = JSON.stringify(pending);
  await db.runAsync('INSERT OR REPLACE INTO pending_operations (client_operation_id, operation_type, payload, created_at) VALUES (?, ?, ?, ?)', pending.input.clientOperationId, 'complete_set', payload, createdAt);
  await db.runAsync('INSERT OR REPLACE INTO local_sets (client_operation_id, session_id, exercise_id, payload, created_at) VALUES (?, ?, ?, ?, ?)', pending.input.clientOperationId, pending.sessionId, pending.exerciseId, JSON.stringify(pending.input), createdAt);
}

export async function pendingSets(): Promise<PendingSet[]> {
  const db = await database();
  const rows = await db.getAllAsync<{ payload: string }>("SELECT payload FROM pending_operations WHERE operation_type = 'complete_set' ORDER BY created_at");
  return rows.map((row) => JSON.parse(row.payload) as PendingSet);
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
