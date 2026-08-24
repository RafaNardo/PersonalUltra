import { router, useLocalSearchParams } from 'expo-router';
import { useEffect } from 'react';
import { LoadingView } from '@/src/components/ui';

/** Compatibility redirect for links created before execution and rest became one screen. */
export function StudentTrainingRestScreen() {
  const { sessionId, exerciseId } = useLocalSearchParams<{ sessionId: string; exerciseId: string }>();
  useEffect(() => {
    if (sessionId && exerciseId) router.replace({ pathname: '/student/exercise/[sessionId]/[exerciseId]', params: { sessionId, exerciseId } });
  }, [exerciseId, sessionId]);
  return <LoadingView message="Retomando seu exercício…" />;
}
