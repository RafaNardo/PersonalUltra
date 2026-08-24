import * as SQLite from 'expo-sqlite';
import { stripLegacyToken, syncSequentially, type SyncResult } from './sync-queue';

export type CompleteSetInput = { clientOperationId: string; setNumber: number; weightKg: number; repetitions: number };
export type CachedWorkoutSnapshot = {
  studentId?: string;
  sessionId: string;
  workoutId: string;
  workoutName: string;
  status: string;
  exercises: Array<{ id: string; exerciseId?: string; name: string; primaryMuscleGroup?: string; equipment?: string; imageRef?: string; instructions?: string; sequence: number; sets: number; repetitionsMin: number; repetitionsMax: number; restSeconds: number; notes: string; completedSets: number; previousPerformance?: { setNumber: number; weightKg: number; repetitions: number; completedAt: string }; performances?: Array<{ setNumber: number; weightKg: number; repetitions: number; completedAt: string }> }>;
};

let databasePromise: Promise<SQLite.SQLiteDatabase> | undefined;
async function database() { databasePromise ??= SQLite.openDatabaseAsync('personal-ultra.db'); return databasePromise; }
async function addColumnIfMissing(db: SQLite.SQLiteDatabase, table: string, column: string, definition: string) {
  const columns = await db.getAllAsync<{ name: string }>(`PRAGMA table_info(${table})`);
  if (!columns.some((item) => item.name === column)) await db.execAsync(`ALTER TABLE ${table} ADD COLUMN ${column} ${definition}`);
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
  // Additive migration: legacy unscoped rows remain preserved, but are never
  // silently adopted by another Student. New writes are always owned.
  await addColumnIfMissing(db, 'cached_workout', 'student_id', 'TEXT');
  await addColumnIfMissing(db, 'cached_exercises', 'student_id', 'TEXT');
  await addColumnIfMissing(db, 'pending_operations', 'student_id', 'TEXT');
  await addColumnIfMissing(db, 'local_sets', 'student_id', 'TEXT');
  const legacyRows = await db.getAllAsync<{ client_operation_id: string; payload: string }>("SELECT client_operation_id, payload FROM pending_operations WHERE operation_type = 'complete_set' AND student_id IS NULL");
  for (const row of legacyRows) {
    try {
      const normalized = stripLegacyToken(JSON.parse(row.payload));
      // Keep old data, but normalize away any accidentally persisted token.
      await db.runAsync('UPDATE pending_operations SET payload = ? WHERE client_operation_id = ?', JSON.stringify(normalized), row.client_operation_id);
    } catch { /* Preserve unreadable legacy rows for diagnostics. */ }
  }
}

export async function cacheWorkout(session: CachedWorkoutSnapshot, studentId: string) {
  const db = await database();
  const updatedAt = new Date().toISOString();
  const stableSession = withoutSignedImageUrls(session);
  const ownedSession = { ...stableSession, studentId };
  await db.runAsync('INSERT OR REPLACE INTO cached_workout (session_id, student_id, payload, updated_at) VALUES (?, ?, ?, ?)', session.sessionId, studentId, JSON.stringify(ownedSession), updatedAt);
  for (const exercise of stableSession.exercises) await db.runAsync('INSERT OR REPLACE INTO cached_exercises (exercise_id, session_id, student_id, payload, updated_at) VALUES (?, ?, ?, ?, ?)', exercise.id, session.sessionId, studentId, JSON.stringify(exercise), updatedAt);
}

export async function cachedWorkout<T>(workoutId: string | undefined, studentId: string): Promise<T | undefined> {
  const db = await database();
  const rows = await db.getAllAsync<{ payload: string }>('SELECT payload FROM cached_workout WHERE student_id = ? ORDER BY updated_at DESC', studentId);
  for (const row of rows) { const parsed = withoutSignedImageUrls(JSON.parse(row.payload) as CachedWorkoutSnapshot) as unknown as T & { workoutId?: string }; if (!workoutId || parsed.workoutId === workoutId) return parsed; }
  return undefined;
}

