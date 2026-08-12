import { router } from 'expo-router';
import { useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { ErrorView, LoadingView } from '@/src/components/ui';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { StudentWaitingHome } from '@/src/features/student/invite/waiting-home';

export default function StudentAccessScreen() {
  const session = useInviteSessionStore((state) => state.session);
  const anamnesis = useQuery({ queryKey: ['student', 'anamnesis', session?.studentId], queryFn: () => inviteApi.anamnesis(session!.accessToken), enabled: Boolean(session?.accessToken) });

  useEffect(() => {
    if (!session) { router.replace('/login'); return; }
    if (anamnesis.data && !anamnesis.data.isCompleted) router.replace('/invite/resume/anamnesis');
  }, [anamnesis.data, session]);

  if (!session || anamnesis.isLoading) return <LoadingView message="Abrindo seu acompanhamento…" />;
  if (anamnesis.isError) return <ErrorView message={anamnesis.error.message} onRetry={() => anamnesis.refetch()} />;
  if (!anamnesis.data!.isCompleted) return null;
  return <StudentWaitingHome />;
}
