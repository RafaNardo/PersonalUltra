import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useMemo, useState } from 'react';
import { AppState, Image, StyleSheet, Text, View } from 'react-native';
import { useMutation } from '@tanstack/react-query';
import { ApiError } from '@/src/api/shared-http';
import { Button, Card, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentSession } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { cacheWorkout, cachedWorkout, pendingSetCount, pendingSetNumbers, syncPendingSets } from '@/src/features/student/offline/training-db';
import { currentExercise, exerciseProgressState, orderedExercises, sessionProgress, useStudentTrainingSessionStore } from '@/src/features/student/training/session-state';
import { exerciseMediaSource } from '@/src/shared/training/exercise-media';

export function StudentTrainingSessionScreen() {
  const { id, start } = useLocalSearchParams<{ id: string; start?: string }>();
  const authSession = useInviteSessionStore((state) => state.session);
  const session = useStudentTrainingSessionStore((state) => state.session);
  const activeSession = session?.workoutId === id ? session : undefined;
  const isOfflineSnapshot = useStudentTrainingSessionStore((state) => state.isOfflineSnapshot);
  const setSession = useStudentTrainingSessionStore((state) => state.setSession);
  const [error, setError] = useState<string>();
  const [loadingSnapshot, setLoadingSnapshot] = useState(false);

  const startWorkout = useMutation({
    mutationFn: async () => {
      await synchronizePendingSets(authSession!.accessToken);
      return hydratePendingProgress(await inviteApi.startWorkout(authSession!.accessToken, id!));
    },
    onSuccess: async (started) => {
      setSession(started, false);
      try { await cacheWorkout(started); } catch { /* The API response remains usable if local storage fails. */ }
    },
    onError: async (startError: Error) => {
      if (startError instanceof ApiError && startError.status === 0) {
        try {
          const cached = await cachedWorkout<StudentSession>(id);
          if (cached) { setSession(await hydratePendingProgress(cached), true); setError(undefined); return; }
        } catch { /* Show the original connectivity error when no snapshot is available. */ }
      }
      setError(startError.message);
    },
  });

  const refresh = async () => {
    if (!authSession || !id || !activeSession) return;
    try {
      await synchronizePendingSets(authSession.accessToken);
      const refreshed = await hydratePendingProgress(await inviteApi.startWorkout(authSession.accessToken, id));
      setSession(refreshed, false);
      await cacheWorkout(refreshed);
    } catch { /* Keep the shared snapshot visible while offline. */ }
  };

  useEffect(() => {
    if (!authSession || !id || activeSession || start !== '1' || startWorkout.isPending || startWorkout.isError) return;
    startWorkout.mutate();
  }, [authSession, id, activeSession, start, startWorkout.isPending, startWorkout.isError]);

  useEffect(() => {
    if (!authSession) return;
    const retry = () => { void synchronizePendingSets(authSession.accessToken).catch(() => undefined); };
    const interval = setInterval(retry, 15_000);
    const subscription = AppState.addEventListener('change', (state) => { if (state === 'active' && !startWorkout.isPending) void refresh(); });
    return () => { clearInterval(interval); subscription.remove(); };
  }, [authSession, id, activeSession, startWorkout.isPending]);

  useEffect(() => {
    if (!authSession || !id || activeSession || start === '1' || loadingSnapshot) return;
    setLoadingSnapshot(true);
    void cachedWorkout<StudentSession>(id).then(async (cached) => {
      if (cached) setSession(await hydratePendingProgress(cached), true);
      else setError('Abra a prévia do treino e confirme o início para criar uma sessão.');
    }).catch(() => setError('Não foi possível recuperar a sessão salva neste dispositivo.')).finally(() => setLoadingSnapshot(false));
  }, [authSession, id, activeSession, start, loadingSnapshot]);

  if (!authSession) { router.replace('/login'); return null; }
  if (!activeSession && (startWorkout.isPending || loadingSnapshot)) return <LoadingView message="Preparando seu treino…" />;
  if (!activeSession && (startWorkout.isError || error)) return <ErrorView message={error ?? startWorkout.error?.message ?? 'Não foi possível abrir este treino.'} onRetry={() => { setError(undefined); startWorkout.reset(); if (start === '1') startWorkout.mutate(); else router.back(); }} />;
  if (!activeSession) return <LoadingView message="Preparando seu treino…" />;

  return <SessionOverview session={activeSession} isOfflineSnapshot={isOfflineSnapshot} authToken={authSession.accessToken} />;
}

