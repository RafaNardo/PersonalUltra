import { router } from 'expo-router';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useTrainingPlan, useTrainingToday } from '@/src/api/hooks';
import { Card, EmptyState, ErrorView, LoadingView, ProgressBar, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';

export default function TrainingScreen() {
  const training = useTrainingToday();
  const plan = useTrainingPlan();
  if (training.isLoading || plan.isLoading) return <LoadingView message="Preparando seu plano…" />;
  if (training.error || plan.error || !training.data || !plan.data) return <ErrorView message="Não foi possível carregar seu plano de treino." onRetry={() => { void training.refetch(); void plan.refetch(); }} />;
  const workout = training.data;
  if (!workout.exercises.length) return <Screen><TopBar eyebrow="Seu plano" title="Treino" /><EmptyState title="Nenhum exercício disponível" message="Seu treino de hoje ainda não tem exercícios definidos." actionLabel="Atualizar" onAction={() => void training.refetch()} /></Screen>;
  const completed = workout.exercises.reduce((total, exercise) => total + exercise.completedSets, 0);
  const planned = workout.exercises.reduce((total, exercise) => total + exercise.prescribedSets, 0);
  const finished = workout.status === 'Completed';
  const openWorkout = () => router.push(`/student/workout/${workout.id}`);
  return <Screen><TopBar eyebrow="Seu plano" title="Treino" />
    <Card style={styles.summary}><View><Tag tone={finished ? 'success' : 'primary'}>{finished ? 'CONCLUÍDO' : workout.status === 'InProgress' ? 'EM ANDAMENTO' : 'TREINO DE HOJE'}</Tag><Text style={styles.title}>{workout.name}</Text><Text style={styles.copy}>{workout.exercises.length} exercícios · cerca de {Math.max(40, workout.exercises.length * 18)} min</Text></View><View style={styles.summaryProgress}><Text style={styles.progressLabel}>{completed}/{planned} séries</Text><ProgressBar value={planned ? completed / planned : 0} /></View><Pressable disabled={finished} onPress={openWorkout} style={[styles.open, finished && styles.openDisabled]}><Text style={styles.openText}>{finished ? 'Treino concluído' : workout.status === 'InProgress' ? 'Continuar treino' : 'Começar treino'}</Text><Text style={styles.openArrow}>→</Text></Pressable></Card>
    <View style={styles.planHeader}><Text style={styles.listTitle}>SEU PLANO</Text><Text style={styles.listCount}>{plan.data.workouts.length} sessões por ciclo</Text></View>
    <View style={styles.planList}>{plan.data.workouts.map((template) => <Pressable key={template.id} accessibilityRole="button" accessibilityLabel={`Ver ${template.name}`} accessibilityHint="Abre os exercícios prescritos" onPress={() => router.push(`/student/training-plan/${template.id}`)}><Card style={styles.planCard}><View style={styles.planSequence}><Text style={styles.planSequenceText}>{template.sequence}</Text></View><View style={styles.exerciseBody}><Text style={styles.exerciseName}>{template.name}</Text><Text style={styles.exerciseCopy}>{template.exercises.length} exercícios · ver prescrição</Text></View><Text style={styles.chevron}>›</Text></Card></Pressable>)}</View>
  </Screen>;
}

const styles = StyleSheet.create({
  summary: { gap: spacing.md, padding: spacing.lg }, title: { ...typography.displayLG, color: colors.textPrimary, marginTop: spacing.sm }, copy: { ...typography.bodyMD, color: colors.textSecondary, marginTop: spacing.xxs }, summaryProgress: { gap: spacing.xs }, progressLabel: { ...typography.caption, color: colors.textSecondary }, open: { minHeight: 52, paddingHorizontal: spacing.lg, borderRadius: radius.sm, backgroundColor: colors.primary, flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }, openDisabled: { opacity: .55 }, openText: { ...typography.bodyLG, color: colors.textPrimary, fontFamily: 'MontserratBold' }, openArrow: { ...typography.headingMD, color: colors.textPrimary },
  listTitle: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, listCount: { ...typography.caption, color: colors.textMuted }, exerciseBody: { flex: 1, gap: spacing.xs }, exerciseName: { ...typography.bodyLG, color: colors.textPrimary, flex: 1, fontFamily: 'MontserratBold' }, exerciseCopy: { ...typography.caption, color: colors.textSecondary }, chevron: { ...typography.headingLG, color: colors.textMuted },
  planHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginTop: spacing.lg }, planList: { gap: spacing.sm }, planCard: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, padding: spacing.md }, planSequence: { width: 36, height: 36, alignItems: 'center', justifyContent: 'center', borderRadius: 18, backgroundColor: '#42121A' }, planSequenceText: { ...typography.headingMD, color: colors.primary },
});
