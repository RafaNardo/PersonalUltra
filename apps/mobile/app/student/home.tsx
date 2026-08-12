import { router } from 'expo-router';
import { useEffect } from 'react';
import { Image, Pressable, StyleSheet, Text, View } from 'react-native';
import { useHome, useNutritionToday, useTrainingPlan, useTrainingToday, syncPendingSetOperations } from '@/src/api/hooks';
import type { TrainingPlanWorkout } from '@/src/api/types';
import { Card, ErrorView, LoadingView, ProgressBar, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { exerciseImage } from '@/src/design/exercise-media';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useAuthStore } from '@/src/state/auth-store';

export default function HomeScreen() {
  const home = useHome(); const today = useTrainingToday(); const nutrition = useNutritionToday(); const trainingPlan = useTrainingPlan();
  const signOut = useAuthStore((state) => state.signOut);
  useEffect(() => { void syncPendingSetOperations(); }, []);
  if (home.isLoading || today.isLoading || trainingPlan.isLoading) return <LoadingView message="Preparando seu dia…" />;
  if (home.error || today.error || trainingPlan.error) return <ErrorView message={(home.error ?? today.error ?? trainingPlan.error)?.message ?? 'Tente novamente.'} onRetry={() => { void home.refetch(); void today.refetch(); void trainingPlan.refetch(); }} />;
  if (!home.data || !today.data || !trainingPlan.data) return <ErrorView message="Ainda não foi possível preparar seu dia." onRetry={() => { void home.refetch(); void today.refetch(); void trainingPlan.refetch(); }} />;
  const workout = today.data; const isDone = workout.status === 'Completed';
  const planned = workout.exercises.reduce((total, item) => total + item.prescribedSets, 0);
  const completed = workout.exercises.reduce((total, item) => total + item.completedSets, 0);
  const image = exerciseImage(workout.exercises[0]?.name ?? '');
  const mealsDone = nutrition.data?.meals.filter((meal) => meal.completed).length ?? 0;
  const mealsTotal = nutrition.data?.meals.length ?? 0;

  return <Screen>
    <TopBar eyebrow="SVR Method" title={home.data.greeting} action={<Pressable onPress={signOut}><Text style={styles.exit}>Sair</Text></Pressable>} />
    <WeeklySchedule workouts={trainingPlan.data.workouts} />
    <Card style={styles.hero}>{image && <Image source={image} style={styles.heroImage} resizeMode="cover" />}<View style={styles.heroShade} /><View style={styles.heroContent}><Tag tone={isDone ? 'success' : 'primary'}>{isDone ? 'TREINO CONCLUÍDO' : 'TREINO DE HOJE'}</Tag><Text style={styles.heroTitle}>{workout.name}</Text><Text style={styles.heroCopy}>{workout.exercises.map((item) => item.primaryMuscleGroup).filter((value, index, list) => list.indexOf(value) === index).join(' · ')}</Text><View style={styles.heroProgress}><Text style={styles.progressText}>{completed}/{planned} séries</Text><ProgressBar value={planned ? completed / planned : 0} /></View><Pressable disabled={isDone} onPress={() => router.push(`/student/workout/${workout.id}`)} style={[styles.heroAction, isDone && styles.disabled]}><Text style={styles.heroActionText}>{isDone ? 'Treino concluído' : workout.status === 'InProgress' ? 'Continuar treino' : 'Começar treino'}</Text><Text style={styles.arrow}>→</Text></Pressable></View></Card>
    <View style={styles.metrics}><Card style={styles.metric}><Text style={styles.metricValue}>{home.data.completedWorkoutsThisWeek}</Text><Text style={styles.metricLabel}>treinos concluídos nesta semana</Text></Card><Pressable accessibilityRole="button" accessibilityLabel="Ver plano de treino" onPress={() => router.push('/student/training')} style={styles.planMetric}><Card style={styles.metric}><Text style={styles.metricLink}>Ver plano</Text><Text style={styles.metricLabel}>{trainingPlan.data.workouts.length} sessões prescritas</Text></Card></Pressable></View>
    <Pressable onPress={() => router.push('/student/coach')}><Card style={styles.coach}><View style={styles.coachIcon}><Text style={styles.coachIconText}>✦</Text></View><View style={styles.coachBody}><Text style={styles.coachLabel}>SVR COACH</Text><Text style={styles.coachTitle}>Sua evolução vem da repetição bem feita.</Text><Text style={styles.coachLink}>Ver recomendação →</Text></View></Card></Pressable>
    <Pressable onPress={() => router.push('/student/nutrition')}><Card style={styles.nutrition}><View><Text style={styles.nutritionLabel}>NUTRIÇÃO · HOJE</Text><Text style={styles.nutritionTitle}>{nutrition.data ? `${nutrition.data.caloriesTarget.toLocaleString('pt-BR')} kcal` : 'Plano alimentar'}</Text></View><View style={styles.nutritionRight}><Text style={styles.nutritionValue}>{mealsTotal ? `${mealsDone}/${mealsTotal}` : '—'}</Text><Text style={styles.nutritionSmall}>refeições</Text></View><View style={styles.nutritionBar}><ProgressBar value={mealsTotal ? mealsDone / mealsTotal : 0} /></View></Card></Pressable>
    <Pressable onPress={() => router.push('/student/progress')}><Text style={styles.footerLink}>Ver meu progresso <Text style={styles.arrow}>→</Text></Text></Pressable>
  </Screen>;
}

