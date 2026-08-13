import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { Image, StyleSheet, Text, TextInput, View } from 'react-native';
import { ApiError } from '@/src/api/shared-http';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentSession } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { cacheWorkout, cachedSession, pendingSetNumbers, queueSet, updateCachedExerciseProgress } from '@/src/features/student/offline/training-db';
import { currentExercise, orderedExercises, useStudentTrainingSessionStore, withPendingProgress } from '@/src/features/student/training/session-state';
import { parseActualSetPerformance } from '@/src/features/student/training/set-performance';
import { exerciseMediaSource } from '@/src/shared/training/exercise-media';

export function StudentTrainingExerciseScreen() {
  const { sessionId, exerciseId } = useLocalSearchParams<{ sessionId: string; exerciseId: string }>();
  const authSession = useInviteSessionStore((state) => state.session);
  const session = useStudentTrainingSessionStore((state) => state.session);
  const ownerStudentId = useStudentTrainingSessionStore((state) => state.studentId);
  const isOfflineSnapshot = useStudentTrainingSessionStore((state) => state.isOfflineSnapshot);
  const setSession = useStudentTrainingSessionStore((state) => state.setSession);
  const [loading, setLoading] = useState(!session || session.sessionId !== sessionId || ownerStudentId !== authSession?.studentId);
  const [error, setError] = useState<string>();
  const [isNavigatingToRest, setIsNavigatingToRest] = useState(false);

  useEffect(() => {
    if (session?.sessionId === sessionId && ownerStudentId === authSession?.studentId) return;
    if (!sessionId || !authSession) return;
    void (async () => {
      try {
        const server = await inviteApi.session(authSession.accessToken, sessionId);
        const pending = await pendingSetNumbers(server.sessionId, authSession.studentId);
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
        const pending = await pendingSetNumbers(cached.sessionId, authSession.studentId);
        setSession(withPendingProgress(cached, pending), true, authSession.studentId);
      } catch { setError('Não foi possível recuperar a sessão salva.'); }
    })().finally(() => setLoading(false));
  }, [session?.sessionId, ownerStudentId, sessionId, authSession, setSession]);

  const requested = session?.exercises.find((exercise) => exercise.id === exerciseId);
  const exercise = session ? currentExercise(session) : undefined;
  const shouldOpenCurrentExercise = Boolean(requested && exercise && requested.id !== exercise.id);

  useEffect(() => {
    if (!authSession) router.replace('/login');
  }, [authSession]);

  useEffect(() => {
    if (loading || error || isNavigatingToRest || session?.sessionId !== sessionId || !shouldOpenCurrentExercise || !exercise) return;
    router.replace({ pathname: '/student/exercise/[sessionId]/[exerciseId]', params: { sessionId: session.sessionId, exerciseId: exercise.id } });
  }, [loading, error, isNavigatingToRest, session?.sessionId, sessionId, shouldOpenCurrentExercise, exercise?.id]);

  if (!authSession) return null;
  if (loading) return <LoadingView message="Abrindo seu exercício…" />;
  if (error || !session || session.sessionId !== sessionId) return <ErrorView message={error ?? 'Sessão indisponível.'} onRetry={() => router.back()} />;
  if (isNavigatingToRest) return <LoadingView message="Série registrada…" />;

  const ordered = orderedExercises(session);
  if (!requested || !exercise) return <CompletedExercise session={session} />;
  if (shouldOpenCurrentExercise) return <LoadingView message="Abrindo o exercício atual…" />;
  return <FocusedExercise session={session} exercise={exercise} authToken={authSession.accessToken} studentId={authSession.studentId} isOfflineSnapshot={isOfflineSnapshot} position={ordered.findIndex((item) => item.id === exercise.id) + 1} onRestTransition={() => setIsNavigatingToRest(true)} />;
}

