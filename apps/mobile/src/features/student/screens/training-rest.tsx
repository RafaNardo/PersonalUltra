import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { AppState, StyleSheet, Text, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentSession } from '@/src/features/student/invite/api';
import { ApiError } from '@/src/api/shared-http';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { cacheWorkout, cachedSession, pendingSetDetails } from '@/src/features/student/offline/training-db';
import { currentExercise, orderedExercises, useStudentTrainingSessionStore, withPendingProgress } from '@/src/features/student/training/session-state';

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
  const next = currentExercise(session);
  // A rest route is valid only after the origin exercise has recorded a set.
  // Exercise order is a suggestion, so a later exercise may legitimately be
  // the origin when the Student chose it from the session overview.
  const invalidOrigin = !exercise || exercise.completedSets < 1 || !ordered.some((item) => item.id === exercise.id);
  if (invalidOrigin) {
    return <ErrorView message="Este descanso não corresponde à sequência atual da sessão." onRetry={() => router.replace({ pathname: '/student/training/[id]', params: { id: session.workoutId } })} />;
  }

  return <RestTimer session={session} exercise={exercise} next={next} isOfflineSnapshot={isOfflineSnapshot} />;
}

function RestTimer({ session, exercise, next, isOfflineSnapshot }: { session: StudentSession; exercise: StudentSession['exercises'][number]; next?: StudentSession['exercises'][number]; isOfflineSnapshot: boolean }) {
  const restSeconds = Math.max(0, Math.floor(Number(exercise.restSeconds) || 0));
  const continuation = exercise.completedSets < exercise.sets ? exercise : next;
  const [targetAt, setTargetAt] = useState<number>();
  const [clock, setClock] = useState(() => Date.now());

  useEffect(() => {
    setTargetAt(Date.now() + restSeconds * 1000);
  }, [exercise.id, restSeconds]);

  useEffect(() => {
    if (targetAt === undefined) return;
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
  const continueFromRest = () => {
    const latest = useStudentTrainingSessionStore.getState().session;
    const destinationSession = latest?.sessionId === session.sessionId ? latest : session;
    const origin = destinationSession.exercises.find((item) => item.id === exercise.id);
    const destination = origin && origin.completedSets < origin.sets ? origin : currentExercise(destinationSession);
    if (destination) {
      router.replace({ pathname: '/student/exercise/[sessionId]/[exerciseId]', params: { sessionId: destinationSession.sessionId, exerciseId: destination.id } });
    } else {
      router.replace({ pathname: '/student/training/[id]', params: { id: destinationSession.workoutId } });
    }
  };
  const addThirtySeconds = () => {
    setTargetAt((current) => Math.max(current ?? Date.now(), Date.now()) + 30_000);
    setClock(Date.now());
  };

  return <Screen style={styles.page}>
    <TopBar eyebrow="DESCANSO" title="Recupere o fôlego" onBack={() => router.back()} />
    {isOfflineSnapshot ? <Card style={styles.offlineCard}><Text style={styles.offlineTitle}>Modo offline</Text><Text style={styles.copy}>A série anterior está salva neste dispositivo e será sincronizada quando a conexão voltar.</Text></Card> : null}
    <Card style={styles.timerCard}>
      <Text style={styles.completedLabel}>SÉRIE REGISTRADA</Text>
      <Text style={styles.exerciseName}>{exercise.name}</Text>
      <Text accessibilityRole="timer" accessibilityLiveRegion="polite" style={styles.timer}>{formatTime(remaining)}</Text>
      <Text style={styles.copy}>{finished ? (continuation ? `Descanso concluído. Próximo sugerido: ${continuation.name}.` : 'Descanso concluído. Volte à visão geral para finalizar.') : 'Use este intervalo conforme a prescrição do seu personal.'}</Text>
    </Card>
    <View style={styles.actions}>
      <Button variant="secondary" onPress={addThirtySeconds}>+30 segundos</Button>
      <Button variant="secondary" onPress={continueFromRest}>Pular descanso</Button>
      <Button variant="secondary" onPress={() => router.replace({ pathname: '/student/training/[id]', params: { id: session.workoutId } })}>Escolher outro exercício</Button>
      <Button disabled={!finished} onPress={continueFromRest}>{continuation ? (continuation.id === exercise.id ? 'Próxima série' : `Próximo sugerido: ${continuation.name}`) : 'Ver visão geral'}</Button>
    </View>
  </Screen>;
}

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
});
