import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useMemo, useRef, useState } from 'react';
import { AppState, Image, StyleSheet, Text, View } from 'react-native';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ApiError } from '@/src/api/shared-http';
import { Button, Card, EmptyState, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentSession } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { cacheWorkout, cachedWorkout, clearCachedSession, pendingSetCount, pendingSetDetails, syncPendingSets } from '@/src/features/student/offline/training-db';
import { currentExercise, exerciseProgressState, orderedExercises, sessionProgress, useStudentTrainingSessionStore, withPendingProgress } from '@/src/features/student/training/session-state';
import { exerciseMediaSource } from '@/src/shared/training/exercise-media';

export function StudentTrainingSessionScreen() {
  const { id, start } = useLocalSearchParams<{ id: string; start?: string }>();
  const authSession = useInviteSessionStore((state) => state.session);
  const session = useStudentTrainingSessionStore((state) => state.session);
  const ownerStudentId = useStudentTrainingSessionStore((state) => state.studentId);
  const ownerMatches = ownerStudentId === authSession?.studentId;
  const activeSession = ownerMatches && sameIdentifier(session?.workoutId, id) ? session : undefined;
  const isOfflineSnapshot = useStudentTrainingSessionStore((state) => state.isOfflineSnapshot);
  const setSession = useStudentTrainingSessionStore((state) => state.setSession);
  const [error, setError] = useState<string>();
  const [loadingSnapshot, setLoadingSnapshot] = useState(false);
  const [snapshotAttempted, setSnapshotAttempted] = useState(false);
  const automaticStartAttempt = useRef<string | undefined>(undefined);

  const startWorkout = useMutation({
    mutationFn: async () => {
      const sync = await synchronizePendingSets(authSession!.accessToken, authSession!.studentId);
      const resumed = await inviteApi.activeSession(authSession!.accessToken, id!);
      if (resumed) {
        const hydrated = await hydratePendingProgress(resumed, authSession!.studentId);
        return { session: hydrated, hasPending: await pendingSetCount(hydrated.sessionId, authSession!.studentId) > 0 };
      }
      if (sync.failed > 0) throw new Error('Não foi possível sincronizar as séries pendentes.');
      const started = await hydratePendingProgress(await inviteApi.startWorkout(authSession!.accessToken, id!), authSession!.studentId);
      return { session: started, hasPending: false };
    },
    onSuccess: async ({ session: started, hasPending }) => {
      setSession(started, hasPending, authSession!.studentId);
      try { await cacheWorkout(started, authSession!.studentId); } catch { /* The API response remains usable if local storage fails. */ }
    },
    onError: async (startError: Error) => {
      if (startError instanceof ApiError && startError.status === 0) {
        try {
          const cached = await cachedWorkout<StudentSession>(id, authSession!.studentId);
          if (cached) { setSession(await hydratePendingProgress(cached, authSession!.studentId), true, authSession!.studentId); setError(undefined); return; }
        } catch { /* Show the original connectivity error when no snapshot is available. */ }
      }
      setError(startError.message);
    },
  });

  const refresh = async () => {
    if (!authSession || !id || !activeSession) return;
    try {
      await synchronizePendingSets(authSession.accessToken, authSession.studentId);
      const refreshed = await inviteApi.activeSession(authSession.accessToken, id);
      if (!refreshed) return;
      const hydrated = await hydratePendingProgress(refreshed, authSession.studentId);
      setSession(hydrated, await pendingSetCount(hydrated.sessionId, authSession.studentId) > 0, authSession.studentId);
      await cacheWorkout(hydrated, authSession.studentId);
    } catch { /* Keep the shared snapshot visible while offline. */ }
  };

  useEffect(() => {
    if (!authSession || !id || activeSession || start !== '1' || startWorkout.isPending || startWorkout.isError) return;
    const attemptKey = `${authSession.studentId}:${id}`.toLowerCase();
    if (automaticStartAttempt.current === attemptKey) return;
    automaticStartAttempt.current = attemptKey;
    startWorkout.mutate();
  }, [authSession, id, activeSession, start, startWorkout.isPending, startWorkout.isError]);

  useEffect(() => {
    if (!authSession) return;
    const retry = () => { void synchronizePendingSets(authSession.accessToken, authSession.studentId).then(() => refresh()).catch(() => undefined); };
    const interval = setInterval(retry, 15_000);
    const subscription = AppState.addEventListener('change', (state) => { if (state === 'active' && !startWorkout.isPending) void refresh(); });
    return () => { clearInterval(interval); subscription.remove(); };
  }, [authSession, id, activeSession, startWorkout.isPending]);

  useEffect(() => {
    if (!authSession || !id || activeSession || start === '1' || loadingSnapshot || snapshotAttempted) return;
    setSnapshotAttempted(true);
    setLoadingSnapshot(true);
    void (async () => {
      try {
        await synchronizePendingSets(authSession.accessToken, authSession.studentId);
        const resumed = await inviteApi.activeSession(authSession.accessToken, id);
        if (resumed) {
          const hydrated = await hydratePendingProgress(resumed, authSession.studentId);
          setSession(hydrated, await pendingSetCount(hydrated.sessionId, authSession.studentId) > 0, authSession.studentId);
          await cacheWorkout(hydrated, authSession.studentId).catch(() => undefined);
          return;
        }
      } catch { /* Fall back to the owned offline snapshot below. */ }
      try {
        const cached = await cachedWorkout<StudentSession>(id, authSession.studentId);
        if (cached) setSession(await hydratePendingProgress(cached, authSession.studentId), true, authSession.studentId);
        else setError('Abra a prévia do treino e confirme o início para criar uma sessão.');
      } catch { setError('Não foi possível recuperar a sessão salva neste dispositivo.'); }
    })().finally(() => setLoadingSnapshot(false));
  }, [authSession, id, activeSession, start, loadingSnapshot, snapshotAttempted]);

  if (!authSession) { router.replace('/login'); return null; }
  if (!activeSession && (startWorkout.isPending || loadingSnapshot)) return <LoadingView message="Preparando seu treino…" />;
  if (!activeSession && (startWorkout.isError || error)) return <ErrorView message={error ?? startWorkout.error?.message ?? 'Não foi possível abrir este treino.'} onRetry={() => { setError(undefined); startWorkout.reset(); if (start === '1') { automaticStartAttempt.current = `${authSession.studentId}:${id}`.toLowerCase(); startWorkout.mutate(); } else router.back(); }} />;
  if (!activeSession) return <LoadingView message="Preparando seu treino…" />;

  return <SessionOverview session={activeSession} isOfflineSnapshot={isOfflineSnapshot} authToken={authSession.accessToken} />;
}

