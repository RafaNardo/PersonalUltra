import { router } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { StyleSheet, Text } from 'react-native';
import { Card, EmptyState, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

export function StudentNutritionScreen() {
  const session = useInviteSessionStore((state) => state.session);
  const query = useQuery({ queryKey: ['student', session?.studentId, 'nutrition'], queryFn: () => inviteApi.nutrition(session!.accessToken), enabled: Boolean(session) });
  if (!session) { router.replace('/login'); return null; }
  if (query.isLoading) return <LoadingView message="Carregando sua alimentação…" />;
  if (query.isError) return <ErrorView message={query.error.message} onRetry={() => query.refetch()} />;
  if (!query.data) return <NutritionEmptyState />;
  return <Screen style={styles.page}><TopBar eyebrow="ALIMENTAÇÃO" title={query.data.name} /><Text style={styles.copy}>{query.data.notes}</Text>{query.data.meals.map((meal) => <Card key={meal.id} style={styles.card}><Text style={styles.title}>{meal.sequence}. {meal.name}</Text>{meal.notes && <Text style={styles.copy}>{meal.notes}</Text>}{meal.foods.map((food) => <Text key={food.foodName} style={styles.food}>{food.foodName} · {food.quantityGrams} g</Text>)}</Card>)}</Screen>;
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
  food: { ...typography.bodyLG, color: colors.textPrimary },
  emptyPage: { paddingVertical: spacing.xl, gap: spacing.xxl },
});
