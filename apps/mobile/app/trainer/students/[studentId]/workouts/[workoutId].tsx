import Ionicons from '@expo/vector-icons/Ionicons';
import { router, useLocalSearchParams } from 'expo-router';
import { useEffect } from 'react';
import { Alert, Image, Pressable, StyleSheet, Text, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useDeleteTrainerStudentWorkout, useTrainerStudentWorkout, useUpdateTrainerStudentWorkout } from '@/src/features/trainer/training/hooks';
import { useWorkoutEditorStore, workoutEditorKey, type WorkoutEditorExercise } from '@/src/features/trainer/training/workout-editor-store';
import { feedback } from '@/src/platform/feedback';
import { exerciseMediaSource } from '@/src/shared/training/exercise-media';

export default function TrainerStudentWorkoutScreen() {
  const { studentId, workoutId } = useLocalSearchParams<{ studentId: string; workoutId: string }>();
  const key = workoutEditorKey(studentId, workoutId);
  const student = useTrainerStudent(studentId);
  const workout = useTrainerStudentWorkout(studentId, workoutId);
  const updateWorkout = useUpdateTrainerStudentWorkout(studentId, workoutId);
  const deleteWorkout = useDeleteTrainerStudentWorkout(studentId, workoutId);
  const draft = useWorkoutEditorStore((state) => state.drafts[key]);
  const initialize = useWorkoutEditorStore((state) => state.initialize);
  const resetFromServer = useWorkoutEditorStore((state) => state.resetFromServer);
  const removeExercise = useWorkoutEditorStore((state) => state.removeExercise);
  const moveExercise = useWorkoutEditorStore((state) => state.moveExercise);

  useEffect(() => { if (workout.data) initialize(key, workout.data); }, [initialize, key, workout.data]);

  if (student.isLoading || workout.isLoading || (workout.data && !draft)) return <LoadingView message="Abrindo o editor…" />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;
  if (workout.isError) return <ErrorView message={workout.error.message} onRetry={() => workout.refetch()} />;

  const studentData = student.data!;
  const workoutData = workout.data!;
  const editor = draft!;
  const addExercise = () => router.push({ pathname: '/trainer/students/[studentId]/workouts/[workoutId]/catalog', params: { studentId: studentId!, workoutId: workoutId! } });
  const editExercise = (exercise: WorkoutEditorExercise) => router.push({ pathname: '/trainer/students/[studentId]/workouts/[workoutId]/catalog/[exerciseId]', params: { studentId: studentId!, workoutId: workoutId!, exerciseId: exercise.exerciseId ?? 'snapshot', workoutExerciseId: exercise.clientId } });
  const confirmRemoval = (exercise: WorkoutEditorExercise) => Alert.alert('Remover exercício?', `${exercise.name} será removido quando você publicar as alterações.`, [{ text: 'Cancelar', style: 'cancel' }, { text: 'Remover', style: 'destructive', onPress: () => { removeExercise(key, exercise.clientId); feedback.warning(); } }]);
  const discard = () => Alert.alert('Descartar alterações?', 'A lista voltará ao último estado publicado.', [{ text: 'Continuar editando', style: 'cancel' }, { text: 'Descartar', style: 'destructive', onPress: () => resetFromServer(key, workoutData) }]);
  const confirmDelete = () => Alert.alert('Excluir este treino?', `${workoutData.name} deixará de aparecer para ${studentData.firstName}. O histórico de sessões realizadas será preservado.`, [{ text: 'Cancelar', style: 'cancel' }, { text: 'Excluir treino', style: 'destructive', onPress: () => void removeWorkout() }]);
  const removeWorkout = async () => {
    try {
      await deleteWorkout.mutateAsync();
      feedback.success();
      router.replace({ pathname: '/trainer/students/[id]', params: { id: studentId!, section: 'training' } });
    } catch (error) {
      feedback.warning();
      Alert.alert('Não foi possível excluir', error instanceof Error ? error.message : 'Tente novamente.');
    }
  };
  const publish = async () => {
    try {
      const saved = await updateWorkout.mutateAsync(editor.exercises.map((exercise, index) => ({
        id: exercise.id,
        exerciseId: exercise.exerciseId,
        sequence: index + 1,
        sets: exercise.sets,
        repetitionsMin: exercise.repetitionsMin,
        repetitionsMax: exercise.repetitionsMax,
        restSeconds: exercise.restSeconds,
        notes: exercise.notes,
      })));
      resetFromServer(key, saved);
      feedback.success();
      Alert.alert('Treino atualizado', 'As alterações já estão disponíveis para o aluno nas próximas sessões.');
    } catch {
      feedback.warning();
    }
  };

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow={`${studentData.firstName} ${studentData.lastName}`} title={workoutData.name} onBack={() => router.back()} action={workoutData.isRecommended ? <Tag tone="success">RECOMENDADO</Tag> : undefined} />
    <View style={styles.metaRow}><Text style={styles.schedule}>{weekday(workoutData.recommendedDay)} · {editor.exercises.length} {editor.exercises.length === 1 ? 'exercício' : 'exercícios'}</Text>{editor.dirty ? <Tag tone="primary">NÃO PUBLICADO</Tag> : <Tag tone="success">PUBLICADO</Tag>}</View>
    {workoutData.notes ? <Text style={styles.copy}>{workoutData.notes}</Text> : null}
    <View style={styles.sectionHeader}><View style={styles.sectionCopy}><Text style={styles.sectionTitle}>Exercícios prescritos</Text><Text style={styles.copy}>Edite, remova ou reorganize antes de publicar.</Text></View><Text style={styles.count}>{editor.exercises.length}</Text></View>
    {editor.exercises.length > 0 ? <Button variant="secondary" disabled={editor.exercises.length >= 30} accessibilityHint="Abre a busca no catálogo" onPress={addExercise}>+ Adicionar exercício</Button> : null}
    {editor.exercises.length >= 30 ? <Text style={styles.limit}>Limite de 30 exercícios atingido.</Text> : null}

    {editor.exercises.length === 0 ? <EmptyState status="TREINO EM CONSTRUÇÃO" symbol="+" title="Comece pela primeira escolha do catálogo." message="Adicione exercícios, configure a prescrição e organize a sequência antes de salvar." actionLabel="Abrir catálogo" onAction={addExercise} /> : <View style={styles.list}>{editor.exercises.map((exercise, index) => <ExerciseCard key={exercise.clientId} exercise={exercise} index={index} count={editor.exercises.length} onMove={(to) => { moveExercise(key, index, to); feedback.selection(); }} onEdit={() => editExercise(exercise)} onRemove={() => confirmRemoval(exercise)} />)}</View>}

    {updateWorkout.isError ? <Card style={styles.errorCard}><Text accessibilityRole="alert" style={styles.errorTitle}>Não foi possível publicar</Text><Text style={styles.copy}>{updateWorkout.error.message} Revise o treino ou tente novamente.</Text></Card> : null}
    <Button loading={updateWorkout.isPending} disabled={!editor.dirty} accessibilityHint="Salva a lista completa de exercícios para o aluno" onPress={() => void publish()}>Publicar alterações</Button>
    {editor.dirty ? <Button variant="ghost" disabled={updateWorkout.isPending} onPress={discard}>Descartar alterações</Button> : null}
    <Text style={styles.concurrencyNote}>As alterações chegam ao aluno após a publicação. Treinos já iniciados continuam como estavam.</Text>
    <Pressable disabled={deleteWorkout.isPending || updateWorkout.isPending} accessibilityRole="button" accessibilityLabel="Excluir treino do aluno" onPress={confirmDelete} style={({ pressed }) => [styles.deleteButton, pressed && styles.deletePressed, (deleteWorkout.isPending || updateWorkout.isPending) && styles.disabled]}><Ionicons name="trash-outline" size={19} color={colors.danger} /><Text style={styles.deleteText}>{deleteWorkout.isPending ? 'Excluindo…' : 'Excluir treino'}</Text></Pressable>
  </Screen>;
}

