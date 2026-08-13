import { router } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { StyleSheet, Text } from 'react-native';
import { Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

export function StudentNutritionScreen() {
  const session = useInviteSessionStore((state) => state.session);
  const query = useQuery({ queryKey: ['student', 'nutrition'], queryFn: () => inviteApi.nutrition(session!.accessToken), enabled: Boolean(session) });
  if (!session) { router.replace('/login'); return null; }
  if (query.isLoading) return <LoadingView message="Carregando sua alimentação…" />;
  if (query.isError) return <ErrorView message={query.error.message} onRetry={() => query.refetch()} />;
  return <Screen style={styles.page}><TopBar eyebrow="ALIMENTAÇÃO" title={query.data?.name ?? 'Seu plano alimentar'} onBack={() => router.back()} /><Text style={styles.copy}>{query.data?.notes ?? 'Seu personal ainda não liberou um plano alimentar.'}</Text>{query.data?.meals.map((meal) => <Card key={meal.id} style={styles.card}><Text style={styles.title}>{meal.sequence}. {meal.name}</Text>{meal.notes && <Text style={styles.copy}>{meal.notes}</Text>}{meal.foods.map((food) => <Text key={food.foodName} style={styles.food}>{food.foodName} · {food.quantityGrams} g</Text>)}</Card>)}</Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.md }, card: { gap: spacing.sm }, title: { ...typography.headingMD, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary }, food: { ...typography.bodyLG, color: colors.textPrimary } });