function WeeklySchedule({ workouts }: { workouts: TrainingPlanWorkout[] }) {
  const slots = [workouts[0], workouts[1], undefined, workouts[2], workouts[3], undefined, undefined];
  const days = ['SEG', 'TER', 'QUA', 'QUI', 'SEX', 'SÁB', 'DOM'];
  const today = (new Date().getDay() + 6) % 7;
  return <Card style={styles.schedule}><View style={styles.scheduleHeader}><View><Text style={styles.weekLabel}>CRONOGRAMA DA SEMANA</Text><Text style={styles.scheduleCopy}>Seu ritmo planejado.</Text></View><Pressable accessibilityRole="button" accessibilityLabel="Ver todos os treinos" onPress={() => router.push('/student/training')}><Text style={styles.scheduleLink}>Ver plano →</Text></Pressable></View><View style={styles.scheduleList}>{slots.map((workout, index) => <View key={days[index]} style={[styles.scheduleRow, index === today && styles.scheduleToday]}><Text style={styles.scheduleDay}>{days[index]}</Text><Text numberOfLines={1} style={[styles.scheduleWorkout, !workout && styles.scheduleRest]}>{workout?.name ?? 'Descanso'}</Text>{index === today && <Text style={styles.scheduleTodayLabel}>HOJE</Text>}</View>)}</View></Card>;
}

const styles = StyleSheet.create({
  exit: { ...typography.bodyMD, color: colors.textSecondary, paddingTop: spacing.xs }, weekLabel: { ...typography.caption, color: colors.textMuted, letterSpacing: 1 }, schedule: { gap: spacing.md, padding: spacing.md }, scheduleHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', gap: spacing.sm }, scheduleCopy: { ...typography.bodyMD, color: colors.textSecondary, marginTop: spacing.xxs }, scheduleLink: { ...typography.caption, color: colors.primary, paddingTop: spacing.xxs }, scheduleList: { gap: spacing.xs }, scheduleRow: { minHeight: 30, flexDirection: 'row', alignItems: 'center', gap: spacing.sm, paddingHorizontal: spacing.sm, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, scheduleToday: { backgroundColor: '#42121A', borderWidth: 1, borderColor: colors.primary }, scheduleDay: { width: 30, ...typography.caption, color: colors.textMuted }, scheduleWorkout: { flex: 1, ...typography.caption, color: colors.textPrimary }, scheduleRest: { color: colors.textMuted }, scheduleTodayLabel: { ...typography.caption, color: colors.primary, fontSize: 10 },
  hero: { minHeight: 294, overflow: 'hidden', padding: 0, backgroundColor: colors.surfaceElevated }, heroImage: { ...StyleSheet.absoluteFillObject, opacity: .48 }, heroShade: { ...StyleSheet.absoluteFillObject, backgroundColor: 'rgba(0, 0, 0, .46)' }, heroContent: { flex: 1, justifyContent: 'flex-end', gap: spacing.sm, padding: spacing.lg }, heroTitle: { ...typography.displayLG, color: colors.textPrimary }, heroCopy: { ...typography.bodyMD, color: colors.textSecondary }, heroProgress: { gap: spacing.xs, marginTop: spacing.xs }, progressText: { ...typography.caption, color: colors.textPrimary }, heroAction: { marginTop: spacing.sm, minHeight: 50, borderRadius: radius.sm, backgroundColor: colors.primary, paddingHorizontal: spacing.lg, flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }, heroActionText: { ...typography.bodyLG, color: colors.textPrimary, fontFamily: 'MontserratBold' }, arrow: { color: colors.primary, fontSize: 20 }, disabled: { opacity: .55 },
  metrics: { flexDirection: 'row', gap: spacing.md }, metric: { flex: 1, gap: spacing.xs, minHeight: 106, justifyContent: 'space-between' }, planMetric: { flex: 1 }, metricValue: { ...typography.metricXL, color: colors.textPrimary }, metricLink: { ...typography.headingMD, color: colors.primary }, metricLabel: { ...typography.caption, color: colors.textSecondary, textTransform: 'uppercase' },
  coach: { flexDirection: 'row', gap: spacing.md, padding: spacing.md }, coachIcon: { width: 46, height: 46, borderRadius: 23, alignItems: 'center', justifyContent: 'center', backgroundColor: '#42121A' }, coachIconText: { color: colors.primary, fontSize: 22 }, coachBody: { flex: 1, gap: spacing.xxs }, coachLabel: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, coachTitle: { ...typography.bodyLG, color: colors.textPrimary }, coachLink: { ...typography.caption, color: colors.primary, marginTop: spacing.xs },
  nutrition: { flexDirection: 'row', alignItems: 'center', flexWrap: 'wrap', gap: spacing.sm }, nutritionLabel: { ...typography.caption, color: colors.textMuted, letterSpacing: .8 }, nutritionTitle: { ...typography.headingMD, color: colors.textPrimary, marginTop: spacing.xxs }, nutritionRight: { marginLeft: 'auto', alignItems: 'flex-end' }, nutritionValue: { ...typography.headingMD, color: colors.textPrimary }, nutritionSmall: { ...typography.caption, color: colors.textSecondary }, nutritionBar: { width: '100%' }, footerLink: { ...typography.bodyLG, color: colors.primary, textAlign: 'center', fontFamily: 'MontserratBold' },
});
