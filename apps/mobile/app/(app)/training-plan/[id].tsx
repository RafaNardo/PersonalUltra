import { router, useLocalSearchParams } from 'expo-router';
import { Image, StyleSheet, Text, View } from 'react-native';
import { useTrainingPlan } from '@/src/api/hooks';
import { Card, EmptyState, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { exerciseImage } from '@/src/design/exercise-media';
import { colors, radius, spacing, typography } from '@/src/design/tokens';

export default function TrainingPlanDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const plan = useTrainingPlan();
  if (plan.isLoading) return <LoadingView message="Carregando a prescrição…" />;
  if (plan.error || !plan.data) return <ErrorView message="Não foi possível carregar este treino." onRetry={() => void plan.refetch()} />;
  const workout = plan.data.workouts.find((item) => item.id === id);
  if (!workout) return <Screen><TopBar eyebrow="Seu plano" title="Treino" onBack={() => router.back()} /><EmptyState title="Treino não encontrado" message="Atualize o plano e tente novamente." actionLabel="Atualizar" onAction={() => void plan.refetch()} /></Screen>;

  return <Screen><TopBar eyebrow={`Sessão ${workout.sequence} · ${plan.data.name}`} title={workout.name} onBack={() => router.back()} />
    <Card style={styles.summary}><Tag tone="primary">PRESCRIÇÃO DO PLANO</Tag><Text style={styles.summaryTitle}>{workout.exercises.length} exercícios</Text><Text style={styles.summaryCopy}>Consulte a sequência, séries, repetições e carga sugerida. A execução acontece no treino agendado.</Text></Card>
    <View style={styles.list}>{workout.exercises.map((exercise) => { const image = exerciseImage(exercise.name); return <Card key={exercise.id} style={styles.exercise}><View style={styles.thumb}>{image ? <Image source={image} style={styles.thumbImage} resizeMode="cover" /> : <Text style={styles.thumbFallback}>{exercise.sequence}</Text>}</View><View style={styles.body}><View style={styles.exerciseTop}><Text style={styles.exerciseName}>{exercise.name}</Text><Text style={styles.sequence}>{exercise.sequence}</Text></View><Text style={styles.muscle}>{exercise.primaryMuscleGroup}</Text><Text style={styles.prescription}>{exercise.prescribedSets} × {exercise.minimumRepetitions}–{exercise.maximumRepetitions}</Text><Text style={styles.detail}>Alvo {exercise.recommendedLoadKg} kg · descanso {exercise.restSeconds}s</Text></View></Card>; })}</View>
  </Screen>;
}

const styles = StyleSheet.create({
  summary: { gap: spacing.sm }, summaryTitle: { ...typography.headingLG, color: colors.textPrimary }, summaryCopy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, list: { gap: spacing.sm }, exercise: { flexDirection: 'row', gap: spacing.sm, padding: spacing.sm }, thumb: { width: 82, height: 82, overflow: 'hidden', borderRadius: radius.sm, backgroundColor: colors.surfaceElevated, alignItems: 'center', justifyContent: 'center' }, thumbImage: { width: '100%', height: '100%' }, thumbFallback: { ...typography.headingLG, color: colors.primary }, body: { flex: 1, gap: spacing.xxs }, exerciseTop: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm }, exerciseName: { ...typography.bodyLG, color: colors.textPrimary, fontFamily: 'MontserratBold', flex: 1 }, sequence: { ...typography.caption, color: colors.primary }, muscle: { ...typography.caption, color: colors.textMuted, textTransform: 'uppercase' }, prescription: { ...typography.headingMD, color: colors.textPrimary }, detail: { ...typography.caption, color: colors.textSecondary },
});
