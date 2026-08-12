import { router, useLocalSearchParams } from 'expo-router';
import { Image, StyleSheet, Text, View } from 'react-native';
import { useCompleteWorkout, useStartWorkout, useTrainingToday } from '@/src/api/hooks';
import { Button, Card, ErrorView, LoadingView, ProgressBar, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { exerciseImage } from '@/src/design/exercise-media';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainingStore } from '@/src/state/training-store';
import { feedback } from '@/src/platform/feedback';
import { telemetry } from '@/src/platform/telemetry';

export default function WorkoutScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const training = useTrainingToday();
  const start = useStartWorkout();
  const complete = useCompleteWorkout();
  const setActiveSession = useTrainingStore((state) => state.setActiveSession);
  if (training.isLoading) return <LoadingView />;
  if (training.error) return <ErrorView message={training.error.message} onRetry={() => void training.refetch()} />;
  const workout = training.data;
  if (!workout || workout.id !== id) return <ErrorView message="Este treino não está mais disponível." onRetry={() => router.replace('/(app)/home')} />;

  const completedSets = workout.exercises.reduce((total, exercise) => total + exercise.completedSets, 0);
  const plannedSets = workout.exercises.reduce((total, exercise) => total + exercise.prescribedSets, 0);
  const started = workout.status === 'InProgress';
  const startWorkout = async () => { await start.mutateAsync(workout.id); feedback.success(); telemetry.event('workout_started'); setActiveSession(workout.id); };
  const finishWorkout = async () => { const result = await complete.mutateAsync(workout.id); feedback.success(); telemetry.event('workout_completed'); router.replace({ pathname: '/(app)/summary/[id]', params: { id: result.id, completedSets: String(result.completedSets) } }); };

  return <Screen>
    <TopBar eyebrow={workout.status === 'Planned' ? 'Pronto para começar' : 'Treino em andamento'} title={workout.name} onBack={() => router.replace('/(app)/training')} />
    <View style={styles.progress}><View style={styles.progressHeader}><Text style={styles.progressText}>{completedSets}/{plannedSets} séries</Text><Text style={styles.progressText}>{workout.exercises.length} exercícios</Text></View><ProgressBar value={plannedSets ? completedSets / plannedSets : 0} /></View>
    {workout.status === 'Planned' && <Card style={styles.startCard}><Text style={styles.startEyebrow}>PRONTO PARA COMEÇAR</Text><Text style={styles.startTitle}>Aqueça, ajuste o ambiente e comece quando estiver pronto.</Text><Button onPress={startWorkout} loading={start.isPending}>Iniciar treino</Button>{start.error && <Text style={styles.error}>{start.error.message}</Text>}</Card>}
    <View style={styles.exerciseList}>{workout.exercises.map((exercise) => { const image = exerciseImage(exercise.name); return <Card key={exercise.id} style={styles.exerciseCard}><View style={styles.exerciseTop}>{image ? <Image source={image} style={styles.thumb} resizeMode="cover" /> : <View style={styles.sequence}><Text style={styles.sequenceText}>{exercise.sequence}</Text></View>}<View style={styles.exerciseInfo}><Text style={styles.exerciseName}>{exercise.name}</Text><Text style={styles.exerciseTarget}>{exercise.prescribedSets} × {exercise.minimumRepetitions}–{exercise.maximumRepetitions} reps · alvo {exercise.recommendedLoadKg} kg</Text></View><Tag tone={exercise.completedSets >= exercise.prescribedSets ? 'success' : 'neutral'}>{exercise.completedSets}/{exercise.prescribedSets}</Tag></View><Button variant="secondary" disabled={!started} onPress={() => router.push(`/(app)/exercise/${workout.id}/${exercise.id}`)}>{started ? 'Abrir exercício' : 'Inicie para registrar'}</Button></Card>; })}</View>
    {started && <Button onPress={finishWorkout} loading={complete.isPending}>Finalizar treino</Button>}
    {complete.error && <Text style={styles.error}>{complete.error.message}</Text>}
  </Screen>;
}

const styles = StyleSheet.create({
  progress: { gap: spacing.sm }, progressHeader: { flexDirection: 'row', justifyContent: 'space-between' }, progressText: { ...typography.caption, color: colors.textSecondary }, startCard: { gap: spacing.md, borderColor: '#55202A' }, startEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, startTitle: { ...typography.bodyLG, color: colors.textPrimary }, exerciseList: { gap: spacing.md }, exerciseCard: { gap: spacing.md }, exerciseTop: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm }, thumb: { width: 64, height: 64, borderRadius: 10 }, sequence: { width: 40, height: 40, borderRadius: 20, backgroundColor: colors.surfaceElevated, alignItems: 'center', justifyContent: 'center' }, sequenceText: { ...typography.caption, color: colors.primary }, exerciseInfo: { flex: 1, gap: spacing.xxs }, exerciseName: { ...typography.headingMD, color: colors.textPrimary }, exerciseTarget: { ...typography.bodyMD, color: colors.textSecondary }, error: { ...typography.bodyMD, color: colors.danger },
});
