import { router } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentWorkout } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

export function StudentTrainingScreen() {
  const session = useInviteSessionStore((s) => s.session); const training = useQuery({ queryKey: ['student', session?.studentId, 'training'], queryFn: () => inviteApi.training(session!.accessToken), enabled: Boolean(session) });
  if (!session) { router.replace('/login'); return null; } if (training.isLoading) return <LoadingView message="Carregando seus treinos…" />; if (training.isError) return <ErrorView message={training.error.message} onRetry={() => training.refetch()} />;
  const items = [training.data!.recommended, ...training.data!.available].filter((item): item is StudentWorkout => Boolean(item));
  return <Screen style={styles.page}><TopBar eyebrow="SEUS TREINOS" title="Treinos" onBack={() => router.back()} /><Text style={styles.copy}>Veja o treino recomendado, continue uma sessão em andamento ou escolha uma alternativa. Abrir os detalhes não inicia o treino.</Text>{items.length === 0 ? <Card><Text style={styles.copy}>Seu personal ainda não liberou treinos.</Text></Card> : <View style={styles.list}>{items.map((item) => <WorkoutCard key={item.id} workout={item} />)}</View>}<Card style={styles.history}><Text style={styles.title}>Histórico recente</Text>{training.data!.history.length === 0 ? <Text style={styles.copy}>Suas sessões concluídas aparecerão aqui.</Text> : training.data!.history.slice(0, 5).map((item) => <Pressable key={item.sessionId} accessibilityRole="button" accessibilityLabel={`Abrir resumo de ${item.workoutName}`} style={({ pressed }) => [styles.historyItem, pressed && styles.pressed]} onPress={() => item.status === 'Completed' ? router.push({ pathname: '/student/training/summary/[sessionId]', params: { sessionId: item.sessionId } }) : router.push({ pathname: '/student/training/[id]', params: { id: item.workoutId, start: '1' } })}><Text style={styles.copy}>{item.workoutName} · {item.status === 'Completed' ? 'Concluído' : 'Em andamento'}</Text><Text style={styles.historyAction}>{item.status === 'Completed' ? 'Ver resumo' : 'Continuar sessão'}</Text></Pressable>)}</Card></Screen>;
}

function WorkoutCard({ workout }: { workout: StudentWorkout }) {
  const state = workout.state === 'Recommended' ? { label: 'Recomendado', tone: 'primary' as const } : workout.state === 'InProgress' ? { label: 'Em andamento', tone: 'success' as const } : workout.state === 'Completed' ? { label: 'Concluído', tone: 'success' as const } : { label: 'Disponível', tone: 'neutral' as const };
  return <Card style={styles.card}><View style={styles.cardHeader}><Text style={styles.title}>{workout.name}</Text><Tag tone={state.tone}>{state.label}</Tag></View><Text style={styles.meta}>{workout.exerciseCount} exercícios · {workout.prescribedSets} séries · Dia {workout.recommendedDay}</Text>{workout.notes ? <Text style={styles.copy}>{workout.notes}</Text> : null}<Button onPress={() => router.push({ pathname: '/student/training/preview/[id]', params: { id: workout.id } })}>{workout.state === 'InProgress' ? 'Continuar treino' : workout.state === 'Completed' ? 'Ver treino e iniciar novamente' : workout.isRecommended ? 'Ver treino recomendado' : 'Ver treino'}</Button></Card>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, list: { gap: spacing.sm }, card: { gap: spacing.sm }, cardHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', gap: spacing.sm }, title: { ...typography.headingMD, color: colors.textPrimary, flex: 1 }, meta: { ...typography.caption, color: colors.primary }, history: { gap: spacing.sm }, historyItem: { gap: spacing.xxs, paddingVertical: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border }, historyAction: { ...typography.caption, color: colors.primary }, pressed: { opacity: .75 } });
