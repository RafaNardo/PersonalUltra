import Storage from 'expo-sqlite/kv-store';
import type { WorkoutExercise, WorkoutTemplate } from '@/src/api/trainer-client';

const schemaVersion = 1;
const keyPrefix = 'personal-ultra-trainer-template-draft';

export type LocalTemplateDraft = {
  schemaVersion: typeof schemaVersion;
  id: string;
  sourceTemplateId: string;
  name: string;
  notes: string;
  exercises: WorkoutExercise[];
  updatedAt: string;
};

export async function createTemplateDraft(source: WorkoutTemplate) {
  const id = `${Date.now()}-${source.id}`;
  const draft: LocalTemplateDraft = {
    schemaVersion,
    id,
    sourceTemplateId: source.id,
    name: `${source.name} — cópia`,
    notes: source.notes,
    exercises: source.exercises ?? [],
    updatedAt: new Date().toISOString(),
  };
  await saveTemplateDraft(draft);
  return draft;
}

export async function saveTemplateDraft(draft: LocalTemplateDraft): Promise<LocalTemplateDraft> {
  const saved: LocalTemplateDraft = { ...draft, exercises: withoutSignedImageUrls(draft.exercises), schemaVersion, updatedAt: new Date().toISOString() };
  await Storage.setItem(`${keyPrefix}:${draft.id}`, JSON.stringify(saved));
  return saved;
}

export async function loadTemplateDraft(id: string): Promise<LocalTemplateDraft | undefined> {
  const key = `${keyPrefix}:${id}`;
  const raw = await Storage.getItem(key);
  if (!raw) return undefined;
  try {
    const value = JSON.parse(raw) as Partial<LocalTemplateDraft>;
    if (value.schemaVersion !== schemaVersion || value.id !== id || typeof value.sourceTemplateId !== 'string' || typeof value.name !== 'string' || typeof value.notes !== 'string' || !Array.isArray(value.exercises) || typeof value.updatedAt !== 'string') {
      await Storage.removeItem(key);
      return undefined;
    }
    return { ...value, exercises: withoutSignedImageUrls(value.exercises) } as LocalTemplateDraft;
  } catch {
    await Storage.removeItem(key);
    return undefined;
  }
}

export async function removeTemplateDraft(id: string) {
  await Storage.removeItem(`${keyPrefix}:${id}`);
}

function withoutSignedImageUrls(exercises: WorkoutExercise[]): WorkoutExercise[] {
  return exercises.map(({ imageUrl: _signedUrl, ...exercise }) => exercise as WorkoutExercise);
}
