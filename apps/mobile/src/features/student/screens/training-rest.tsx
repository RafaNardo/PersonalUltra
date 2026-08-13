import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { AppState, StyleSheet, Text, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentSession } from '@/src/features/student/invite/api';
import { ApiError } from '@/src/api/shared-http';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { cacheWorkout, cachedSession, clearCachedSession, pendingSetCount, pendingSetDetails, syncPendingSets } from '@/src/features/student/offline/training-db';
import { currentExercise, orderedExercises, useStudentTrainingSessionStore, withPendingProgress } from '@/src/features/student/training/session-state';
import { useQueryClient, useMutation } from '@tanstack/react-query';

/**
 * Rest is deliberately UI-only. The set has already been persisted (or
 * queued) by the focused exercise screen; this route only controls the
 * transition to the next persisted exercise in the shared session snapshot.
 */
export function StudentTrainingRestScreen() {
  const { sessionId, exerciseId } = useLocalSearchParams<{ sessionId: string; exerciseId: string }>();
  const authSession = useInviteSessionStore((state) => state.session);
  const session = useStudentTrainingSessionStore((state) => state.session);
  const ownerStudentId = useStudentTrainingSessionStore((state) => state.studentId);
  const isOfflineSnapshot = useStudentTrainingSessionStore((state) => state.isOfflineSnapshot);
  const setSession = useStudentTrainingSessionStore((state) => state.setSession);
  const [loading, setLoading] = useState(!session || session.sessionId !== sessionId || ownerStudentId !== authSession?.studentId);
  const [error, setError] = useState<string>();

  useEffect(() => {
    if (session?.sessionId === sessionId && ownerStudentId === authSession?.studentId) return;
    if (!sessionId || !authSession) return;
    void (async () => {
      try {
        const server = await inviteApi.session(authSession.accessToken, sessionId);
        const pending = await pendingSetDetails(server.sessionId, authSession.studentId);
        const hydrated = withPendingProgress(server, pending);
        setSession(hydrated, false, authSession.studentId);
        await cacheWorkout(hydrated, authSession.studentId).catch(() => undefined);
        return;
      } catch (loadError) {
        if (!(loadError instanceof ApiError) || loadError.status !== 0) { setError(loadError instanceof Error ? loadError.message : 'Não foi possível recuperar a sessão.'); return; }
      }
      try {
        const cached = await cachedSession<StudentSession>(sessionId, authSession.studentId);
        if (!cached) { setError('A sessão não está disponível neste dispositivo.'); return; }
        const pending = await pendingSetDetails(cached.sessionId, authSession.studentId);
        setSession(withPendingProgress(cached, pending), true, authSession.studentId);
      } catch { setError('Não foi possível recuperar a sessão salva.'); }
    })().finally(() => setLoading(false));
  }, [session?.sessionId, ownerStudentId, sessionId, authSession, setSession]);

  if (!authSession) { router.replace('/login'); return null; }
  if (loading) return <LoadingView message="Preparando seu descanso…" />;
  if (error || !session || session.sessionId !== sessionId) return <ErrorView message={error ?? 'Sessão indisponível.'} onRetry={() => router.back()} />;

  const ordered = orderedExercises(session);
  const exercise = session.exercises.find((item) => item.id === exerciseId);
  // A rest route is valid only after the origin exercise has recorded a set.
  // Exercise order is a suggestion, so a later exercise may legitimately be
  // the origin when the Student chose it from the session overview.
  const invalidOrigin = !exercise || exercise.completedSets < 1 || !ordered.some((item) => item.id === exercise.id);
  if (invalidOrigin) {
    return <ErrorView message="Este descanso não corresponde à sequência atual da sessão." onRetry={() => router.replace({ pathname: '/student/training/[id]', params: { id: session.workoutId } })} />;
  }

  return <RestTimer session={session} exercise={exercise} isOfflineSnapshot={isOfflineSnapshot} authToken={authSession.accessToken} studentId={authSession.studentId} />;
}

