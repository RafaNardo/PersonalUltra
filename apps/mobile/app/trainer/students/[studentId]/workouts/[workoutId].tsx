import { router, useLocalSearchParams } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useTrainerStudentWorkout } from '@/src/features/trainer/training/hooks';

export default function TrainerStudentWorkoutScreen() {
  const { studentId, workoutId } = useLocalSearchParams<{ studentId: string; workoutId: string }>();
  const student = useTrainerStudent(studentId);
  const workout = useTrainerStudentWorkout(studentId, workoutId);

  if (student.isLoading || workout.isLoading) return <LoadingView message="Abrindo o treino…" />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;
  if (workout.isError) return <ErrorView message={workout.error.message} onRetry={() => workout.refetch()} />;

  const studentData = student.data!;
  const workoutData = workout.data!;
  return <Screen style={styles.page}>
    <TopBar eyebrow={`${studentData.firstName} ${studentData.lastName}`} title={workoutData.name} onBack={() => router.back()} action={workoutData.isRecommended ? <Tag tone="success">RECOMENDADO</Tag> : undefined} />
    <Text style={styles.schedule}>{weekday(workoutData.recommendedDay)} · {workoutData.exercises.length} {workoutData.exercises.length === 1 ? 'exercício' : 'exercícios'}</Text>
    {workoutData.notes ? <Text style={styles.copy}>{workoutData.notes}</Text> : null}
    <View style={styles.sectionHeader}><View><Text style={styles.sectionTitle}>Exercícios prescritos</Text><Text style={styles.copy}>Monte a prescrição com o catálogo Ultra.</Text></View><Text style={styles.count}>{workoutData.exercises.length}</Text></View>
    <Button accessibilityHint="Abre a busca no catálogo sem alterar o treino ainda" onPress={() => router.push({ pathname: '/trainer/students/[studentId]/workouts/[workoutId]/catalog', params: { studentId: studentId!, workoutId: workoutId! } })}>+ Adicionar exercício</Button>
    {workoutData.exercises.length === 0 ? <EmptyState title="Treino sem exercícios" message="A prescrição ainda não possui exercícios configurados." /> : <View style={styles.list}>{workoutData.exercises.map((exercise) => <Card key={exercise.id} style={styles.exercise}>
      <View style={styles.exerciseHeader}><View style={styles.sequence}><Text style={styles.sequenceText}>{exercise.sequence}</Text></View><View style={styles.exerciseIdentity}><Text style={styles.exerciseName}>{exercise.name}</Text>{exercise.primaryMuscleGroup || exercise.equipment ? <Text style={styles.context}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text> : null}</View></View>
      <Text style={styles.prescription}>{exercise.sets} séries · {exercise.repetitionsMin}–{exercise.repetitionsMax} reps · {exercise.restSeconds}s descanso</Text>
      {exercise.notes ? <Text style={styles.copy}>{exercise.notes}</Text> : null}
      {exercise.instructions ? <Text numberOfLines={3} style={styles.instructions}>{exercise.instructions}</Text> : null}
    </Card>)}</View>}
    <Card style={styles.readOnlyNotice}><Text style={styles.noticeTitle}>Visualização da prescrição</Text><Text style={styles.copy}>Você já pode explorar e configurar exercícios do catálogo. A inclusão e a publicação serão habilitadas na próxima etapa do editor.</Text></Card>
  </Screen>;
}

function weekday(day: number) {
  return ['Dia não definido', 'Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado', 'Domingo'][day] ?? 'Dia não definido';
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.md }, schedule: { ...typography.caption, color: colors.titanium }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, sectionHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.md, marginTop: spacing.sm }, sectionTitle: { ...typography.headingMD, color: colors.textPrimary }, count: { ...typography.caption, color: colors.primary, backgroundColor: colors.surfaceElevated, paddingHorizontal: spacing.sm, paddingVertical: spacing.xs, borderRadius: radius.pill }, list: { gap: spacing.sm }, exercise: { gap: spacing.sm }, exerciseHeader: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm }, sequence: { width: 34, height: 34, alignItems: 'center', justifyContent: 'center', borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, sequenceText: { ...typography.caption, color: colors.primary }, exerciseIdentity: { flex: 1, gap: spacing.xxs }, exerciseName: { ...typography.headingMD, color: colors.textPrimary }, context: { ...typography.caption, color: colors.textMuted }, prescription: { ...typography.bodyMD, color: colors.primary }, instructions: { ...typography.bodyMD, color: colors.titanium, lineHeight: 21, paddingTop: spacing.xs, borderTopWidth: 1, borderTopColor: colors.border }, readOnlyNotice: { gap: spacing.xs, backgroundColor: colors.surfaceElevated }, noticeTitle: { ...typography.caption, color: colors.titaniumLight } });
