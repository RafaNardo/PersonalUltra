import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { AppState, Image, StyleSheet, Text, TextInput, View } from 'react-native';
import { useMutation } from '@tanstack/react-query';
import { ApiError } from '@/src/api/shared-http';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentSession } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { cacheWorkout, cachedWorkout, pendingSetCount, pendingSetNumbers, queueSet, syncPendingSets, updateCachedExerciseProgress } from '@/src/features/student/offline/training-db';
import { parseActualSetPerformance } from '@/src/features/student/training/set-performance';
import { exerciseMediaSource } from '@/src/shared/training/exercise-media';

export default function StudentTrainingSessionScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const session = useInviteSessionStore((state) => state.session);
  const [workout, setWorkout] = useState<StudentSession>();
  const [error, setError] = useState<string>();
  const [isOfflineSnapshot, setIsOfflineSnapshot] = useState(false);
  const [completionError, setCompletionError] = useState<string>();

  const start = useMutation({
    mutationFn: async () => {
      await synchronizePendingSets(session!.accessToken);
      return hydratePendingProgress(await inviteApi.startWorkout(session!.accessToken, id!));
    },
    onSuccess: async (startedWorkout) => {
      setWorkout(startedWorkout);
      setIsOfflineSnapshot(false);
      setError(undefined);
      try { await cacheWorkout(startedWorkout); } catch { /* API data remains usable if local cache storage fails. */ }
    },
    onError: async (startError: Error) => {
      if (startError instanceof ApiError && startError.status === 0) {
        try {
          const cached = await cachedWorkout<StudentSession>(id);
          if (cached) {
            setWorkout(await hydratePendingProgress(cached));
            setIsOfflineSnapshot(true);
            setError(undefined);
            return;
          }
        } catch { /* Show the original connectivity error if the local snapshot cannot be read. */ }
      }
      setError(startError.message);
    },
  });

  const complete = useMutation({
    mutationFn: async () => {
      await synchronizePendingSets(session!.accessToken);
      const remaining = await pendingSetCount(workout!.sessionId);
      if (remaining > 0) throw new Error(`${remaining} ${remaining === 1 ? 'série ainda está pendente' : 'séries ainda estão pendentes'}. Conecte-se e tente novamente antes de concluir.`);
      return inviteApi.completeWorkout(session!.accessToken, workout!.sessionId);
    },
    onSuccess: () => router.replace('/student-training'),
    onError: (completeError: Error) => setCompletionError(completeError instanceof ApiError && completeError.status === 0
      ? 'Conecte-se à internet para concluir o treino.'
      : completeError.message),
  });

  useEffect(() => {
    if (session && id && !workout && !start.isPending && !start.isError) start.mutate();
  }, [id, session, workout, start.isPending, start.isError]);

  useEffect(() => {
    if (!session) return;
    const retry = () => { void synchronizePendingSets(session.accessToken).catch(() => undefined); };
    const interval = setInterval(retry, 15_000);
    const subscription = AppState.addEventListener('change', (state) => {
      if (state !== 'active' || start.isPending) return;
      void (async () => {
        try {
          await synchronizePendingSets(session.accessToken);
          if (!id) return;
          const refreshed = await hydratePendingProgress(await inviteApi.startWorkout(session.accessToken, id));
          setWorkout(refreshed);
          setIsOfflineSnapshot(false);
          await cacheWorkout(refreshed);
        } catch { /* Keep the visible snapshot and pending progress while offline. */ }
      })();
    });
    return () => { clearInterval(interval); subscription.remove(); };
  }, [id, session, start.isPending]);

  if (!session) {
    router.replace('/login');
    return null;
  }
  if (!workout && start.isPending) return <LoadingView message="Preparando seu treino…" />;
  if (!workout && (start.isError || error)) return <ErrorView message={error ?? start.error?.message ?? 'Não foi possível iniciar este treino.'} onRetry={() => { setError(undefined); start.reset(); start.mutate(); }} />;
  if (!workout) return <LoadingView message="Preparando seu treino…" />;

  return <Screen style={styles.page}>
    <TopBar eyebrow="TREINO EM ANDAMENTO" title={workout.workoutName} onBack={() => router.back()} />
    <Text style={styles.copy}>Registre a carga e as repetições realizadas em cada série.</Text>
    {isOfflineSnapshot ? <Card style={styles.offlineCard}><Text accessibilityRole="alert" style={styles.offlineTitle}>Você está sem conexão</Text><Text style={styles.copy}>Exibindo o treino salvo neste dispositivo. As séries ficam pendentes para a próxima sincronização.</Text></Card> : null}
    {workout.exercises.length === 0 ? <Card><Text style={styles.copy}>Este treino ainda não possui exercícios.</Text></Card> : workout.exercises.map((exercise) => <Exercise key={exercise.id} session={session.accessToken} workout={workout} exercise={exercise} />)}
    {completionError ? <Text accessibilityRole="alert" style={styles.error}>{completionError}</Text> : null}
    <Button disabled={workout.exercises.length === 0} loading={complete.isPending} onPress={() => { setCompletionError(undefined); complete.mutate(); }}>Concluir treino</Button>
  </Screen>;
}