function RestTimer({ session, exercise, isOfflineSnapshot, authToken, studentId }: { session: StudentSession; exercise: StudentSession['exercises'][number]; isOfflineSnapshot: boolean; authToken: string; studentId: string }) {
  const restSeconds = Math.max(0, Math.floor(Number(exercise.restSeconds) || 0));
  const pendingExercises = orderedExercises(session).filter((item) => item.completedSets < item.sets);
  const exerciseComplete = exercise.completedSets >= exercise.sets;
  const sessionComplete = pendingExercises.length === 0;
  const queryClient = useQueryClient();
  const clearSession = useStudentTrainingSessionStore((state) => state.clearSession);
  const [completionError, setCompletionError] = useState<string>();
  const complete = useMutation({
    onMutate: () => setCompletionError(undefined),
    mutationFn: async () => {
      const sync = await synchronizePendingSets(authToken, studentId);
      if (sync.failed > 0) throw new Error('Conecte-se e aguarde a sincronização das séries pendentes antes de concluir.');
      if (await pendingSetCount(session.sessionId, studentId) > 0) throw new Error('Há séries pendentes de sincronização. Conecte-se e tente novamente.');
      const authoritative = await inviteApi.session(authToken, session.sessionId);
      if (authoritative.exercises.some((item) => item.completedSets < item.sets)) throw new Error('A sessão ainda não está completa no servidor.');
      return inviteApi.completeWorkout(authToken, session.sessionId);
    },
    onSuccess: async () => {
      await clearCachedSession(session.sessionId, studentId).catch(() => undefined);
      await queryClient.invalidateQueries({ queryKey: ['student', studentId, 'training'] });
      clearSession();
      router.replace({ pathname: '/student/training/summary/[sessionId]', params: { sessionId: session.sessionId } });
    },
    onError: (error: Error) => setCompletionError(error instanceof ApiError && error.status === 0 ? 'Conecte-se à internet para concluir o treino.' : error.message),
  });
  const [targetAt, setTargetAt] = useState<number>();
  const [clock, setClock] = useState(() => Date.now());
  const latestPerformance = exercise.performances?.find((item) => item.setNumber === exercise.completedSets) ?? exercise.performances?.at(-1);

  useEffect(() => {
    if (sessionComplete) return;
    const completedAt = latestPerformance ? new Date(latestPerformance.completedAt).getTime() : Date.now();
    setTargetAt((Number.isNaN(completedAt) ? Date.now() : completedAt) + restSeconds * 1000);
  }, [exercise.id, exercise.completedSets, latestPerformance?.completedAt, restSeconds, sessionComplete]);

  useEffect(() => {
    if (targetAt === undefined || sessionComplete) return;
    const tick = () => setClock(Date.now());
    tick();
    const interval = setInterval(tick, 250);
    const subscription = AppState.addEventListener('change', (state) => {
      // The target is an absolute wall-clock timestamp. If the app was in
      // background, the first active tick immediately catches up instead of
      // incorrectly pausing the prescribed rest.
      if (state === 'active') tick();
    });
    return () => { clearInterval(interval); subscription.remove(); };
  }, [targetAt]);

  const remaining = targetAt === undefined ? restSeconds : Math.max(0, Math.ceil((targetAt - clock) / 1000));
  const finished = remaining === 0;
  if (sessionComplete) return <SessionReady session={session} isOfflineSnapshot={isOfflineSnapshot} complete={complete} completionError={completionError} />;

  const continueFromRest = () => {
    const latest = useStudentTrainingSessionStore.getState().session;
    const destinationSession = latest?.sessionId === session.sessionId ? latest : session;
    const origin = destinationSession.exercises.find((item) => item.id === exercise.id);
    const destination = origin && origin.completedSets < origin.sets ? origin : currentExercise(destinationSession);
    if (destination) router.replace({ pathname: '/student/exercise/[sessionId]/[exerciseId]', params: { sessionId: destinationSession.sessionId, exerciseId: destination.id } });
  };

  return <Screen style={styles.page}>
    <TopBar eyebrow={exerciseComplete ? 'EXERCÍCIO CONCLUÍDO' : 'DESCANSO'} title={exerciseComplete ? 'Você terminou este exercício' : 'Recupere o fôlego'} onBack={() => router.back()} />
    {isOfflineSnapshot ? <Card style={styles.offlineCard}><Text style={styles.offlineTitle}>Modo offline</Text><Text style={styles.copy}>A série anterior está salva neste dispositivo e será sincronizada quando a conexão voltar.</Text></Card> : null}
    <Card style={styles.timerCard}>
      <Text style={styles.completedLabel}>{isOfflineSnapshot ? 'SÉRIE SALVA NESTE DISPOSITIVO' : 'SÉRIE REGISTRADA'}</Text>
      <Text style={styles.exerciseName}>{exercise.name}</Text>
      <Text accessibilityRole="timer" accessibilityLabel={`Descanso: ${formatTime(remaining)} restantes`} style={styles.timer}>{formatTime(remaining)}</Text>
      <Text style={styles.copy}>{finished ? (exerciseComplete ? 'Escolha qualquer exercício pendente para continuar. A sequência do personal é uma sugestão.' : 'Descanso concluído. Continue quando estiver pronto.') : 'Use este intervalo conforme a prescrição do seu personal.'}</Text>
    </Card>
    {exerciseComplete ? <PendingExerciseList session={session} exercises={pendingExercises} restFinished={finished} /> : null}
    {!exerciseComplete ? <View style={styles.actions}><Button onPress={continueFromRest}>{finished ? 'Próxima série' : 'Pular descanso'}</Button></View> : null}
  </Screen>;
}