function sameIdentifier(left: string | undefined, right: string | undefined) {
  return Boolean(left && right && left.toLowerCase() === right.toLowerCase());
}

function SessionOverview({ session, isOfflineSnapshot, authToken }: { session: StudentSession; isOfflineSnapshot: boolean; authToken: string }) {
  const queryClient = useQueryClient();
  const exercises = useMemo(() => orderedExercises(session), [session]);
  const progress = sessionProgress(session);
  const current = currentExercise(session);
  const allComplete = Boolean(session.exercises.length) && !current;
  const [completionError, setCompletionError] = useState<string>();
  const clearSession = useStudentTrainingSessionStore((state) => state.clearSession);
  const studentId = useInviteSessionStore((state) => state.session?.studentId) ?? '';
  const complete = useMutation({
    mutationFn: async () => {
      const sync = await synchronizePendingSets(authToken, studentId);
      if (sync.failed > 0) throw new Error('Conecte-se e aguarde a sincronização das séries pendentes antes de concluir.');
      const pending = await pendingSetCount(session.sessionId, studentId);
      if (pending > 0) throw new Error(`${pending} ${pending === 1 ? 'série ainda está pendente' : 'séries ainda estão pendentes'}. Conecte-se e tente novamente antes de concluir.`);
      const authoritative = await inviteApi.session(authToken, session.sessionId);
      if (authoritative.exercises.some((exercise) => exercise.completedSets < exercise.sets)) throw new Error('A sessão ainda não está completa no servidor.');
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

  return <Screen style={styles.page}>
    <TopBar eyebrow="TREINO EM ANDAMENTO" title={session.workoutName} onBack={() => router.back()} />
    <View style={styles.progressHeader}><View style={styles.progressCopy}><Text style={styles.progressTitle}>Progresso da sessão</Text><Text style={styles.copy}>{progress.completedSets} de {progress.totalSets} séries registradas</Text></View><Text style={styles.progressValue}>{progress.percentage}%</Text></View>
    <View accessibilityRole="progressbar" accessibilityValue={{ min: 0, max: 100, now: progress.percentage }} style={styles.progressTrack}><View style={[styles.progressFill, { width: `${progress.percentage}%` }]} /></View>
    {isOfflineSnapshot ? <Card style={styles.offlineCard}><Text accessibilityRole="alert" style={styles.offlineTitle}>Você está sem conexão</Text><Text style={styles.copy}>Exibindo a sessão salva neste dispositivo. Séries novas ficam pendentes para sincronização.</Text></Card> : null}
    <Text style={styles.intro}>A ordem do seu personal é uma sugestão. Abra qualquer exercício pendente e adapte a sessão quando precisar.</Text>
    {exercises.length === 0 ? <EmptyState status="SESSÃO SEM EXERCÍCIOS" symbol="●" title="Não há uma sequência para executar." message="Volte aos treinos e escolha outra sessão enquanto seu personal revisa esta prescrição." actionLabel="Voltar aos treinos" onAction={() => router.replace('/student/training')} /> : exercises.map((exercise) => <OverviewExercise key={exercise.id} session={session} exercise={exercise} onOpen={() => router.push({ pathname: '/student/exercise/[sessionId]/[exerciseId]', params: { sessionId: session.sessionId, exerciseId: exercise.id } })} />)}
    {current ? <Button onPress={() => router.push({ pathname: '/student/exercise/[sessionId]/[exerciseId]', params: { sessionId: session.sessionId, exerciseId: current.id } })}>{progress.completedSets > 0 ? 'Continuar próximo sugerido' : 'Começar próximo sugerido'}</Button> : null}
    {allComplete ? <Button loading={complete.isPending} onPress={() => { setCompletionError(undefined); complete.mutate(); }}>Concluir treino</Button> : null}
    {completionError ? <Text accessibilityRole="alert" style={styles.error}>{completionError}</Text> : null}
  </Screen>;
}

function OverviewExercise({ session, exercise, onOpen }: { session: StudentSession; exercise: StudentSession['exercises'][number]; onOpen: () => void }) {
  const state = exerciseProgressState(session, exercise);
  const source = exerciseMediaSource(exercise.imageRef);
  const stateLabel = state === 'completed' ? 'Concluído' : state === 'current' ? 'Próximo sugerido' : 'Pendente';
  return <Card style={[styles.card, state === 'current' && styles.currentCard]}>
    <View style={styles.exerciseRow}>{source ? <Image source={source} style={styles.thumbnail} resizeMode="cover" accessibilityLabel={`Imagem do exercício ${exercise.name}`} /> : null}<View style={styles.exerciseIdentity}><Text style={styles.sequence}>{exercise.sequence}. {exercise.name}</Text>{(exercise.primaryMuscleGroup || exercise.equipment) ? <Text style={styles.context}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text> : null}</View><Tag tone={state === 'completed' ? 'success' : state === 'current' ? 'primary' : 'neutral'}>{stateLabel}</Tag></View>
    <View style={styles.prescription}><Prescription label="Séries" value={`${Math.min(exercise.completedSets, exercise.sets)}/${exercise.sets}`} /><Prescription label="Repetições" value={`${exercise.repetitionsMin}–${exercise.repetitionsMax}`} /><Prescription label="Descanso" value={`${exercise.restSeconds}s`} /></View>
    {exercise.instructions ? <Text numberOfLines={2} style={styles.copy}>{exercise.instructions}</Text> : null}
    {exercise.notes ? <Text numberOfLines={2} style={styles.note}>Personal: {exercise.notes}</Text> : null}
    {state !== 'completed' ? <Button variant={state === 'current' ? 'primary' : 'secondary'} onPress={onOpen} accessibilityLabel={`Começar exercício ${exercise.name}`} accessibilityHint="Abre este exercício para registrar a próxima série">Começar exercício</Button> : <Text accessibilityLabel={`${exercise.name} concluído`} style={styles.completedHint}>Todas as séries deste exercício foram registradas.</Text>}
  </Card>;
}

function Prescription({ label, value }: { label: string; value: string }) { return <View style={styles.prescriptionItem}><Text style={styles.prescriptionLabel}>{label}</Text><Text style={styles.prescriptionValue}>{value}</Text></View>; }
async function synchronizePendingSets(token: string, studentId: string) { return syncPendingSets(studentId, async (item) => { await inviteApi.completeSet(token, item.sessionId, item.exerciseId, item.input); }); }
async function hydratePendingProgress(workout: StudentSession, studentId: string): Promise<StudentSession> { const pending = await pendingSetDetails(workout.sessionId, studentId); return withPendingProgress(workout, pending); }

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, intro: { ...typography.bodyLG, color: colors.titaniumLight, lineHeight: 24 }, progressHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.md }, progressCopy: { gap: spacing.xxs, flex: 1 }, progressTitle: { ...typography.headingMD, color: colors.textPrimary }, progressValue: { ...typography.headingLG, color: colors.primary }, progressTrack: { height: 8, borderRadius: 8, backgroundColor: colors.surfaceElevated, overflow: 'hidden' }, progressFill: { height: '100%', backgroundColor: colors.primary, borderRadius: 8 }, card: { gap: spacing.md, overflow: 'hidden' }, currentCard: { borderColor: colors.primary }, exerciseRow: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.sm }, thumbnail: { width: 64, height: 64, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, exerciseIdentity: { flex: 1, gap: spacing.xxs }, sequence: { ...typography.headingMD, color: colors.textPrimary }, context: { ...typography.caption, color: colors.titanium }, note: { ...typography.caption, color: colors.titaniumLight }, prescription: { flexDirection: 'row', gap: spacing.xs }, prescriptionItem: { flex: 1, gap: spacing.xxs, padding: spacing.sm, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, prescriptionLabel: { ...typography.caption, color: colors.textMuted }, prescriptionValue: { ...typography.bodyLG, color: colors.textPrimary }, completedHint: { ...typography.caption, color: colors.textMuted }, offlineCard: { gap: spacing.xs, borderColor: colors.warning }, offlineTitle: { ...typography.headingMD, color: colors.warning }, error: { ...typography.caption, color: colors.danger } });