export async function cachedSession<T extends { sessionId?: string }>(sessionId: string, studentId: string): Promise<T | undefined> {
  const db = await database();
  const row = await db.getFirstAsync<{ payload: string }>('SELECT payload FROM cached_workout WHERE session_id = ? AND student_id = ?', sessionId, studentId);
  return row ? withoutSignedImageUrls(JSON.parse(row.payload) as CachedWorkoutSnapshot) as unknown as T : undefined;
}

export type PendingSet = { studentId: string; sessionId: string; exerciseId: string; input: CompleteSetInput };
export type PendingSetNumbers = Record<string, number[]>;
export type PendingSetDetail = CompleteSetInput & { exerciseId: string; completedAt: string };
export function normalizePendingSet(value: unknown): PendingSet {
  const pending = stripLegacyToken(value as Partial<PendingSet> & { token?: string });
  if (!pending.studentId || !pending.sessionId || !pending.exerciseId || !pending.input?.clientOperationId) throw new Error('Invalid or unowned pending set payload.');
  return { studentId: pending.studentId, sessionId: pending.sessionId, exerciseId: pending.exerciseId, input: pending.input };
}

export async function queueSet(pending: PendingSet) {
  const db = await database(); const createdAt = new Date().toISOString(); const payload = JSON.stringify(pending);
  await db.withTransactionAsync(async () => {
    const existing = await db.getFirstAsync<{ student_id?: string; payload: string }>('SELECT student_id, payload FROM pending_operations WHERE client_operation_id = ?', pending.input.clientOperationId);
    if (existing) {
      let same = false;
      try {
        const current = normalizePendingSet(JSON.parse(existing.payload));
        same = current.studentId === pending.studentId && current.sessionId === pending.sessionId && current.exerciseId === pending.exerciseId && current.input.setNumber === pending.input.setNumber && current.input.weightKg === pending.input.weightKg && current.input.repetitions === pending.input.repetitions;
      } catch { /* Treat unreadable legacy rows as an ownership collision. */ }
      if (!same) throw new Error('A pending operation already belongs to another session or payload.');
    }
    const local = await db.getFirstAsync<{ student_id?: string; session_id: string; exercise_id: string; payload: string }>('SELECT student_id, session_id, exercise_id, payload FROM local_sets WHERE client_operation_id = ?', pending.input.clientOperationId);
    if (local && (local.student_id !== pending.studentId || local.session_id !== pending.sessionId || local.exercise_id !== pending.exerciseId)) throw new Error('A local set already belongs to another session.');
    await db.runAsync('INSERT OR IGNORE INTO pending_operations (client_operation_id, student_id, operation_type, payload, created_at) VALUES (?, ?, ?, ?, ?)', pending.input.clientOperationId, pending.studentId, 'complete_set', payload, createdAt);
    await db.runAsync('INSERT OR IGNORE INTO local_sets (client_operation_id, student_id, session_id, exercise_id, payload, created_at) VALUES (?, ?, ?, ?, ?, ?)', pending.input.clientOperationId, pending.studentId, pending.sessionId, pending.exerciseId, JSON.stringify(pending.input), createdAt);
  });
}

