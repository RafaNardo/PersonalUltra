import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Redirect, router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { Alert, StyleSheet, Text, TextInput, View } from 'react-native';
import { ApiError } from '@/src/api/shared-http';
import { Button, Card, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentSession, type StudentSessionExercise, type StudentSetPerformance } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { cacheWorkout, cachedSession, clearCachedSession, pendingSetCount, pendingSetDetails, queueSet, syncPendingSets, updateCachedExerciseProgress } from '@/src/features/student/offline/training-db';
import { orderedExercises, useStudentTrainingSessionStore, withPendingProgress } from '@/src/features/student/training/session-state';
import { parseActualSetPerformance } from '@/src/features/student/training/set-performance';
import { ExerciseImage } from '@/src/shared/training/exercise-image';

export function StudentTrainingExerciseScreen() {
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
    void hydrateSession(sessionId, authSession.accessToken, authSession.studentId, setSession).catch((loadError) => setError(loadError instanceof Error ? loadError.message : 'Não foi possível recuperar a sessão.')).finally(() => setLoading(false));
  }, [session?.sessionId, ownerStudentId, sessionId, authSession, setSession]);
  if (!authSession) return <Redirect href="/login" />;
  if (loading) return <LoadingView message="Abrindo seu exercício…" />;
  if (error || !session || session.sessionId !== sessionId) return <ErrorView message={error ?? 'Sessão indisponível.'} onRetry={() => router.back()} />;
  const exercise = session.exercises.find((item) => item.id === exerciseId);
  if (!exercise) return <ErrorView message="Este exercício não pertence à sessão atual." onRetry={() => router.replace({ pathname: '/student/training/[id]', params: { id: session.workoutId } })} />;
  const position = orderedExercises(session).findIndex((item) => item.id === exercise.id) + 1;
  return <FocusedExercise session={session} exercise={exercise} authToken={authSession.accessToken} studentId={authSession.studentId} isOfflineSnapshot={isOfflineSnapshot} position={position} />;
}