function PendingExerciseList({ session, exercises, restFinished }: { session: StudentSession; exercises: StudentSession['exercises']; restFinished: boolean }) {
  const openExercise = (exerciseId: string) => {
    const latest = useStudentTrainingSessionStore.getState().session;
    const source = latest?.sessionId === session.sessionId ? latest : session;
    const selected = source.exercises.find((item) => item.id === exerciseId && item.completedSets < item.sets) ?? currentExercise(source);
    if (selected) router.replace({ pathname: '/student/exercise/[sessionId]/[exerciseId]', params: { sessionId: source.sessionId, exerciseId: selected.id } });
  };
  return <View style={styles.pendingList}><Text style={styles.pendingTitle}>Escolha o próximo exercício</Text>{exercises.map((item, index) => <Card key={item.id} style={styles.pendingCard}><View style={styles.pendingCopy}><Text style={styles.pendingName}>{item.name}</Text><Text style={styles.copy}>{item.completedSets}/{item.sets} séries · {item.repetitionsMin}–{item.repetitionsMax} reps{index === 0 ? ' · próximo sugerido' : ''}</Text></View><Button variant="secondary" accessibilityLabel={`${restFinished ? 'Escolher' : 'Escolher e pular descanso'} ${item.name}`} onPress={() => openExercise(item.id)} accessibilityHint={`Abre ${item.name} para registrar a próxima série`}>{restFinished ? 'Escolher' : 'Escolher e pular descanso'}</Button></Card>)}</View>;
}

function SessionReady({ session, isOfflineSnapshot, complete, completionError }: { session: StudentSession; isOfflineSnapshot: boolean; complete: { isPending: boolean; reset: () => void; mutate: () => void }; completionError?: string }) {
  return <Screen style={styles.page}><TopBar eyebrow="SESSÃO PRONTA" title="Treino concluído" onBack={() => router.back()} />{isOfflineSnapshot ? <Card style={styles.offlineCard}><Text style={styles.offlineTitle}>Você está sem conexão</Text><Text style={styles.copy}>A conclusão precisa ser confirmada pelo servidor quando a conexão voltar.</Text></Card> : null}<Card style={styles.readyCard}><Text style={styles.completedLabel}>VOCÊ TERMINOU ESSA SESSÃO</Text><Text style={styles.readyTitle}>Mandou bem.</Text><Text style={styles.copy}>Todas as séries de {session.exercises.length} exercícios foram registradas. Quando estiver online, confirme a conclusão para abrir seu resumo.</Text></Card><Button variant="success" loading={complete.isPending} onPress={() => { complete.reset(); complete.mutate(); }}>Concluir treino</Button>{completionError ? <Text accessibilityRole="alert" style={styles.error}>{completionError}</Text> : null}</Screen>;
}

async function synchronizePendingSets(token: string, studentId: string) { return syncPendingSets(studentId, async (item) => { await inviteApi.completeSet(token, item.sessionId, item.exerciseId, item.input); }); }

function formatTime(seconds: number) {
  const minutes = Math.floor(seconds / 60);
  const rest = seconds % 60;
  return `${String(minutes).padStart(2, '0')}:${String(rest).padStart(2, '0')}`;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.lg },
  copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
  offlineCard: { gap: spacing.xs, borderColor: colors.warning },
  offlineTitle: { ...typography.headingMD, color: colors.warning },
  timerCard: { alignItems: 'center', gap: spacing.md, paddingVertical: spacing.xl * 1.5, borderColor: colors.primary },
  completedLabel: { ...typography.caption, color: colors.primary, letterSpacing: 1 },
  exerciseName: { ...typography.headingLG, color: colors.textPrimary, textAlign: 'center' },
  timer: { ...typography.displayLG, color: colors.primary, fontVariant: ['tabular-nums'] },
  actions: { gap: spacing.sm },
  pendingList: { gap: spacing.sm },
  pendingTitle: { ...typography.headingMD, color: colors.textPrimary },
  pendingCard: { gap: spacing.sm },
  pendingCopy: { gap: spacing.xxs },
  pendingName: { ...typography.bodyLG, color: colors.textPrimary },
  readyCard: { gap: spacing.md, borderColor: colors.primary },
  readyTitle: { ...typography.displayLG, color: colors.textPrimary },
  error: { ...typography.caption, color: colors.danger },
});