function ExerciseCard({ exercise, index, count, onMove, onEdit, onRemove }: { exercise: WorkoutEditorExercise; index: number; count: number; onMove: (to: number) => void; onEdit: () => void; onRemove: () => void }) {
  const source = exerciseMediaSource(exercise.imageRef);
  return <Card style={styles.exercise}>
    <View style={styles.exerciseHeader}>
      {source ? <Image source={source} accessible={false} resizeMode="cover" style={styles.thumbnail} /> : <View style={styles.thumbnailFallback}><Text style={styles.thumbnailFallbackText}>{index + 1}</Text></View>}
      <View style={styles.exerciseIdentity}><Text style={styles.exerciseName}>{exercise.name}</Text>{exercise.primaryMuscleGroup || exercise.equipment ? <Text style={styles.context}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text> : null}<Text style={styles.prescription}>{exercise.sets} séries · {exercise.repetitionsMin}–{exercise.repetitionsMax} reps · {exercise.restSeconds}s</Text></View>
    </View>
    {exercise.notes ? <Text style={styles.copy}>{exercise.notes}</Text> : null}
    <View style={styles.actions}>
      <Pressable disabled={index === 0} accessibilityRole="button" accessibilityLabel={`Mover ${exercise.name} para cima`} accessibilityState={{ disabled: index === 0 }} onPress={() => onMove(index - 1)} style={[styles.orderButton, index === 0 && styles.disabled]}><Text style={styles.orderText}>↑</Text></Pressable>
      <Pressable disabled={index === count - 1} accessibilityRole="button" accessibilityLabel={`Mover ${exercise.name} para baixo`} accessibilityState={{ disabled: index === count - 1 }} onPress={() => onMove(index + 1)} style={[styles.orderButton, index === count - 1 && styles.disabled]}><Text style={styles.orderText}>↓</Text></Pressable>
      <Pressable accessibilityRole="button" accessibilityLabel={`Editar ${exercise.name}`} onPress={onEdit} style={styles.textButton}><Text style={styles.editText}>Editar</Text></Pressable>
      <Pressable accessibilityRole="button" accessibilityLabel={`Remover ${exercise.name}`} onPress={onRemove} style={styles.textButton}><Text style={styles.removeText}>Remover</Text></Pressable>
    </View>
  </Card>;
}

