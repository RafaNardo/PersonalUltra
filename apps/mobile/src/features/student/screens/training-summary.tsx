import { router, useLocalSearchParams } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { StyleSheet, Text, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentSessionDetail } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { ExerciseImage } from '@/src/shared/training/exercise-image';

/**
 * A history screen is deliberately server-backed. It must remain useful after
 * the active Zustand snapshot has been cleared or the app has restarted.
 */
export function StudentTrainingSummaryScreen() {
  const { sessionId } = useLocalSearchParams<{ sessionId: string }>();
  const authSession = useInviteSessionStore((state) => state.session);
  const summary = useQuery({
    queryKey: ['student', authSession?.studentId, 'training-session', sessionId],
    queryFn: () => inviteApi.session(authSession!.accessToken, sessionId!),
    enabled: Boolean(authSession && sessionId),
  });

  if (!authSession) { router.replace('/login'); return null; }
  if (summary.isLoading) return <LoadingView message="Carregando resumo do treino…" />;
  if (summary.isError) return <ErrorView message={summary.error.message} onRetry={() => summary.refetch()} />;
  if (!summary.data) return <ErrorView message="Este resumo não está disponível." onRetry={() => summary.refetch()} />;

  return <SummaryContent summary={summary.data} />;
}

function SummaryContent({ summary }: { summary: StudentSessionDetail }) {
  const orderedExercises = [...summary.exercises].sort((left, right) => left.sequence - right.sequence);
  const completedExercises = orderedExercises.filter((exercise) => exercise.completedSets >= exercise.sets).length;
  const completedSets = orderedExercises.reduce((total, exercise) => total + exercise.performances.length, 0);
  const totalSets = orderedExercises.reduce((total, exercise) => total + exercise.sets, 0);
  const duration = summary.completedAt ? durationLabel(summary.startedAt, summary.completedAt) : undefined;

  return <Screen style={styles.page}>
    <TopBar eyebrow="RESUMO DO TREINO" title={summary.workoutName} onBack={() => router.replace('/student/training')} />
    <View style={styles.heading}>
      <View style={styles.headingCopy}>
        <Text style={styles.title}>{summary.status === 'Completed' ? 'Treino concluído' : 'Sessão de treino'}</Text>
        <Text style={styles.copy}>{formatDate(summary.completedAt ?? summary.startedAt)}</Text>
      </View>
      <Tag tone={summary.status === 'Completed' ? 'success' : 'neutral'}>{summary.status === 'Completed' ? 'Concluído' : 'Em andamento'}</Tag>
    </View>
    <Card style={styles.stats}>
      <Stat label="Exercícios" value={`${completedExercises}/${orderedExercises.length}`} />
      <Stat label="Séries" value={`${completedSets}/${totalSets}`} />
      {duration ? <Stat label="Duração" value={duration} /> : null}
    </Card>
    {orderedExercises.length === 0 ? <EmptyState status="SEM REGISTROS" symbol="●" title="Esta sessão não possui exercícios registrados." message="O resumo permanece disponível, mas não há séries ou desempenhos para exibir." /> : orderedExercises.map((exercise) => <SummaryExercise key={exercise.id} exercise={exercise} />)}
    <Button onPress={() => router.replace('/student/training')}>Voltar aos treinos</Button>
  </Screen>;
}

function SummaryExercise({ exercise }: { exercise: StudentSessionDetail['exercises'][number] }) {
  return <Card style={styles.exerciseCard}>
    <View style={styles.exerciseHeader}>
      <ExerciseImage imageRef={exercise.imageRef} imageUrl={exercise.imageUrl} accessibilityLabel={`Imagem do exercício ${exercise.name}`} style={styles.thumbnail} />
      <View style={styles.exerciseIdentity}><Text style={styles.exerciseName}>{exercise.sequence}. {exercise.name}</Text><Text style={styles.copy}>{exercise.completedSets} de {exercise.sets} séries concluídas</Text></View>
      <Tag tone={exercise.completedSets >= exercise.sets ? 'success' : 'neutral'}>{exercise.completedSets >= exercise.sets ? 'Concluído' : 'Parcial'}</Tag>
    </View>
    {exercise.performances.length ? <View style={styles.sets}>{exercise.performances.map((performance) => <View key={performance.setNumber} style={styles.setRow}><Text style={styles.setLabel}>Série {performance.setNumber}</Text><Text style={styles.setValue}>{performance.weightKg} kg × {performance.repetitions} reps</Text></View>)}</View> : <Text style={styles.copy}>Nenhuma série registrada.</Text>}
  </Card>;
}

function Stat({ label, value }: { label: string; value: string }) { return <View style={styles.stat}><Text style={styles.statLabel}>{label}</Text><Text style={styles.statValue}>{value}</Text></View>; }

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Data não disponível' : new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium', timeStyle: 'short' }).format(date);
}

function durationLabel(startedAt: string, completedAt: string) {
  const started = new Date(startedAt).getTime();
  const completed = new Date(completedAt).getTime();
  if (!Number.isFinite(started) || !Number.isFinite(completed) || completed <= started) return undefined;
  const minutes = Math.floor((completed - started) / 60_000);
  return minutes > 0 ? `${minutes} min` : 'menos de 1 min';
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.lg },
  heading: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: spacing.md },
  headingCopy: { flex: 1, gap: spacing.xs },
  title: { ...typography.headingLG, color: colors.textPrimary },
  copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
  stats: { flexDirection: 'row', gap: spacing.xs },
  stat: { flex: 1, gap: spacing.xxs, padding: spacing.sm, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated },
  statLabel: { ...typography.caption, color: colors.textMuted },
  statValue: { ...typography.bodyLG, color: colors.textPrimary },
  exerciseCard: { gap: spacing.md },
  exerciseHeader: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.sm },
  thumbnail: { width: 56, height: 56, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated },
  exerciseIdentity: { flex: 1, gap: spacing.xxs },
  exerciseName: { ...typography.headingMD, color: colors.textPrimary },
  sets: { gap: spacing.xs, borderTopWidth: 1, borderTopColor: colors.border, paddingTop: spacing.sm },
  setRow: { flexDirection: 'row', justifyContent: 'space-between', gap: spacing.sm },
  setLabel: { ...typography.caption, color: colors.textMuted },
  setValue: { ...typography.bodyMD, color: colors.titanium },
});