function Exercise({ session, workout, exercise }: { session: string; workout: StudentSession; exercise: StudentSession['exercises'][number] }) {
  const [sets, setSets] = useState(exercise.completedSets);
  const [weight, setWeight] = useState('');
  const [repetitions, setRepetitions] = useState('');
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ tone: 'error' | 'offline'; text: string }>();
  const source = exerciseMediaSource(exercise.imageRef);
  const finished = sets >= exercise.sets;

  const save = async () => {
    const parsed = parseActualSetPerformance(weight, repetitions);
    if (!parsed.success) {
      setMessage({ tone: 'error', text: parsed.message });
      return;
    }

    const input = {
      clientOperationId: `${workout.sessionId}-${exercise.id}-${sets + 1}`,
      setNumber: sets + 1,
      ...parsed.value,
    };
    setSaving(true);
    setMessage(undefined);
    try {
      const response = await inviteApi.completeSet(session, workout.sessionId, exercise.id, input);
      try { await updateCachedExerciseProgress(workout.sessionId, exercise.id, response.completedSets); } catch { /* The server-confirmed set remains authoritative. */ }
      setMessage(undefined);
    } catch (saveError) {
      if (!(saveError instanceof ApiError) || saveError.status !== 0) {
        setMessage({ tone: 'error', text: saveError instanceof Error ? saveError.message : 'Não foi possível salvar esta série.' });
        setSaving(false);
        return;
      }
      try {
        await queueSet({ sessionId: workout.sessionId, exerciseId: exercise.id, input });
        try { await updateCachedExerciseProgress(workout.sessionId, exercise.id, sets + 1); } catch { /* Pending sets still hydrate from local_sets. */ }
        setMessage({ tone: 'offline', text: 'Série salva no dispositivo e pendente para a próxima sincronização.' });
      } catch {
        setMessage({ tone: 'error', text: 'Não foi possível salvar a série no dispositivo.' });
        setSaving(false);
        return;
      }
    }
    setSets((value) => value + 1);
    setWeight('');
    setRepetitions('');
    setSaving(false);
  };

  return <Card style={styles.card}>
    {source ? <Image source={source} style={styles.exerciseImage} resizeMode="cover" accessibilityLabel={`Imagem do exercício ${exercise.name}`} /> : null}
    <View style={styles.exerciseHeader}>
      <View style={styles.exerciseIdentity}>
        <Text style={styles.title}>{exercise.sequence}. {exercise.name}</Text>
        {(exercise.primaryMuscleGroup || exercise.equipment) ? <Text style={styles.context}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text> : null}
      </View>
      <Text style={styles.setProgress}>{Math.min(sets, exercise.sets)}/{exercise.sets}</Text>
    </View>
    <View style={styles.prescription}>
      <Prescription label="Séries" value={String(exercise.sets)} />
      <Prescription label="Repetições" value={`${exercise.repetitionsMin}–${exercise.repetitionsMax}`} />
      <Prescription label="Descanso" value={`${exercise.restSeconds}s`} />
    </View>
    {exercise.instructions ? <View style={styles.detail}><Text style={styles.detailLabel}>INSTRUÇÕES</Text><Text style={styles.copy}>{exercise.instructions}</Text></View> : null}
    {exercise.notes ? <View style={styles.detail}><Text style={styles.detailLabel}>OBSERVAÇÕES DO PERSONAL</Text><Text style={styles.copy}>{exercise.notes}</Text></View> : null}
    {finished ? <Text accessibilityRole="text" style={styles.finished}>Exercício concluído</Text> : <>
      <Text style={styles.nextSet}>Série {sets + 1} de {exercise.sets}</Text>
      <View style={styles.row}>
        <View style={styles.field}>
          <Text style={styles.inputLabel}>Carga real (kg)</Text>
          <TextInput value={weight} onChangeText={setWeight} keyboardType="decimal-pad" placeholder="Ex.: 42,5" placeholderTextColor={colors.textMuted} accessibilityLabel={`Carga realizada na série ${sets + 1} de ${exercise.name}`} style={styles.input} />
        </View>
        <View style={styles.field}>
          <Text style={styles.inputLabel}>Reps reais</Text>
          <TextInput value={repetitions} onChangeText={setRepetitions} keyboardType="number-pad" placeholder={`${exercise.repetitionsMin}–${exercise.repetitionsMax}`} placeholderTextColor={colors.textMuted} accessibilityLabel={`Repetições realizadas na série ${sets + 1} de ${exercise.name}`} style={styles.input} />
        </View>
      </View>
      <Button variant="secondary" loading={saving} onPress={() => void save()}>Salvar série realizada</Button>
    </>}
    {message ? <Text accessibilityRole="alert" style={message.tone === 'error' ? styles.error : styles.offlineMessage}>{message.text}</Text> : null}
  </Card>;
}

