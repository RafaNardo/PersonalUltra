import { router } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { StyleSheet, Text, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

export default function StudentTrainingScreen() {
  const session = useInviteSessionStore((s) => s.session); const training = useQuery({ queryKey: ['student', 'training'], queryFn: () => inviteApi.training(session!.accessToken), enabled: Boolean(session) });
  if (!session) { router.replace('/login'); return null; } if (training.isLoading) return <LoadingView message="Carregando seus treinos…" />; if (training.isError) return <ErrorView message={training.error.message} onRetry={() => training.refetch()} />;
  const items = [training.data!.recommended, ...training.data!.available].filter(Boolean);
  return <Screen style={styles.page}><TopBar eyebrow="SEUS TREINOS" title="Treinos disponíveis" onBack={() => router.back()} /><Text style={styles.copy}>Escolha uma sessão para começar. O treino recomendado aparece primeiro.</Text>{items.length === 0 ? <Card><Text style={styles.copy}>Seu personal ainda não liberou treinos.</Text></Card> : <View style={styles.list}>{items.map((item) => <Card key={item!.id} style={styles.card}><Text style={styles.title}>{item!.name}</Text><Text style={styles.meta}>{item!.exerciseCount} exercícios · Dia {item!.recommendedDay}</Text><Text style={styles.copy}>{item!.notes}</Text><Button onPress={() => router.push({ pathname: '/student-training/[id]', params: { id: item!.id } })}>{item!.isRecommended ? 'Começar recomendado' : 'Começar treino'}</Button></Card>)}</View>}<Card style={styles.history}><Text style={styles.title}>Histórico recente</Text>{training.data!.history.length === 0 ? <Text style={styles.copy}>Suas sessões concluídas aparecerão aqui.</Text> : training.data!.history.slice(0, 5).map((item) => <Text key={item.sessionId} style={styles.copy}>{item.workoutName} · {item.status === 'Completed' ? 'Concluído' : 'Em andamento'}</Text>)}</Card></Screen>;
}
const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, list: { gap: spacing.sm }, card: { gap: spacing.sm }, title: { ...typography.headingMD, color: colors.textPrimary }, meta: { ...typography.caption, color: colors.primary }, history: { gap: spacing.sm } });
