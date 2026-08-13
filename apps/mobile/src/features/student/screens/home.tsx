import { router } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

function formatDate(value: string) {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium' }).format(new Date(value));
}

export function StudentHomeScreen() {
  const session = useInviteSessionStore((state) => state.session);
  const clear = useInviteSessionStore((state) => state.clear);
  const studentKey = session?.studentId;
  const training = useQuery({ queryKey: ['student', studentKey, 'training'], queryFn: () => inviteApi.training(session!.accessToken), enabled: Boolean(session) });
  const message = useQuery({ queryKey: ['student', studentKey, 'trainer-message'], queryFn: () => inviteApi.activeTrainerMessage(session!.accessToken), enabled: Boolean(session) });
  const nutrition = useQuery({ queryKey: ['student', studentKey, 'nutrition'], queryFn: () => inviteApi.nutrition(session!.accessToken), enabled: Boolean(session) });
  const weight = useQuery({ queryKey: ['student', studentKey, 'weight'], queryFn: () => inviteApi.weight(session!.accessToken), enabled: Boolean(session) });
  const branding = useQuery({ queryKey: ['student', studentKey, 'branding'], queryFn: () => inviteApi.branding(session!.accessToken), enabled: Boolean(session) });

  if (!session) {
    router.replace('/login');
    return null;
  }

  if (training.isLoading) return <LoadingView message="Abrindo seu dia…" />;
  if (training.isError) return <ErrorView message={training.error.message} onRetry={() => training.refetch()} />;

  const data = training.data!;
  const workouts = data.workouts;
  const inProgress = data.history.find((item) => item.status === 'InProgress');
  const inProgressWorkout = (inProgress ? workouts.find((workout) => workout.id === inProgress.workoutId) : undefined)
    ?? workouts.find((workout) => workout.state === 'InProgress');
  const primaryWorkout = inProgressWorkout ?? workouts[0];
  const primaryTitle = inProgressWorkout?.name ?? primaryWorkout?.name;
  const latestWeight = weight.data?.length ? weight.data[weight.data.length - 1] : undefined;

  return (
    <Screen style={styles.page}>
      <View style={styles.header}>
        <View style={styles.headerCopy}>
          <Text style={styles.eyebrow}>PERSONAL ULTRA</Text>
          <Text style={styles.title}>Olá, {session.firstName}.</Text>
          <Text style={styles.copy}>{branding.data?.displayName ? `Seu acompanhamento com ${branding.data.displayName}.` : 'Seu dia de treino, alimentação e acompanhamento.'}</Text>
        </View>
        <Pressable accessibilityRole="button" accessibilityLabel="Trocar contexto" onPress={() => { clear(); router.replace('/demo-role-switch'); }}>
          <Text style={styles.contextAction}>Trocar contexto</Text>
        </Pressable>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>{inProgressWorkout ? 'Continue de onde parou' : 'Iniciar treino'}</Text>
        {primaryTitle ? (
          <Card style={styles.primaryCard}>
            <View style={styles.cardHeader}>
              <View style={styles.cardTitleGroup}>
                <Text style={styles.primaryTitle}>{primaryTitle}</Text>
                <Text style={styles.meta}>{inProgressWorkout ? 'Sessão em andamento' : 'Escolha um treino para hoje'}</Text>
              </View>
            </View>
            {!inProgressWorkout && primaryWorkout?.notes ? <Text style={styles.copy}>{primaryWorkout.notes}</Text> : null}
            {primaryWorkout ? <Text style={styles.detail}>{primaryWorkout.exerciseCount} {primaryWorkout.exerciseCount === 1 ? 'exercício prescrito' : 'exercícios prescritos'}{inProgress?.completedSets ? ` · ${inProgress.completedSets} séries registradas` : ''}</Text> : null}
            <Button onPress={() => inProgressWorkout
              ? router.push({ pathname: '/student/training/[id]', params: { id: inProgressWorkout.id, start: '1' } })
              : router.push('/student/training/start')}>{inProgressWorkout ? 'Continuar treino' : 'Escolher treino'}</Button>
          </Card>
        ) : (
          <EmptyState status={workouts.length ? 'TREINOS DISPONÍVEIS' : 'AGUARDANDO SEU PERSONAL'} symbol="●" title={workouts.length ? 'Escolha o treino que combina com seu dia.' : 'Seus treinos aparecerão aqui.'} message={workouts.length ? 'Seu personal liberou a rotina. Escolha uma sessão quando estiver pronto.' : 'Quando seu personal publicar a prescrição, você poderá escolher a sessão por aqui.'} actionLabel={workouts.length ? 'Escolher treino' : undefined} onAction={workouts.length ? () => router.push('/student/training/start') : undefined} />
        )}
      </View>
      <Button variant="secondary" onPress={() => router.push('/student/training/start')}>Ver todos os treinos</Button>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Seu acompanhamento</Text>
        <Card style={styles.card}>
          <Text style={styles.cardEyebrow}>MENSAGEM DO PERSONAL</Text>
          {message.isLoading ? <Text style={styles.copy}>Carregando mensagem…</Text> : message.isError ? <Text style={styles.copy}>Não foi possível carregar a mensagem agora.</Text> : message.data ? <><Text style={styles.message}>{message.data.message}</Text><Text style={styles.detail}>Enviada em {formatDate(message.data.startsAt)}</Text></> : <EmptyState variant="inline" status="SEM MENSAGEM ATIVA" symbol="●" title="Tudo certo por aqui." message="Quando seu personal enviar uma orientação, ela ficará em destaque neste espaço." />}
        </Card>

        <View style={styles.supportCards}>
          <Card style={styles.card}>
            <Text style={styles.cardEyebrow}>ALIMENTAÇÃO</Text>
            {nutrition.isLoading ? <Text style={styles.copy}>Carregando…</Text> : nutrition.isError ? <Text style={styles.copy}>Não foi possível carregar agora.</Text> : nutrition.data ? <><Text style={styles.cardTitle}>{nutrition.data.name}</Text><Text style={styles.copy}>{nutrition.data.meals.length} {nutrition.data.meals.length === 1 ? 'refeição cadastrada' : 'refeições cadastradas'}</Text>{nutrition.data.notes ? <Text style={styles.detail} numberOfLines={2}>{nutrition.data.notes}</Text> : null}</> : <EmptyState variant="inline" status="AGUARDANDO SEU PERSONAL" symbol="●" title="Seu plano alimentar aparecerá aqui." message="Você poderá consultar refeições e orientações assim que ele for publicado." />}
            <Button variant="ghost" onPress={() => router.push('/student/nutrition')}>Abrir alimentação</Button>
          </Card>
          <Card style={styles.card}>
            <Text style={styles.cardEyebrow}>PROGRESSO</Text>
            {weight.isLoading ? <Text style={styles.copy}>Carregando…</Text> : weight.isError ? <Text style={styles.copy}>Não foi possível carregar agora.</Text> : latestWeight ? <><Text style={styles.cardTitle}>{latestWeight.weightKg} kg</Text><Text style={styles.copy}>Último registro em {formatDate(latestWeight.recordedAt)}</Text><Text style={styles.detail}>{weight.data!.length} {weight.data!.length === 1 ? 'registro salvo' : 'registros salvos'}</Text></> : <EmptyState variant="inline" status="PRIMEIRO REGISTRO" symbol="+" title="Comece a acompanhar sua evolução." message="Registre seu peso para construir seu histórico ao longo do acompanhamento." />}
            <Button variant="ghost" onPress={() => router.push('/student/progress')}>Abrir progresso</Button>
          </Card>
        </View>
      </View>

      <Button variant="ghost" onPress={() => { clear(); router.replace('/login'); }}>Sair</Button>
    </Screen>
  );
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl },
  header: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: spacing.md },
  headerCopy: { flex: 1, gap: spacing.xs },
  eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1.2 },
  title: { ...typography.displayLG, color: colors.textPrimary },
  copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
  contextAction: { ...typography.caption, color: colors.primary, textAlign: 'right' },
  section: { gap: spacing.sm },
  sectionTitle: { ...typography.headingMD, color: colors.textPrimary },
  primaryCard: { gap: spacing.md, borderColor: colors.primary },
  card: { gap: spacing.sm },
  cardHeader: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: spacing.sm },
  cardTitleGroup: { flex: 1, gap: spacing.xs },
  primaryTitle: { ...typography.headingLG, color: colors.textPrimary },
  cardTitle: { ...typography.headingMD, color: colors.textPrimary },
  cardEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 },
  meta: { ...typography.caption, color: colors.primary },
  detail: { ...typography.caption, color: colors.textMuted, lineHeight: 18 },
  message: { ...typography.bodyLG, color: colors.textPrimary, lineHeight: 24 },
  supportCards: { gap: spacing.sm },
});
