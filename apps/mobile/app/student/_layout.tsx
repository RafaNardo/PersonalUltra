import { Stack } from 'expo-router';

/**
 * Student owns an independently extractable navigation tree.
 * Focused workout routes live outside the tab group so they do not render the
 * public navigation while a session is in progress.
 */
export default function StudentLayout() {
  return (
    <Stack screenOptions={{ headerShown: false, animation: 'fade' }}>
      <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
      <Stack.Screen name="training/start" options={{ headerShown: false }} />
      <Stack.Screen name="training/preview/[id]" options={{ headerShown: false }} />
      <Stack.Screen name="training/[id]" options={{ headerShown: false }} />
      <Stack.Screen name="training/summary/[sessionId]" options={{ headerShown: false }} />
      <Stack.Screen name="exercise/[sessionId]/[exerciseId]" options={{ headerShown: false }} />
      <Stack.Screen name="rest/[sessionId]/[exerciseId]" options={{ headerShown: false }} />
    </Stack>
  );
}