export async function pendingSets(studentId: string): Promise<PendingSet[]> {
  const db = await database();
  const rows = await db.getAllAsync<{ payload: string }>("SELECT payload FROM pending_operations WHERE operation_type = 'complete_set' AND student_id = ? ORDER BY created_at", studentId);
  return rows.flatMap((row) => { try { return [normalizePendingSet(JSON.parse(row.payload))]; } catch { return []; } });
}
export async function pendingSetNumbers(sessionId: string, studentId: string): Promise<PendingSetNumbers> {
  const db = await database(); const rows = await db.getAllAsync<{ exercise_id: string; payload: string }>('SELECT exercise_id, payload FROM local_sets WHERE session_id = ? AND student_id = ? ORDER BY created_at', sessionId, studentId);
  return rows.reduce<PendingSetNumbers>((result, row) => {
    try {
      const input = JSON.parse(row.payload) as CompleteSetInput;
      const numbers = result[row.exercise_id] ?? [];
      if (!numbers.includes(input.setNumber)) numbers.push(input.setNumber);
      result[row.exercise_id] = numbers.sort((left, right) => left - right);
    } catch { /* Ignore malformed rows; the sync queue remains diagnosable. */ }
    return result;
  }, {});
}
export async function pendingSetDetails(sessionId: string, studentId: string): Promise<PendingSetDetail[]> {
  const db = await database(); const rows = await db.getAllAsync<{ exercise_id: string; payload: string; created_at: string }>('SELECT exercise_id, payload, created_at FROM local_sets WHERE session_id = ? AND student_id = ? ORDER BY created_at', sessionId, studentId);
  return rows.flatMap((row) => {
    try { return [{ ...(JSON.parse(row.payload) as CompleteSetInput), exerciseId: row.exercise_id, completedAt: row.created_at }]; }
    catch { return []; }
  });
}
export async function pendingSetCount(sessionId: string, studentId: string): Promise<number> {
  const db = await database(); const row = await db.getFirstAsync<{ count: number }>('SELECT COUNT(*) AS count FROM local_sets WHERE session_id = ? AND student_id = ?', sessionId, studentId); return row?.count ?? 0;
}
export async function updateCachedExerciseProgress(sessionId: string, studentId: string, exerciseId: string, completedSets: number, performance?: { setNumber: number; weightKg: number; repetitions: number; completedAt: string }) {
  const db = await database(); const row = await db.getFirstAsync<{ payload: string }>('SELECT payload FROM cached_workout WHERE session_id = ? AND student_id = ?', sessionId, studentId); if (!row) return;
  const snapshot = withoutSignedImageUrls(JSON.parse(row.payload) as CachedWorkoutSnapshot); const exercise = snapshot.exercises.find((item) => item.id === exerciseId); if (!exercise) return; exercise.completedSets = Math.max(exercise.completedSets, completedSets); if (performance) exercise.performances = [...(exercise.performances ?? []).filter((item) => item.setNumber !== performance.setNumber), performance].sort((left, right) => left.setNumber - right.setNumber); const updatedAt = new Date().toISOString();
  await db.withTransactionAsync(async () => { await db.runAsync('UPDATE cached_workout SET payload = ?, updated_at = ? WHERE session_id = ? AND student_id = ?', JSON.stringify(snapshot), updatedAt, sessionId, studentId); await db.runAsync('UPDATE cached_exercises SET payload = ?, updated_at = ? WHERE exercise_id = ? AND session_id = ? AND student_id = ?', JSON.stringify(exercise), updatedAt, exerciseId, sessionId, studentId); });
}
export async function clearCachedSession(sessionId: string, studentId: string) {
  const db = await database();
  await db.withTransactionAsync(async () => {
    await db.runAsync('DELETE FROM cached_exercises WHERE session_id = ? AND student_id = ?', sessionId, studentId);
    await db.runAsync('DELETE FROM cached_workout WHERE session_id = ? AND student_id = ?', sessionId, studentId);
  });
}
export async function removePendingSet(clientOperationId: string, studentId: string) { const db = await database(); await db.runAsync('DELETE FROM pending_operations WHERE client_operation_id = ? AND student_id = ?', clientOperationId, studentId); await db.runAsync('DELETE FROM local_sets WHERE client_operation_id = ? AND student_id = ?', clientOperationId, studentId); }
export async function clearTrainingData() { const db = await database(); await db.execAsync('DELETE FROM pending_operations; DELETE FROM local_sets; DELETE FROM cached_exercises; DELETE FROM cached_workout;'); }
export async function syncPendingSets(studentId: string, send: (pending: PendingSet) => Promise<void>): Promise<SyncResult> { const pending = await pendingSets(studentId); return syncSequentially(pending, send, (item) => removePendingSet(item.input.clientOperationId, studentId)); }

function withoutSignedImageUrls<T extends CachedWorkoutSnapshot>(session: T): T {
  const exercises = session.exercises.map((exercise) => {
    const { imageUrl: _signedUrl, ...stableExercise } = exercise as typeof exercise & { imageUrl?: string };
    return stableExercise;
  });
  return { ...session, exercises } as T;
}