function Prescription({ label, value }: { label: string; value: string }) {
  return <View style={styles.prescriptionItem}><Text style={styles.prescriptionLabel}>{label}</Text><Text style={styles.prescriptionValue}>{value}</Text></View>;
}

async function synchronizePendingSets(token: string) {
  return syncPendingSets(async (item) => {
    await inviteApi.completeSet(token, item.sessionId, item.exerciseId, item.input);
  });
}

async function hydratePendingProgress(workout: StudentSession): Promise<StudentSession> {
  const pending = await pendingSetNumbers(workout.sessionId);
  return {
    ...workout,
    exercises: workout.exercises.map((exercise) => ({ ...exercise, completedSets: Math.max(exercise.completedSets, pending[exercise.id] ?? 0) })),
  };
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.lg },
  copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
  card: { gap: spacing.md, overflow: 'hidden' },
  exerciseImage: { width: '100%', aspectRatio: 1.7, borderRadius: radius.md, backgroundColor: colors.surfaceElevated },
  exerciseHeader: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.sm },
  exerciseIdentity: { flex: 1, gap: spacing.xxs },
  title: { ...typography.headingMD, color: colors.textPrimary },
  context: { ...typography.caption, color: colors.titanium },
  setProgress: { ...typography.headingMD, color: colors.primary },
  prescription: { flexDirection: 'row', gap: spacing.xs },
  prescriptionItem: { flex: 1, gap: spacing.xxs, padding: spacing.sm, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated },
  prescriptionLabel: { ...typography.caption, color: colors.textMuted },
  prescriptionValue: { ...typography.bodyLG, color: colors.textPrimary },
  detail: { gap: spacing.xs, paddingTop: spacing.xs, borderTopWidth: 1, borderTopColor: colors.border },
  detailLabel: { ...typography.caption, color: colors.primary, letterSpacing: .6 },
  nextSet: { ...typography.caption, color: colors.titaniumLight },
  row: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.sm },
  field: { flex: 1, gap: spacing.xs },
  inputLabel: { ...typography.caption, color: colors.textSecondary },
  input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 48, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, paddingHorizontal: spacing.md, paddingVertical: spacing.sm, backgroundColor: colors.surfaceElevated },
  finished: { ...typography.bodyMD, color: colors.success },
  error: { ...typography.caption, color: colors.danger },
  offlineCard: { gap: spacing.xs, borderColor: colors.warning },
  offlineTitle: { ...typography.headingMD, color: colors.warning },
  offlineMessage: { ...typography.caption, color: colors.warning },
});
