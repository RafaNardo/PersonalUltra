import { router } from 'expo-router';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useNutritionToday } from '@/src/api/hooks';
import { Card, EmptyState, ErrorView, LoadingView, ProgressBar, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';

export default function NutritionScreen() {
  const nutrition = useNutritionToday();
  if (nutrition.isLoading) return <LoadingView />;
  if (nutrition.error || !nutrition.data) return <ErrorView message="Não foi possível carregar sua alimentação." onRetry={() => void nutrition.refetch()} />;
  const data = nutrition.data;
  const completed = data.meals.filter((meal) => meal.completed).length;
  const hasMeals = data.meals.length > 0;
  return <Screen><TopBar eyebrow="Plano alimentar" title="Nutrição" />
    <Card style={styles.target}><View><Text style={styles.targetLabel}>META DIÁRIA</Text><Text style={styles.targetValue}>{data.caloriesTarget.toLocaleString('pt-BR')} <Text style={styles.targetUnit}>kcal</Text></Text></View><View style={styles.targetStatus}><Text style={styles.targetStatusValue}>{hasMeals ? `${completed}/${data.meals.length}` : '—'}</Text><Text style={styles.targetStatusLabel}>{hasMeals ? 'concluídas' : 'refeições'}</Text></View><View style={styles.targetBar}><ProgressBar value={hasMeals ? completed / data.meals.length : 0} /></View></Card>
    <View style={styles.macros}><Macro label="PROTEÍNA" value={`${data.proteinTarget}g`} tone="red" /><Macro label="CARBO" value={`${data.carbsTarget}g`} tone="neutral" /><Macro label="GORDURA" value={`${data.fatTarget}g`} tone="neutral" /></View>
    <View style={styles.section}><Text style={styles.sectionTitle}>REFEIÇÕES DE HOJE</Text><Text style={styles.sectionCopy}>Toque para ver alimentos e substituições.</Text></View>
    {hasMeals ? <View style={styles.meals}>{data.meals.map((meal, index) => <Pressable key={meal.id} accessibilityRole="button" accessibilityLabel={`Abrir ${meal.name}`} onPress={() => router.push(`/(app)/meal/${meal.id}`)}><Card style={styles.meal}><View style={[styles.mealIndex, meal.completed && styles.mealDone]}><Text style={styles.mealIndexText}>{meal.completed ? '✓' : index + 1}</Text></View><View style={styles.mealContent}><View style={styles.mealTitleRow}><Text style={styles.name}>{meal.name}</Text><Tag tone={meal.completed ? 'success' : 'neutral'}>{meal.completed ? 'CONCLUÍDA' : 'PENDENTE'}</Tag></View><Text style={styles.copy} numberOfLines={2}>{meal.foods.map((food) => food.name).join(' · ')}</Text></View><Text style={styles.chevron}>›</Text></Card></Pressable>)}</View> : <EmptyState title="Seu plano alimentar está sendo preparado" message="Quando as refeições estiverem disponíveis, elas aparecerão aqui." actionLabel="Atualizar" onAction={() => void nutrition.refetch()} />}
  </Screen>;
}

function Macro({ label, value, tone }: { label: string; value: string; tone: 'red' | 'neutral' }) { return <View style={styles.macro}><Text style={styles.macroLabel}>{label}</Text><Text style={[styles.macroValue, tone === 'red' && styles.macroRed]}>{value}</Text></View>; }

const styles = StyleSheet.create({
  target: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', gap: spacing.sm }, targetLabel: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, targetValue: { ...typography.displayLG, color: colors.textPrimary, marginTop: spacing.xs }, targetUnit: { ...typography.bodyLG, color: colors.textSecondary }, targetStatus: { marginLeft: 'auto', alignItems: 'flex-end' }, targetStatusValue: { ...typography.headingMD, color: colors.textPrimary }, targetStatusLabel: { ...typography.caption, color: colors.textMuted }, targetBar: { width: '100%', marginTop: spacing.xs },
  macros: { flexDirection: 'row', gap: spacing.sm }, macro: { flex: 1, padding: spacing.sm, borderRadius: radius.sm, backgroundColor: colors.surface }, macroLabel: { ...typography.caption, color: colors.textMuted, fontSize: 10 }, macroValue: { ...typography.headingMD, color: colors.textPrimary, marginTop: spacing.xs }, macroRed: { color: colors.primary }, section: { gap: spacing.xxs }, sectionTitle: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, sectionCopy: { ...typography.bodyMD, color: colors.textSecondary }, meals: { gap: spacing.sm }, meal: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, padding: spacing.md }, mealIndex: { width: 30, height: 30, borderRadius: 15, backgroundColor: colors.surfaceElevated, alignItems: 'center', justifyContent: 'center' }, mealDone: { backgroundColor: '#123D2B' }, mealIndexText: { ...typography.caption, color: colors.primary }, mealContent: { flex: 1, gap: spacing.xs }, mealTitleRow: { flexDirection: 'row', justifyContent: 'space-between', gap: spacing.xs, alignItems: 'center' }, name: { ...typography.headingMD, color: colors.textPrimary, flex: 1 }, copy: { ...typography.bodyMD, color: colors.textSecondary }, chevron: { ...typography.headingLG, color: colors.textMuted }, empty: { gap: spacing.sm }, emptyTitle: { ...typography.headingMD, color: colors.textPrimary }, emptyCopy: { ...typography.bodyMD, color: colors.textSecondary },
});