function FocusedExercise({ session, exercise, authToken, studentId, isOfflineSnapshot, position, onRestTransition }: { session: StudentSession; exercise: StudentSession['exercises'][number]; authToken: string; studentId: string; isOfflineSnapshot: boolean; position: number; onRestTransition: () => void }) {
  const updateExerciseProgress = useStudentTrainingSessionStore((state) => state.updateExerciseProgress);
  const setOfflineSnapshot = useStudentTrainingSessionStore((state) => state.setOfflineSnapshot);
  const currentSessionPerformance = exercise.performances?.at(-1);
  const previousPerformance = currentSessionPerformance ?? exercise.previousPerformance;
  const [weight, setWeight] = useState(() => formatWeight(previousPerformance?.weightKg));
  const [repetitions, setRepetitions] = useState(() => previousPerformance ? String(previousPerformance.repetitions) : '');
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ tone: 'error' | 'offline'; text: string }>();
  const source = exerciseMediaSource(exercise.imageRef);
  const setNumber = exercise.completedSets + 1;
  useEffect(() => {
    const previous = exercise.performances?.at(-1) ?? exercise.previousPerformance;
    setWeight(formatWeight(previous?.weightKg));
    setRepetitions(previous ? String(previous.repetitions) : '');
  }, [exercise.id, setNumber, exercise.performances, exercise.previousPerformance]);
  const save = async () => {
    const parsed = parseActualSetPerformance(weight, repetitions);
    if (!parsed.success) { setMessage({ tone: 'error', text: parsed.message }); return; }
    const input = { clientOperationId: `${session.sessionId}-${exercise.id}-${setNumber}`, setNumber, ...parsed.value };
    setSaving(true); setMessage(undefined);
    try {
      const response = await inviteApi.completeSet(authToken, session.sessionId, exercise.id, input);
      const completedSets = Math.max(setNumber, response.completedSets);
      const performance = { setNumber, ...parsed.value, completedAt: new Date().toISOString() };
      onRestTransition();
      updateExerciseProgress(exercise.id, completedSets, performance);
      await updateCachedExerciseProgress(session.sessionId, studentId, exercise.id, completedSets, performance).catch(() => undefined);
      router.replace({ pathname: '/student/rest/[sessionId]/[exerciseId]', params: { sessionId: session.sessionId, exerciseId: exercise.id } });
    } catch (saveError) {
      if (!(saveError instanceof ApiError) || saveError.status !== 0) { setMessage({ tone: 'error', text: saveError instanceof Error ? saveError.message : 'Não foi possível salvar esta série.' }); setSaving(false); return; }
      try {
        await queueSet({ studentId, sessionId: session.sessionId, exerciseId: exercise.id, input });
        const performance = { setNumber, ...parsed.value, completedAt: new Date().toISOString() };
        onRestTransition();
        updateExerciseProgress(exercise.id, setNumber, performance);
        setOfflineSnapshot(true);
        try { await updateCachedExerciseProgress(session.sessionId, studentId, exercise.id, setNumber, performance); } catch { /* Pending sets hydrate from local_sets. */ }
        setMessage({ tone: 'offline', text: 'Série salva neste dispositivo e pendente para sincronização.' });
        router.replace({ pathname: '/student/rest/[sessionId]/[exerciseId]', params: { sessionId: session.sessionId, exerciseId: exercise.id } });
      } catch { setMessage({ tone: 'error', text: 'Não foi possível salvar a série no dispositivo.' }); }
    }
    setSaving(false);
  };

  return <Screen style={styles.page}>
    <TopBar eyebrow={`EXERCÍCIO ${position} DE ${session.exercises.length}`} title={exercise.name} onBack={() => router.back()} />
    {isOfflineSnapshot ? <Card style={styles.offlineCard}><Text style={styles.offlineTitle}>Modo offline</Text><Text style={styles.copy}>Sua série será sincronizada quando a conexão voltar.</Text></Card> : null}
    {source ? <Image source={source} style={styles.exerciseImage} resizeMode="cover" accessibilityLabel={`Imagem do exercício ${exercise.name}`} /> : null}
    <View style={styles.contextRow}><Text style={styles.context}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text><Text style={styles.setProgress}>{Math.min(exercise.completedSets, exercise.sets)}/{exercise.sets} séries</Text></View>
    <View style={styles.prescription}><Prescription label="Repetições" value={`${exercise.repetitionsMin}–${exercise.repetitionsMax}`} /><Prescription label="Descanso" value={`${exercise.restSeconds}s`} /></View>
    {exercise.instructions ? <Detail label="INSTRUÇÕES" value={exercise.instructions} /> : null}
    {exercise.notes ? <Detail label="OBSERVAÇÕES DO PERSONAL" value={exercise.notes} /> : null}
    <Card style={styles.entryCard}><Text style={styles.eyebrow}>PRÓXIMA SÉRIE</Text><Text style={styles.nextSet}>Série {setNumber} de {exercise.sets}</Text>{previousPerformance ? <Text style={styles.prefillHint}>{currentSessionPerformance ? 'Mantivemos os valores da série anterior. Ajuste apenas se precisar.' : 'Usamos sua última carga e repetições neste exercício como ponto de partida.'}</Text> : null}<View style={styles.row}><View style={styles.field}><Text style={styles.inputLabel}>Carga real (kg)</Text><TextInput value={weight} onChangeText={setWeight} keyboardType="decimal-pad" placeholder="Ex.: 42,5" placeholderTextColor={colors.textMuted} accessibilityLabel={`Carga realizada na série ${setNumber} de ${exercise.name}`} style={styles.input} /></View><View style={styles.field}><Text style={styles.inputLabel}>Reps reais</Text><TextInput value={repetitions} onChangeText={setRepetitions} keyboardType="number-pad" placeholder={`${exercise.repetitionsMin}–${exercise.repetitionsMax}`} placeholderTextColor={colors.textMuted} accessibilityLabel={`Repetições realizadas na série ${setNumber} de ${exercise.name}`} style={styles.input} /></View></View><Button loading={saving} onPress={() => void save()}>Salvar série realizada</Button>{message ? <Text accessibilityRole="alert" style={message.tone === 'error' ? styles.error : styles.offlineMessage}>{message.text}</Text> : null}</Card>
  </Screen>;
}

