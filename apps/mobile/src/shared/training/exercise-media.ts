import type { ImageSource } from 'expo-image';

export function exerciseMediaSource(imageRef?: string, imageUrl?: string): ImageSource | undefined {
  const normalizedRef = imageRef?.trim().replace(/^\/+/, '');
  const remoteUrl = imageUrl?.trim();
  if (!normalizedRef || !remoteUrl) return undefined;
  try {
    const parsed = new URL(remoteUrl);
    return parsed.protocol === 'https:' && !parsed.username && !parsed.password
      ? { uri: parsed.toString(), cacheKey: normalizedRef }
      : undefined;
  } catch {
    return undefined;
  }
}