function weekday(day: number) {
  return ['Dia não definido', 'Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado', 'Domingo'][day] ?? 'Dia não definido';
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, metaRow: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.sm }, schedule: { ...typography.caption, color: colors.titanium, flex: 1 }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, sectionHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.md, marginTop: spacing.sm }, sectionCopy: { flex: 1 }, sectionTitle: { ...typography.headingMD, color: colors.textPrimary }, count: { ...typography.caption, color: colors.primary, backgroundColor: colors.surfaceElevated, paddingHorizontal: spacing.sm, paddingVertical: spacing.xs, borderRadius: radius.pill }, limit: { ...typography.caption, color: colors.warning }, list: { gap: spacing.sm }, exercise: { gap: spacing.md }, exerciseHeader: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm }, thumbnail: { width: 82, height: 82, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, thumbnailFallback: { width: 82, height: 82, alignItems: 'center', justifyContent: 'center', borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, thumbnailFallbackText: { ...typography.headingMD, color: colors.primary }, exerciseIdentity: { flex: 1, gap: spacing.xxs }, exerciseName: { ...typography.headingMD, color: colors.textPrimary }, context: { ...typography.caption, color: colors.textMuted }, prescription: { ...typography.bodyMD, color: colors.primary }, actions: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs, paddingTop: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border }, orderButton: { width: 44, height: 44, alignItems: 'center', justifyContent: 'center', borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, orderText: { ...typography.headingMD, color: colors.titaniumLight }, disabled: { opacity: .3 }, textButton: { minHeight: 44, justifyContent: 'center', paddingHorizontal: spacing.xs }, editText: { ...typography.caption, color: colors.primary }, removeText: { ...typography.caption, color: colors.danger }, errorCard: { gap: spacing.xs, borderColor: colors.danger, backgroundColor: '#251216' }, errorTitle: { ...typography.caption, color: colors.danger }, concurrencyNote: { ...typography.caption, color: colors.textMuted, textAlign: 'center', lineHeight: 18 }, deleteButton: { minHeight: 48, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border, marginTop: spacing.sm, paddingTop: spacing.lg }, deletePressed: { opacity: .7 }, deleteText: { ...typography.bodyMD, color: colors.danger, fontFamily: 'MontserratSemiBold' },
});
