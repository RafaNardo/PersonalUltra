export type SyncResult = { synced: number; failed: number };

export function stripLegacyToken<T extends object>(value: T & { token?: unknown }): Omit<T, 'token'> {
  const { token: _, ...withoutToken } = value;
  return withoutToken;
}

export async function syncSequentially<T>(items: readonly T[], send: (item: T) => Promise<void>, remove: (item: T) => Promise<void>): Promise<SyncResult> {
  let synced = 0;
  for (const item of items) {
    try {
      await send(item);
      await remove(item);
      synced += 1;
    } catch {
      return { synced, failed: items.length - synced };
    }
  }
  return { synced, failed: 0 };
}