function FocusedExercise({ session, exercise, authToken, studentId, isOfflineSnapshot, position }: { session: StudentSession; exercise: StudentSessionExercise; authToken: string; studentId: string; isOfflineSnapshot: boolean; position: number }) {
  const updateExerciseProgress = useStudentTrainingSessionStore((state) => state.updateExerciseProgress);
  const setOfflineSnapshot = useStudentTrainingSessionStore((state) => state.setOfflineSnapshot);
  const setSession = useStudentTrainingSessionStore((state) => state.setSession);
  const clearSession = useStudentTrainingSessionStore((state) => state.clearSession);
  const queryClient = useQueryClient();
  const currentPerformance = exercise.performances?.at(-1);
  const previousPerformance = currentPerformance ?? exercise.previousPerformance;
  const [weight, setWeight] = useState(() => formatWeight(previousPerformance?.weightKg));
  const [repetitions, setRepetitions] = useState(() => previousPerformance?.repetitions ? String(previousPerformance.repetitions) : '');
  const [durationMinutes, setDurationMinutes] = useState(() => formatMinutes(previousPerformance?.durationSeconds ?? exercise.targetDurationSeconds));
  const [phase, setPhase] = useState<'entry' | 'rest'>(exercise.isCompleted ? 'rest' : 'entry');
  const [targetAt, setTargetAt] = useState<number>();
  const [clock, setClock] = useState(Date.now());
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ tone: 'error' | 'offline'; text: string }>();
  const setNumber = exercise.completedSets + 1;
  const pendingExercises = orderedExercises(session).filter((item) => !item.isCompleted);
  const sessionComplete = pendingExercises.length === 0;
  const remaining = phase === 'rest' && !exercise.isCompleted ? Math.max(0, Math.ceil(((targetAt ?? Date.now()) - clock) / 1000)) : 0;

  useEffect(() => {
    if (phase !== 'rest' || exercise.isCompleted) return;
    const interval = setInterval(() => setClock(Date.now()), 500);
    return () => clearInterval(interval);
  }, [phase, exercise.isCompleted]);
  useEffect(() => {
    if (phase !== 'entry') return;
    const previous = exercise.performances?.at(-1) ?? exercise.previousPerformance;
    setWeight(formatWeight(previous?.weightKg));
    setRepetitions(previous?.repetitions ? String(previous.repetitions) : '');
    setDurationMinutes(formatMinutes(previous?.durationSeconds ?? exercise.targetDurationSeconds));
  }, [exercise.id, setNumber, phase, exercise.performances, exercise.previousPerformance, exercise.targetDurationSeconds]);

  const finish = useMutation({
    mutationFn: async () => {
      const sync = await synchronizePendingSets(authToken, studentId);
      if (sync.failed > 0 || await pendingSetCount(session.sessionId, studentId) > 0) throw new Error('Conecte-se e aguarde a sincronização antes de concluir.');
      await inviteApi.completeWorkout(authToken, session.sessionId);
      await clearCachedSession(session.sessionId, studentId);
    },
    onSuccess: async () => {
      clearSession();
      await queryClient.invalidateQueries({ queryKey: ['student', 'training'] });
      router.replace({ pathname: '/student/training/summary/[sessionId]', params: { sessionId: session.sessionId } });
    },
  });

  const save = async () => {
    const parsed = exercise.trackingMode === 'Duration' ? parseDuration(durationMinutes) : parseActualSetPerformance(weight, repetitions);
    if (!parsed.success) { setMessage({ tone: 'error', text: parsed.message }); return; }
    const input = { clientOperationId: `${session.sessionId}-${exercise.id}-${setNumber}`, setNumber, ...parsed.value };
    setSaving(true); setMessage(undefined);
    try {
      const sync = await synchronizePendingSets(authToken, studentId);
      if (sync.failed > 0) throw new ApiError(0, 'Sem conexão com o servidor.');
      const response = await inviteApi.completeSet(authToken, session.sessionId, exercise.id, input);
      const completedSets = Math.max(setNumber, response.completedSets);
      const performance: StudentSetPerformance = { setNumber, ...parsed.value, completedAt: new Date().toISOString() };
      updateExerciseProgress(exercise.id, completedSets, performance);
      await updateCachedExerciseProgress(session.sessionId, studentId, exercise.id, completedSets, performance).catch(() => undefined);
      if (!await pendingSetCount(session.sessionId, studentId).then((count) => count > 0).catch(() => true)) setOfflineSnapshot(false);
      beginRest();
    } catch (saveError) {
      if (!(saveError instanceof ApiError) || saveError.status !== 0) { setMessage({ tone: 'error', text: saveError instanceof Error ? saveError.message : 'Não foi possível salvar este registro.' }); setSaving(false); return; }
      try {
        await queueSet({ studentId, sessionId: session.sessionId, exerciseId: exercise.id, input });
        const performance: StudentSetPerformance = { setNumber, ...parsed.value, completedAt: new Date().toISOString() };
        updateExerciseProgress(exercise.id, setNumber, performance);
        setOfflineSnapshot(true);
        await updateCachedExerciseProgress(session.sessionId, studentId, exercise.id, setNumber, performance).catch(() => undefined);
        setMessage({ tone: 'offline', text: 'Registro salvo neste dispositivo e pendente para sincronização.' });
        beginRest();
      } catch { setMessage({ tone: 'error', text: 'Não foi possível salvar o registro no dispositivo.' }); }
    } finally { setSaving(false); }
  };
  const beginRest = () => { setClock(Date.now()); setTargetAt(Date.now() + Math.max(0, exercise.restSeconds) * 1000); setPhase('rest'); };
  const confirmExercise = () => Alert.alert('Concluir sem detalhar todas as séries?', 'Confirme apenas se você realizou este exercício. Cargas, repetições ou duração que faltarem não serão inventadas.', [{ text: 'Cancelar', style: 'cancel' }, { text: 'Confirmar conclusão', onPress: () => void (async () => {
    try {
      await inviteApi.confirmExercise(authToken, session.sessionId, exercise.id);
      const authoritative = await inviteApi.session(authToken, session.sessionId);
      setSession(authoritative, false, studentId);
      await cacheWorkout(authoritative, studentId).catch(() => undefined);
      setPhase('rest');
    } catch (confirmError) { setMessage({ tone: 'error', text: confirmError instanceof Error ? confirmError.message : 'Não foi possível concluir o exercício.' }); }
  })() }]);
  const openExercise = (id: string) => router.replace({ pathname: '/student/exercise/[sessionId]/[exerciseId]', params: { sessionId: session.sessionId, exerciseId: id } });

  return <Screen style={styles.page}>
    <TopBar eyebrow={`EXERCÍCIO ${position} DE ${session.exercises.length}`} title={exercise.name} onBack={() => router.back()} />
    {isOfflineSnapshot ? <Card style={styles.offlineCard}><Text style={styles.offlineTitle}>Modo offline</Text><Text style={styles.copy}>Seus registros serão sincronizados quando a conexão voltar.</Text></Card> : null}
    <ExerciseImage imageRef={exercise.imageRef} imageUrl={exercise.imageUrl} contentFit="contain" accessibilityLabel={`Imagem do exercício ${exercise.name}`} style={styles.exerciseImage} />
    <View style={styles.contextRow}><Text style={styles.context}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text><Text style={styles.setProgress}>{Math.min(exercise.completedSets, exercise.sets)}/{exercise.sets} {exercise.trackingMode === 'Duration' ? 'blocos' : 'séries'}</Text></View>
    <View style={styles.prescription}><Prescription label={exercise.trackingMode === 'Duration' ? 'Tempo por bloco' : 'Repetições'} value={exercise.trackingMode === 'Duration' ? durationLabel(exercise.targetDurationSeconds) : `${exercise.repetitionsMin}–${exercise.repetitionsMax}`} /><Prescription label="Descanso" value={`${exercise.restSeconds}s`} /></View>
    {exercise.instructions ? <Detail label="INSTRUÇÕES" value={exercise.instructions} /> : null}
    {exercise.notes ? <Detail label="OBSERVAÇÕES DO PERSONAL" value={exercise.notes} /> : null}

    {phase === 'entry' && !exercise.isCompleted ? <Card style={styles.entryCard}><Text style={styles.eyebrow}>PRÓXIMO {exercise.trackingMode === 'Duration' ? 'BLOCO' : 'REGISTRO'}</Text><Text style={styles.nextSet}>{exercise.trackingMode === 'Duration' ? 'Bloco' : 'Série'} {setNumber} de {exercise.sets}</Text>{previousPerformance ? <Text style={styles.prefillHint}>Usamos o último valor registrado como ponto de partida. Ajuste apenas se precisar.</Text> : null}{exercise.trackingMode === 'Duration' ? <View style={styles.field}><Text style={styles.inputLabel}>Duração realizada (minutos)</Text><TextInput value={durationMinutes} onChangeText={setDurationMinutes} keyboardType="decimal-pad" placeholder={formatMinutes(exercise.targetDurationSeconds)} placeholderTextColor={colors.textMuted} accessibilityLabel={`Duração realizada no bloco ${setNumber} de ${exercise.name}`} style={styles.input} /></View> : <View style={styles.row}><View style={styles.field}><Text style={styles.inputLabel}>Carga real (kg)</Text><TextInput value={weight} onChangeText={setWeight} keyboardType="decimal-pad" placeholder="Ex.: 42,5" placeholderTextColor={colors.textMuted} accessibilityLabel={`Carga realizada na série ${setNumber} de ${exercise.name}`} style={styles.input} /></View><View style={styles.field}><Text style={styles.inputLabel}>Reps reais</Text><TextInput value={repetitions} onChangeText={setRepetitions} keyboardType="number-pad" placeholder={`${exercise.repetitionsMin}–${exercise.repetitionsMax}`} placeholderTextColor={colors.textMuted} accessibilityLabel={`Repetições realizadas na série ${setNumber} de ${exercise.name}`} style={styles.input} /></View></View>}<Button loading={saving} onPress={() => void save()}>{exercise.trackingMode === 'Duration' ? 'Salvar bloco realizado' : 'Salvar série realizada'}</Button><Button variant="ghost" disabled={saving || isOfflineSnapshot} onPress={confirmExercise}>Concluir exercício sem detalhar tudo</Button>{message ? <Text accessibilityRole="alert" style={message.tone === 'error' ? styles.error : styles.offlineMessage}>{message.text}</Text> : null}</Card> : null}

    {phase === 'rest' && !exercise.isCompleted ? <Card style={styles.restCard}><Text style={styles.eyebrow}>DESCANSO</Text><Text accessibilityLiveRegion="polite" style={styles.timer}>{clockLabel(remaining)}</Text><Text style={styles.copy}>{remaining > 0 ? 'Respire e prepare-se. Você pode avançar quando estiver pronto.' : 'Descanso concluído. Os últimos valores continuam preenchidos.'}</Text><Button onPress={() => setPhase('entry')}>{remaining > 0 ? 'Pular descanso' : exercise.trackingMode === 'Duration' ? 'Próximo bloco' : 'Próxima série'}</Button></Card> : null}

    {phase === 'rest' && exercise.isCompleted && !sessionComplete ? <View style={styles.nextArea}><Card style={styles.completedCard}><Tag tone="success">EXERCÍCIO CONCLUÍDO</Tag><Text style={styles.completedTitle}>Você terminou essa.</Text><Text style={styles.copy}>Escolha o próximo exercício. A ordem sugerida continua visível, mas a decisão é sua.</Text></Card>{pendingExercises.map((item, index) => <Card key={item.id} style={styles.pendingCard}><View style={styles.pendingRow}><ExerciseImage imageRef={item.imageRef} imageUrl={item.imageUrl} contentFit="contain" accessible={false} style={styles.pendingImage} /><View style={styles.pendingIdentity}><Text style={styles.pendingName}>{item.name}</Text><Text style={styles.copy}>{item.completedSets}/{item.sets} {item.trackingMode === 'Duration' ? 'blocos' : 'séries'}{index === 0 ? ' · próximo sugerido' : ''}</Text></View></View><Button variant={index === 0 ? 'primary' : 'secondary'} onPress={() => openExercise(item.id)}>Começar exercício</Button></Card>)}</View> : null}

    {phase === 'rest' && sessionComplete ? <Card style={styles.finishCard}><Text style={styles.celebration}>★</Text><Text style={styles.completedTitle}>Treino completo.</Text><Text style={styles.copy}>Você passou por todos os exercícios. Confirme para registrar essa sessão no seu histórico.</Text><Button variant="success" loading={finish.isPending} onPress={() => finish.mutate()}>Concluir treino</Button>{finish.isError ? <Text accessibilityRole="alert" style={styles.error}>{finish.error.message}</Text> : null}</Card> : null}
  </Screen>;
}

