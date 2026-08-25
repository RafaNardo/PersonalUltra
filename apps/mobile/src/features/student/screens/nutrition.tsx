import { Redirect, router } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { useState } from 'react';
import { Card, EmptyState, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { formatNutritionQuantity } from '@/src/shared/nutrition';

function formatUpdatedAt(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return 'Atualização recente';
  return `Atualizado em ${new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium' }).format(date)}`;
}

function goalValue(value: number | null | undefined, suffix: string) {
  return value == null ? null : `${new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 }).format(value)} ${suffix}`;
}

export function StudentNutritionScreen() {
  const session = useInviteSessionStore((state) => state.session);
  const query = useQuery({ queryKey: ['student', session?.studentId, 'nutrition'], queryFn: () => inviteApi.nutrition(session!.accessToken), enabled: Boolean(session) });
  if (!session) return <Redirect href="/login" />;
  if (query.isLoading) return <LoadingView message="Carregando sua alimentação…" />;
  if (query.isError) return <ErrorView message={query.error.message} onRetry={() => query.refetch()} />;
  if (!query.data) return <NutritionEmptyState />;
  const meals = [...query.data.meals].sort((left, right) => left.sequence - right.sequence);
  return <Screen style={styles.page}>
    <TopBar eyebrow="ALIMENTAÇÃO" title={query.data.name} />
    <View style={styles.planMeta}><Text style={styles.responsible}>Orientado por {query.data.responsibleTrainerName}</Text><Text style={styles.updatedAt}>{formatUpdatedAt(query.data.updatedAt)}</Text></View>
    {query.data.notes ? <Text style={styles.copy}>{query.data.notes}</Text> : null}
    {query.data.dailyGoals && [query.data.dailyGoals.calories, query.data.dailyGoals.proteinGrams, query.data.dailyGoals.carbohydratesGrams, query.data.dailyGoals.fatGrams].some((value) => value != null) ? <Card style={styles.goalsCard}><Text style={styles.sequence}>SUA REFERÊNCIA DIÁRIA</Text><Text style={styles.title}>Metas para o seu dia</Text><Text style={styles.goalsCopy}>Um passo de cada vez: consistência também é progresso.</Text><View style={styles.goalGrid}>{[[goalValue(query.data.dailyGoals.calories, 'kcal'), 'Calorias'], [goalValue(query.data.dailyGoals.proteinGrams, 'g'), 'Proteínas'], [goalValue(query.data.dailyGoals.carbohydratesGrams, 'g'), 'Carboidratos'], [goalValue(query.data.dailyGoals.fatGrams, 'g'), 'Gorduras']].filter(([value]) => value).map(([value, label]) => <View key={label} style={styles.goal}><Text style={styles.goalValue}>{value}</Text><Text style={styles.goalLabel}>{label}</Text></View>)}</View></Card> : null}
    {meals.length === 0 ? <EmptyState variant="section" status="PLANO EM PREPARAÇÃO" title="As refeições ainda serão organizadas." message="Seu personal já iniciou este plano. Quando as refeições forem salvas, elas aparecerão aqui." /> : meals.map((meal) => {
      const foods = [...meal.foods].sort((left, right) => left.sequence - right.sequence);
      return <Card key={meal.id} style={styles.card}>
        <Text style={styles.sequence}>REFEIÇÃO {meal.sequence}</Text>
        <Text style={styles.title}>{meal.name}</Text>
        {meal.notes ? <Text style={styles.copy}>{meal.notes}</Text> : null}
        {foods.length ? <View style={styles.foodList}>{foods.map((food) => <StudentFoodRow key={food.id} food={food} />)}</View> : <EmptyState variant="inline" status="ITENS EM PREPARAÇÃO" title="Esta refeição ainda não tem itens." message="Seu personal pode completar os alimentos e quantidades na próxima atualização." />}
      </Card>;
    })}
  </Screen>;
}

function StudentFoodRow({ food }: { food: { id: string; foodName: string; quantity: number; unit: import('@/src/features/student/invite/api').NutritionQuantityUnit; alternatives?: Array<{ id: string; foodName: string; quantity: number; unit: import('@/src/features/student/invite/api').NutritionQuantityUnit; sequence: number; notes?: string }> } }) {
  const [open, setOpen] = useState(false);
  const alternatives = [...(food.alternatives ?? [])].sort((left, right) => left.sequence - right.sequence);
  return <View style={styles.foodBlock}><View style={styles.foodRow}><Text style={styles.food}>{food.foodName}</Text><Text style={styles.quantity}>{formatNutritionQuantity(food.quantity, food.unit)}</Text></View>{alternatives.length ? <><Pressable accessibilityRole="button" accessibilityState={{ expanded: open }} onPress={() => setOpen((value) => !value)} style={styles.alternativesToggle}><Text style={styles.alternativesToggleText}>{open ? 'Ocultar alternativas' : 'Alternativas possíveis'} ({alternatives.length})</Text><Text style={styles.alternativesChevron}>{open ? '⌃' : '⌄'}</Text></Pressable>{open ? <View style={styles.alternatives}>{alternatives.map((alternative) => <View key={alternative.id} style={styles.alternative}><View style={styles.foodRow}><Text style={styles.alternativeName}>{alternative.foodName}</Text><Text style={styles.quantity}>{formatNutritionQuantity(alternative.quantity, alternative.unit)}</Text></View>{alternative.notes ? <Text style={styles.alternativeNotes}>{alternative.notes}</Text> : null}</View>)}</View> : null}</> : null}</View>;
}

function NutritionEmptyState() {
  return <Screen style={styles.emptyPage}>
    <TopBar eyebrow="ALIMENTAÇÃO" title="Seu plano alimentar" />
    <EmptyState variant="page" status="AGUARDANDO SEU PERSONAL" title="Sua alimentação também faz parte do processo." message="Seu cadastro está tudo certo. Quando o personal preparar suas orientações alimentares, elas aparecerão aqui de forma simples e organizada." items={['As refeições organizadas na sequência do seu dia.', 'Os alimentos e as quantidades definidos para você.', 'As observações deixadas pelo seu personal.']} footer="Enquanto isso, seus treinos e registros continuam disponíveis normalmente." actionLabel="Voltar ao início" onAction={() => router.replace('/student')} />
  </Screen>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md },
  card: { gap: spacing.sm },
  title: { ...typography.headingMD, color: colors.textPrimary },
  copy: { ...typography.bodyMD, color: colors.textSecondary },
  planMeta: { gap: spacing.xxs },
  responsible: { ...typography.bodyMD, color: colors.titaniumLight },
  updatedAt: { ...typography.caption, color: colors.textMuted },
  sequence: { ...typography.caption, color: colors.primary, letterSpacing: 1 },
  foodList: { gap: spacing.xs, marginTop: spacing.xs },
  foodBlock: { gap: spacing.xs },
  foodRow: { flexDirection: 'row', alignItems: 'baseline', justifyContent: 'space-between', gap: spacing.md, paddingTop: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border },
  food: { ...typography.bodyLG, color: colors.textPrimary, flex: 1 },
  quantity: { ...typography.bodyMD, color: colors.titaniumLight, textAlign: 'right' },
  alternativesToggle: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', paddingVertical: spacing.xs },
  alternativesToggleText: { ...typography.caption, color: colors.primary },
  alternativesChevron: { ...typography.bodyLG, color: colors.primary },
  alternatives: { gap: spacing.xs, paddingLeft: spacing.md, borderLeftWidth: 2, borderLeftColor: colors.primary },
  alternative: { gap: spacing.xxs, paddingVertical: spacing.xs },
  alternativeName: { ...typography.bodyMD, color: colors.textPrimary, flex: 1 },
  alternativeNotes: { ...typography.caption, color: colors.textSecondary },
  goalsCard: { gap: spacing.sm, borderColor: colors.primary },
  goalsCopy: { ...typography.bodyMD, color: colors.textSecondary },
  goalGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm, marginTop: spacing.xs },
  goal: { flexGrow: 1, flexBasis: '42%', padding: spacing.md, gap: spacing.xxs, borderRadius: 12, backgroundColor: colors.surfaceElevated },
  goalValue: { ...typography.headingMD, color: colors.textPrimary },
  goalLabel: { ...typography.caption, color: colors.titaniumLight },
  emptyPage: { paddingVertical: spacing.xl, gap: spacing.xxl },
});
