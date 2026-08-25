import { Redirect, router } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, ListItem, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentWorkout } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

/**
 * Preparation is deliberately read-only. Selecting a row opens a preview;
 * the session is created only after the explicit action in that preview.
 */
export function StudentTrainingScreen({ withinTabs = false }: { withinTabs?: boolean }) {
  const session = useInviteSessionStore((state) => state.session);
  const training = useQuery({
    queryKey: ['student', session?.studentId, 'training'],
    queryFn: () => inviteApi.training(session!.accessToken),
    enabled: Boolean(session),
  });

  if (!session) return <Redirect href="/login" />;
  if (training.isLoading) return <LoadingView message="Preparando seus treinos…" />;
  if (training.isError) return <ErrorView message={training.error.message} onRetry={() => training.refetch()} />;

  const workouts = training.data?.workouts ?? [];
  const activeSession = training.data?.history.find((item) => item.status === 'InProgress');
  const active = (activeSession ? workouts.find((workout) => workout.id === activeSession.workoutId) : undefined)
    ?? workouts.find((workout) => workout.state === 'InProgress');

  const visibleHistory = training.data!.history.filter((item) => item.status === 'Completed' || item.status === 'InProgress');

  return <Screen withinTabs={withinTabs} style={styles.page}>
    <TopBar eyebrow="PREPARAÇÃO" title="Escolha seu treino" onBack={withinTabs ? undefined : () => router.back()} />
    <Text style={styles.intro}>Escolha o treino que faz sentido para hoje. A ordem foi organizada pelo seu personal, mas você decide quando começar.</Text>
    {activeSession ? <Card style={styles.activeCard}>
      <Text style={styles.activeEyebrow}>SESSÃO EM ANDAMENTO</Text>
      <Text style={styles.activeTitle}>{active?.name ?? activeSession.workoutName}</Text>
      <Text style={styles.copy}>Você pode continuar de onde parou.</Text>
      <Button onPress={() => router.replace({ pathname: '/student/training/[id]', params: { id: activeSession.workoutId, start: '1' } })}>Continuar treino</Button>
    </Card> : null}
    {workouts.length === 0 ? <EmptyState status="AGUARDANDO PRESCRIÇÃO" symbol="●" title="Seu personal ainda não liberou treinos." message="Quando a rotina estiver disponível, você poderá escolher qualquer treino por aqui." /> : <View style={styles.list}>
      {workouts.map((workout) => <WorkoutPreparationItem key={workout.id} workout={workout} onPress={() => router.push({ pathname: '/student/training/preview/[id]', params: { id: workout.id } })} />)}
    </View>}
    <Card style={styles.noteCard}><Text style={styles.noteTitle}>Como funciona</Text><Text style={styles.copy}>A prévia mostra a prescrição sem iniciar uma sessão. O treino começa somente quando você confirmar.</Text></Card>
    <Card style={styles.history}><Text style={styles.noteTitle}>Histórico recente</Text>{visibleHistory.length === 0 ? <EmptyState variant="inline" status="PRIMEIRA SESSÃO" title="Seu histórico começa no primeiro treino." message="As sessões e registros realizados aparecerão neste espaço." /> : visibleHistory.slice(0, 5).map((item) => <Pressable key={item.sessionId} accessibilityRole="button" accessibilityLabel={`Abrir resumo de ${item.workoutName}`} style={({ pressed }) => [styles.historyItem, pressed && styles.pressed]} onPress={() => item.status === 'Completed' ? router.push({ pathname: '/student/training/summary/[sessionId]', params: { sessionId: item.sessionId } }) : router.push({ pathname: '/student/training/[id]', params: { id: item.workoutId, start: '1' } })}><Text style={styles.copy}>{item.workoutName} · {item.status === 'Completed' ? 'Concluído' : 'Em andamento'}</Text><Text style={styles.historyAction}>{item.status === 'Completed' ? 'Ver resumo' : 'Continuar sessão'}</Text></Pressable>)}</Card>
  </Screen>;
}

function WorkoutPreparationItem({ workout, onPress }: { workout: StudentWorkout; onPress: () => void }) {
  const status = workout.state === 'InProgress' ? 'Em andamento' : workout.state === 'Completed' ? 'Concluído anteriormente' : undefined;
  const lastExecution = workout.lastCompletedAt ? `Última execução: ${formatDate(workout.lastCompletedAt)}` : 'Ainda não executado';
  return <ListItem
    title={workout.name}
    metadata={`${workout.exerciseCount} ${workout.exerciseCount === 1 ? 'exercício' : 'exercícios'} · ${workout.prescribedSets} ${workout.prescribedSets === 1 ? 'série' : 'séries'}`}
    description={[status, lastExecution, workout.notes || undefined].filter(Boolean).join(' · ')}
    actionLabel="Ver treino"
    onPress={onPress}
    accessibilityLabel={`Ver treino ${workout.name}`}
    accessibilityHint="Abre a preparação sem iniciar uma sessão"
  />;
}

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'data não disponível' : new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' }).format(date);
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.lg },
  intro: { ...typography.bodyLG, color: colors.titaniumLight, lineHeight: 24 },
  list: { gap: spacing.xs },
  activeCard: { gap: spacing.xs, borderColor: colors.primary },
  activeEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 },
  activeTitle: { ...typography.headingMD, color: colors.textPrimary },
  copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
  noteCard: { gap: spacing.xs },
  noteTitle: { ...typography.headingMD, color: colors.textPrimary },
  history: { gap: spacing.sm },
  historyItem: { gap: spacing.xxs, paddingVertical: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border },
  historyAction: { ...typography.caption, color: colors.primary },
  pressed: { opacity: .75 },
});