function SessionOverview({ session, isOfflineSnapshot, authToken }: { session: StudentSession; isOfflineSnapshot: boolean; authToken: string }) {
  const exercises = useMemo(() => orderedExercises(session), [session]);
  const progress = sessionProgress(session);
  const current = currentExercise(session);
  const allComplete = Boolean(session.exercises.length) && !current;
  const [completionError, setCompletionError] = useState<string>();
  const clearSession = useStudentTrainingSessionStore((state) => state.clearSession);
  const complete = useMutation({
    mutationFn: async () => {
      const pending = await pendingSetCount(session.sessionId);
      if (pending > 0) throw new Error(`${pending} ${pending === 1 ? 'série ainda está pendente' : 'séries ainda estão pendentes'}. Conecte-se e tente novamente antes de concluir.`);
      return inviteApi.completeWorkout(authToken, session.sessionId);
    },
    onSuccess: () => {
      clearSession();
      router.replace('/student/training');
    },
    onError: (error: Error) => setCompletionError(error instanceof ApiError && error.status === 0 ? 'Conecte-se à internet para concluir o treino.' : error.message),
  });

  return <Screen style={styles.page}>
    <TopBar eyebrow="TREINO EM ANDAMENTO" title={session.workoutName} onBack={() => router.back()} />
    <View style={styles.progressHeader}><View style={styles.progressCopy}><Text style={styles.progressTitle}>Progresso da sessão</Text><Text style={styles.copy}>{progress.completedSets} de {progress.totalSets} séries registradas</Text></View><Text style={styles.progressValue}>{progress.percentage}%</Text></View>
    <View accessibilityRole="progressbar" accessibilityValue={{ min: 0, max: 100, now: progress.percentage }} style={styles.progressTrack}><View style={[styles.progressFill, { width: `${progress.percentage}%` }]} /></View>
    {isOfflineSnapshot ? <Card style={styles.offlineCard}><Text accessibilityRole="alert" style={styles.offlineTitle}>Você está sem conexão</Text><Text style={styles.copy}>Exibindo a sessão salva neste dispositivo. Séries novas ficam pendentes para sincronização.</Text></Card> : null}
    <Text style={styles.intro}>Acompanhe a sequência completa e abra somente o exercício atual para registrar a próxima série.</Text>
    {exercises.length === 0 ? <Card><Text style={styles.copy}>Este treino ainda não possui exercícios.</Text></Card> : exercises.map((exercise) => <OverviewExercise key={exercise.id} session={session} exercise={exercise} />)}
    {current ? <Button onPress={() => router.push({ pathname: '/student/exercise/[sessionId]/[exerciseId]', params: { sessionId: session.sessionId, exerciseId: current.id } })}>{progress.completedSets > 0 ? 'Continuar exercício atual' : 'Começar exercício atual'}</Button> : null}
    {allComplete ? <Button loading={complete.isPending} onPress={() => { setCompletionError(undefined); complete.mutate(); }}>Concluir treino</Button> : null}
    {completionError ? <Text accessibilityRole="alert" style={styles.error}>{completionError}</Text> : null}
  </Screen>;
}

function OverviewExercise({ session, exercise }: { session: StudentSession; exercise: StudentSession['exercises'][number] }) {
  const state = exerciseProgressState(session, exercise);
  const source = exerciseMediaSource(exercise.imageRef);
  const stateLabel = state === 'completed' ? 'Concluído' : state === 'current' ? 'Atual' : 'Pendente';
  return <Card style={[styles.card, state === 'current' && styles.currentCard]}>
    <View style={styles.exerciseRow}>{source ? <Image source={source} style={styles.thumbnail} resizeMode="cover" accessibilityLabel={`Imagem do exercício ${exercise.name}`} /> : null}<View style={styles.exerciseIdentity}><Text style={styles.sequence}>{exercise.sequence}. {exercise.name}</Text>{(exercise.primaryMuscleGroup || exercise.equipment) ? <Text style={styles.context}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text> : null}</View><Tag tone={state === 'completed' ? 'success' : state === 'current' ? 'primary' : 'neutral'}>{stateLabel}</Tag></View>
    <View style={styles.prescription}><Prescription label="Séries" value={`${Math.min(exercise.completedSets, exercise.sets)}/${exercise.sets}`} /><Prescription label="Repetições" value={`${exercise.repetitionsMin}–${exercise.repetitionsMax}`} /><Prescription label="Descanso" value={`${exercise.restSeconds}s`} /></View>
    {exercise.instructions ? <Text numberOfLines={2} style={styles.copy}>{exercise.instructions}</Text> : null}
    {exercise.notes ? <Text numberOfLines={2} style={styles.note}>Personal: {exercise.notes}</Text> : null}
  </Card>;
}

function Prescription({ label, value }: { label: string; value: string }) { return <View style={styles.prescriptionItem}><Text style={styles.prescriptionLabel}>{label}</Text><Text style={styles.prescriptionValue}>{value}</Text></View>; }
async function synchronizePendingSets(token: string) { return syncPendingSets(async (item) => { await inviteApi.completeSet(token, item.sessionId, item.exerciseId, item.input); }); }
async function hydratePendingProgress(workout: StudentSession): Promise<StudentSession> { const pending = await pendingSetNumbers(workout.sessionId); return { ...workout, exercises: workout.exercises.map((exercise) => ({ ...exercise, completedSets: Math.max(exercise.completedSets, pending[exercise.id] ?? 0) })) }; }

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, intro: { ...typography.bodyLG, color: colors.titaniumLight, lineHeight: 24 }, progressHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.md }, progressCopy: { gap: spacing.xxs, flex: 1 }, progressTitle: { ...typography.headingMD, color: colors.textPrimary }, progressValue: { ...typography.headingLG, color: colors.primary }, progressTrack: { height: 8, borderRadius: 8, backgroundColor: colors.surfaceElevated, overflow: 'hidden' }, progressFill: { height: '100%', backgroundColor: colors.primary, borderRadius: 8 }, card: { gap: spacing.md, overflow: 'hidden' }, currentCard: { borderColor: colors.primary }, exerciseRow: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.sm }, thumbnail: { width: 64, height: 64, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, exerciseIdentity: { flex: 1, gap: spacing.xxs }, sequence: { ...typography.headingMD, color: colors.textPrimary }, context: { ...typography.caption, color: colors.titanium }, note: { ...typography.caption, color: colors.titaniumLight }, prescription: { flexDirection: 'row', gap: spacing.xs }, prescriptionItem: { flex: 1, gap: spacing.xxs, padding: spacing.sm, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, prescriptionLabel: { ...typography.caption, color: colors.textMuted }, prescriptionValue: { ...typography.bodyLG, color: colors.textPrimary }, offlineCard: { gap: spacing.xs, borderColor: colors.warning }, offlineTitle: { ...typography.headingMD, color: colors.warning }, error: { ...typography.caption, color: colors.danger } });
