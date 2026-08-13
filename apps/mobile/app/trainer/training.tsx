import { router } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useDuplicateTrainerTemplate, useTrainerTemplates } from '@/src/features/trainer/training/hooks';

export default function TrainerTrainingScreen() {
  const templates = useTrainerTemplates(); const duplicate = useDuplicateTrainerTemplate();
  if (templates.isLoading) return <LoadingView message="Carregando seus treinos…" />;
  if (templates.isError) return <ErrorView message={templates.error.message} onRetry={() => templates.refetch()} />;
  return <Screen style={styles.page}><TopBar eyebrow="PRESCRIÇÃO" title="Meus treinos" onBack={() => router.back()} /><Text style={styles.copy}>Modelos reutilizáveis com exercícios do catálogo.</Text><View style={styles.list}>{templates.data!.map((item) => <Card key={item.id} style={styles.item}><Text style={styles.itemTitle}>{item.name}</Text><Text style={styles.copy}>{item.exerciseCount ?? 0} exercícios</Text><View style={styles.actions}><Button variant="secondary" onPress={() => duplicate.mutate(item.id)}>Duplicar</Button><Button variant="ghost" onPress={() => router.push({ pathname: '/trainer/training/[id]', params: { id: item.id } })}>Editar</Button></View></Card>)}</View></Screen>;
}
const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary }, list: { gap: spacing.sm }, item: { gap: spacing.xs }, itemTitle: { ...typography.headingMD, color: colors.textPrimary }, actions: { flexDirection: 'row', gap: spacing.sm } });
