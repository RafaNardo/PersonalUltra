import { Redirect, router } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { StyleSheet, Text, View } from 'react-native';
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
        {foods.length ? <View style={styles.foodList}>{foods.map((food) => <View key={food.id} style={styles.foodRow}><Text style={styles.food}>{food.foodName}</Text><Text style={styles.quantity}>{formatNutritionQuantity(food.quantity, food.unit)}</Text></View>)}</View> : <EmptyState variant="inline" status="ITENS EM PREPARAÇÃO" title="Esta refeição ainda não tem itens." message="Seu personal pode completar os alimentos e quantidades na próxima atualização." />}
      </Card>;
    })}
  </Screen>;
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
  foodRow: { flexDirection: 'row', alignItems: 'baseline', justifyContent: 'space-between', gap: spacing.md, paddingTop: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border },
  food: { ...typography.bodyLG, color: colors.textPrimary, flex: 1 },
  quantity: { ...typography.bodyMD, color: colors.titaniumLight, textAlign: 'right' },
  goalsCard: { gap: spacing.sm, borderColor: colors.primary },
  goalsCopy: { ...typography.bodyMD, color: colors.textSecondary },
  goalGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm, marginTop: spacing.xs },
  goal: { flexGrow: 1, flexBasis: '42%', padding: spacing.md, gap: spacing.xxs, borderRadius: 12, backgroundColor: colors.surfaceElevated },
  goalValue: { ...typography.headingMD, color: colors.textPrimary },
  goalLabel: { ...typography.caption, color: colors.titaniumLight },
  emptyPage: { paddingVertical: spacing.xl, gap: spacing.xxl },
});
