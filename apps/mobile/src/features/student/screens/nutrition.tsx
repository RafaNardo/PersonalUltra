import { router } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { StyleSheet, Text, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView, Tag } from '@/src/components/ui';
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
    <View style={styles.emptyHero}>
      <View style={styles.emptyMark}><Text style={styles.emptyMarkText}>✦</Text></View>
      <Tag tone="primary" style={styles.emptyStatus}>AGUARDANDO SEU PERSONAL</Tag>
      <Text style={styles.emptyTitle}>Sua alimentação também faz parte do processo.</Text>
      <Text style={styles.emptyCopy}>Seu cadastro está tudo certo. Quando o personal preparar suas orientações alimentares, elas aparecerão aqui de forma simples e organizada.</Text>
    </View>
    <Card style={styles.previewCard}>
      <Text style={styles.previewEyebrow}>NESTE ESPAÇO VOCÊ VERÁ</Text>
      <EmptyStateItem number="01" text="As refeições organizadas na sequência do seu dia." />
      <EmptyStateItem number="02" text="Os alimentos e as quantidades definidos para você." />
      <EmptyStateItem number="03" text="As observações deixadas pelo seu personal." />
    </Card>
    <View style={styles.emptyFooter}>
      <Text style={styles.footerCopy}>Enquanto isso, seus treinos e registros continuam disponíveis normalmente.</Text>
      <Button variant="secondary" onPress={() => router.replace('/student')}>Voltar ao início</Button>
    </View>
  </Screen>;
}

function EmptyStateItem({ number, text }: { number: string; text: string }) {
  return <View style={styles.previewItem}><View style={styles.previewNumber}><Text style={styles.previewNumberText}>{number}</Text></View><Text style={styles.previewText}>{text}</Text></View>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md },
  card: { gap: spacing.sm },
  title: { ...typography.headingMD, color: colors.textPrimary },
  copy: { ...typography.bodyMD, color: colors.textSecondary },
  food: { ...typography.bodyLG, color: colors.textPrimary },
  emptyPage: { paddingVertical: spacing.xl, gap: spacing.xxl },
  emptyHero: { alignItems: 'center', gap: spacing.md, paddingVertical: spacing.md },
  emptyMark: { width: 92, height: 92, borderRadius: 46, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.primary },
  emptyMarkText: { ...typography.displayLG, color: colors.background },
  emptyStatus: { alignSelf: 'center' },
  emptyTitle: { ...typography.displayLG, color: colors.textPrimary, textAlign: 'center' },
  emptyCopy: { ...typography.bodyLG, color: colors.textSecondary, lineHeight: 25, textAlign: 'center' },
  previewCard: { gap: spacing.md, backgroundColor: colors.surfaceElevated },
  previewEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 },
  previewItem: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, paddingVertical: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border },
  previewNumber: { width: 38, height: 38, borderRadius: 19, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.surface },
  previewNumberText: { ...typography.caption, color: colors.primary },
  previewText: { ...typography.bodyMD, color: colors.titaniumLight, lineHeight: 21, flex: 1 },
  emptyFooter: { gap: spacing.md },
  footerCopy: { ...typography.bodyMD, color: colors.textMuted, lineHeight: 21, textAlign: 'center' },
});