async function hydrateSession(sessionId: string, token: string, studentId: string, setSession: (session: StudentSession, offline?: boolean, studentId?: string) => void) {
  try {
    const server = await inviteApi.session(token, sessionId);
    const pending = await pendingSetDetails(server.sessionId, studentId);
    const hydrated = withPendingProgress(server, pending);
    setSession(hydrated, false, studentId);
    await cacheWorkout(hydrated, studentId).catch(() => undefined);
    return;
  } catch (error) { if (!(error instanceof ApiError) || error.status !== 0) throw error; }
  const cached = await cachedSession<StudentSession>(sessionId, studentId);
  if (!cached) throw new Error('A sessão não está disponível neste dispositivo.');
  const pending = await pendingSetDetails(cached.sessionId, studentId);
  setSession(withPendingProgress(cached, pending), true, studentId);
}

async function synchronizePendingSets(token: string, studentId: string) { return syncPendingSets(studentId, async (item) => { await inviteApi.completeSet(token, item.sessionId, item.exerciseId, item.input); }); }
function parseDuration(value: string): { success: true; value: { durationSeconds: number } } | { success: false; message: string } { const minutes = Number(value.trim().replace(',', '.')); if (!Number.isFinite(minutes) || minutes <= 0 || minutes > 1440) return { success: false, message: 'Informe uma duração válida em minutos.' }; return { success: true, value: { durationSeconds: Math.max(1, Math.round(minutes * 60)) } }; }
function formatWeight(value?: number) { return value === undefined ? '' : String(value).replace('.', ','); }
function formatMinutes(seconds?: number) { if (!seconds) return ''; const minutes = seconds / 60; return String(Number(minutes.toFixed(2))).replace('.', ','); }
function durationLabel(seconds?: number) { if (!seconds) return '—'; return seconds >= 60 && seconds % 60 === 0 ? `${seconds / 60} min` : `${seconds}s`; }
function clockLabel(seconds: number) { return `${String(Math.floor(seconds / 60)).padStart(2, '0')}:${String(seconds % 60).padStart(2, '0')}`; }
function Prescription({ label, value }: { label: string; value: string }) { return <View style={styles.prescriptionItem}><Text style={styles.prescriptionLabel}>{label}</Text><Text style={styles.prescriptionValue}>{value}</Text></View>; }
function Detail({ label, value }: { label: string; value: string }) { return <View style={styles.detail}><Text style={styles.detailLabel}>{label}</Text><Text style={styles.copy}>{value}</Text></View>; }

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, exerciseImage: { width: '100%', height: 240, alignSelf: 'center', borderRadius: radius.md, backgroundColor: colors.surfaceElevated }, contextRow: { flexDirection: 'row', justifyContent: 'space-between', gap: spacing.sm }, context: { ...typography.caption, color: colors.titanium, flex: 1 }, setProgress: { ...typography.headingMD, color: colors.primary }, prescription: { flexDirection: 'row', gap: spacing.xs }, prescriptionItem: { flex: 1, gap: spacing.xxs, padding: spacing.sm, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, prescriptionLabel: { ...typography.caption, color: colors.textMuted }, prescriptionValue: { ...typography.bodyLG, color: colors.textPrimary }, detail: { gap: spacing.xs, paddingTop: spacing.xs, borderTopWidth: 1, borderTopColor: colors.border }, detailLabel: { ...typography.caption, color: colors.primary, letterSpacing: .6 }, entryCard: { gap: spacing.md, borderColor: colors.primary }, nextSet: { ...typography.headingMD, color: colors.textPrimary }, prefillHint: { ...typography.caption, color: colors.titanium }, row: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.sm }, field: { flex: 1, gap: spacing.xs }, inputLabel: { ...typography.caption, color: colors.textSecondary }, input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 48, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, paddingHorizontal: spacing.md, paddingVertical: spacing.sm, backgroundColor: colors.surfaceElevated }, offlineCard: { gap: spacing.xs, borderColor: colors.warning }, offlineTitle: { ...typography.headingMD, color: colors.warning }, offlineMessage: { ...typography.caption, color: colors.warning }, error: { ...typography.caption, color: colors.danger }, restCard: { gap: spacing.md, alignItems: 'stretch', borderColor: colors.primary }, timer: { ...typography.displayLG, color: colors.textPrimary, textAlign: 'center' }, nextArea: { gap: spacing.md }, completedCard: { gap: spacing.sm, borderColor: colors.success }, completedTitle: { ...typography.headingLG, color: colors.textPrimary }, pendingCard: { gap: spacing.md }, pendingRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm }, pendingImage: { width: 72, height: 72, borderRadius: radius.sm }, pendingIdentity: { flex: 1, gap: spacing.xxs }, pendingName: { ...typography.bodyLG, color: colors.textPrimary, fontFamily: 'MontserratSemiBold' }, finishCard: { gap: spacing.md, alignItems: 'stretch', borderColor: colors.success }, celebration: { fontSize: 46, color: colors.success, textAlign: 'center' },
});