function formatWeight(weightKg: number | undefined) { return weightKg === undefined ? '' : String(weightKg).replace('.', ','); }

function CompletedExercise({ session }: { session: StudentSession }) { return <Screen style={styles.page}><TopBar eyebrow="EXERCÍCIO CONCLUÍDO" title={session.workoutName} onBack={() => router.back()} /><Card><Text style={styles.copy}>Este exercício já teve todas as séries registradas.</Text><Button onPress={() => router.replace({ pathname: '/student/training/[id]', params: { id: session.workoutId } })}>Voltar à visão geral</Button></Card></Screen>; }
function Prescription({ label, value }: { label: string; value: string }) { return <View style={styles.prescriptionItem}><Text style={styles.prescriptionLabel}>{label}</Text><Text style={styles.prescriptionValue}>{value}</Text></View>; }
function Detail({ label, value }: { label: string; value: string }) { return <View style={styles.detail}><Text style={styles.detailLabel}>{label}</Text><Text style={styles.copy}>{value}</Text></View>; }

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, exerciseImage: { width: '100%', maxWidth: 320, height: 160, alignSelf: 'center', borderRadius: radius.md, backgroundColor: colors.surfaceElevated }, contextRow: { flexDirection: 'row', justifyContent: 'space-between', gap: spacing.sm }, context: { ...typography.caption, color: colors.titanium }, setProgress: { ...typography.headingMD, color: colors.primary }, prescription: { flexDirection: 'row', gap: spacing.xs }, prescriptionItem: { flex: 1, gap: spacing.xxs, padding: spacing.sm, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, prescriptionLabel: { ...typography.caption, color: colors.textMuted }, prescriptionValue: { ...typography.bodyLG, color: colors.textPrimary }, detail: { gap: spacing.xs, paddingTop: spacing.xs, borderTopWidth: 1, borderTopColor: colors.border }, detailLabel: { ...typography.caption, color: colors.primary, letterSpacing: .6 }, entryCard: { gap: spacing.md, borderColor: colors.primary }, nextSet: { ...typography.headingMD, color: colors.textPrimary }, prefillHint: { ...typography.caption, color: colors.titanium }, row: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.sm }, field: { flex: 1, gap: spacing.xs }, inputLabel: { ...typography.caption, color: colors.textSecondary }, input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 48, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, paddingHorizontal: spacing.md, paddingVertical: spacing.sm, backgroundColor: colors.surfaceElevated }, offlineCard: { gap: spacing.xs, borderColor: colors.warning }, offlineTitle: { ...typography.headingMD, color: colors.warning }, offlineMessage: { ...typography.caption, color: colors.warning }, error: { ...typography.caption, color: colors.danger } });
