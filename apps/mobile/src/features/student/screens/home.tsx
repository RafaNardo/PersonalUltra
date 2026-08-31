import { Redirect, router } from 'expo-router';
import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import Ionicons from '@expo/vector-icons/Ionicons';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

function formatDate(value: string) {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium' }).format(new Date(value));
}

type TrainingHistoryItem = { sessionId: string; workoutId: string; workoutName: string; status: string; startedAt: string; completedAt?: string; completedSets: number };
type CalendarCell = { key: string; day: number; date: Date; sessions: TrainingHistoryItem[] };

function localDateKey(value: string | Date) {
  const date = value instanceof Date ? value : new Date(value);
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

function eventDate(item: TrainingHistoryItem) {
  if (item.status === 'InProgress') return item.startedAt;
  if (item.status === 'Completed') return item.completedAt ?? item.startedAt;
  return undefined;
}

function monthTitle(date: Date) {
  return new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric' }).format(date);
}

function dateTitle(key: string) {
  const [year, month, day] = key.split('-').map(Number);
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'full' }).format(new Date(year, month - 1, day));
}

function buildCalendar(month: Date, history: TrainingHistoryItem[]) {
  const first = new Date(month.getFullYear(), month.getMonth(), 1);
  const offset = first.getDay();
  const totalDays = new Date(month.getFullYear(), month.getMonth() + 1, 0).getDate();
  const events = new Map<string, TrainingHistoryItem[]>();
  history.forEach((item) => {
    const factualDate = eventDate(item);
    if (!factualDate || Number.isNaN(new Date(factualDate).getTime())) return;
    const key = localDateKey(factualDate);
    const current = events.get(key) ?? [];
    current.push(item);
    events.set(key, current);
  });
  return Array.from({ length: Math.ceil((offset + totalDays) / 7) * 7 }, (_, index): CalendarCell | undefined => {
    const day = index - offset + 1;
    if (day < 1 || day > totalDays) return undefined;
    const date = new Date(month.getFullYear(), month.getMonth(), day);
    const key = localDateKey(date);
    return { key, day, date, sessions: events.get(key) ?? [] };
  });
}

function FactualCalendar({ history }: { history: TrainingHistoryItem[] }) {
  const factualHistory = useMemo(() => history.filter((item) => {
    const factualDate = eventDate(item);
    const date = factualDate ? new Date(factualDate) : undefined;
    return Boolean(date) && !Number.isNaN(date!.getTime()) && date!.getTime() <= Date.now();
  }), [history]);
  const now = new Date();
  const [month, setMonth] = useState(() => new Date(now.getFullYear(), now.getMonth(), 1));
  const [selectedDate, setSelectedDate] = useState(() => localDateKey(now));
  const cells = useMemo(() => buildCalendar(month, factualHistory), [month, factualHistory]);
  const selectedSessions = useMemo(() => {
    return factualHistory.filter((item) => localDateKey(eventDate(item)!) === selectedDate);
  }, [factualHistory, selectedDate]);
  const earliestEvent = factualHistory.reduce<Date | undefined>((earliest, item) => {
    const date = new Date(eventDate(item)!);
    return !earliest || date < earliest ? date : earliest;
  }, undefined);
  const earliestMonth = earliestEvent ? new Date(earliestEvent.getFullYear(), earliestEvent.getMonth(), 1) : month;
  const canGoPrevious = month.getTime() > earliestMonth.getTime();
  const canGoNext = month.getTime() < new Date(now.getFullYear(), now.getMonth(), 1).getTime();
  const shiftMonth = (amount: number) => {
    const next = new Date(month.getFullYear(), month.getMonth() + amount, 1);
    setMonth(next);
    setSelectedDate(localDateKey(next));
  };

  if (factualHistory.length === 0) return <Card style={styles.calendarEmpty}><Text style={styles.cardEyebrow}>ATIVIDADE RECENTE</Text><EmptyState variant="inline" status="SEM SESSÕES AINDA" symbol="●" title="Seu histórico começa quando você treinar." message="Os dias sem sessão ficam neutros. Escolha qualquer treino quando estiver pronto." /></Card>;

  return <View style={styles.section}>
    <View style={styles.calendarHeader}>
      <View style={styles.calendarHeading}><Text style={styles.sectionTitle}>Atividade recente</Text><Text style={styles.detail}>Uma visão dos meses alcançados pelas suas 20 sessões mais recentes — sem metas ou dias obrigatórios.</Text></View>
      <View style={styles.monthActions}>
        <Pressable accessibilityRole="button" accessibilityLabel="Mês anterior" accessibilityState={{ disabled: !canGoPrevious }} disabled={!canGoPrevious} onPress={() => shiftMonth(-1)} style={styles.monthButton}><Text style={[styles.monthButtonText, !canGoPrevious && styles.disabledText]}>‹</Text></Pressable>
        <Pressable accessibilityRole="button" accessibilityLabel="Próximo mês" accessibilityState={{ disabled: !canGoNext }} disabled={!canGoNext} onPress={() => shiftMonth(1)} style={styles.monthButton}><Text style={[styles.monthButtonText, !canGoNext && styles.disabledText]}>›</Text></Pressable>
      </View>
    </View>
    <Card style={styles.calendarCard}>
      <Text accessibilityRole="header" style={styles.monthTitle}>{monthTitle(month)}</Text>
      <View style={styles.weekLabels}>{['D', 'S', 'T', 'Q', 'Q', 'S', 'S'].map((label, index) => <Text key={`${label}-${index}`} style={styles.weekLabel}>{label}</Text>)}</View>
      <View style={styles.calendarGrid} accessibilityLabel={`Calendário de ${monthTitle(month)}`}>
        {cells.map((cell, index) => cell ? <Pressable key={cell.key} accessibilityRole="button" accessibilityLabel={`${cell.day} de ${monthTitle(month)}${cell.sessions.length ? `, ${cell.sessions.length} ${cell.sessions.length === 1 ? 'sessão registrada' : 'sessões registradas'}` : ', nenhum treino registrado'}`} accessibilityState={{ selected: selectedDate === cell.key }} onPress={() => setSelectedDate(cell.key)} style={[styles.dayCell, selectedDate === cell.key && styles.selectedDay, cell.sessions.length > 0 && styles.eventDay]}><Text style={[styles.dayText, selectedDate === cell.key && styles.selectedDayText]}>{cell.day}</Text>{cell.sessions.length > 0 ? <View accessible={false} style={styles.eventDots}><View style={styles.eventDot} />{cell.sessions.length > 1 ? <Text style={styles.eventCount}>{cell.sessions.length}</Text> : null}</View> : null}</Pressable> : <View key={`empty-${index}`} style={styles.dayCell} />)}
      </View>
    </Card>
    <Card style={styles.calendarDetails}>
      <Text style={styles.cardEyebrow}>{dateTitle(selectedDate)}</Text>
      {selectedSessions.length === 0 ? <Text style={styles.copy}>Nenhuma sessão registrada neste dia. Isso não representa atraso nem treino perdido.</Text> : selectedSessions.map((item) => <Pressable key={item.sessionId} accessibilityRole="button" accessibilityLabel={`${item.workoutName}, ${item.status === 'Completed' ? 'treino concluído' : 'sessão em andamento'}, ${item.completedSets} séries registradas`} accessibilityHint={item.status === 'Completed' ? 'Abre o resumo desta sessão' : 'Continua esta mesma sessão'} style={({ pressed }) => [styles.sessionItem, pressed && styles.pressed]} onPress={() => item.status === 'Completed' ? router.push({ pathname: '/student/training/summary/[sessionId]', params: { sessionId: item.sessionId } }) : router.push({ pathname: '/student/training/[id]', params: { id: item.workoutId, start: '1' } })}><View style={styles.sessionCopy}><Text style={styles.cardTitle}>{item.workoutName}</Text><Text style={styles.detail}>{item.status === 'Completed' ? `Concluído em ${formatDate(item.completedAt ?? item.startedAt)}` : `Em andamento desde ${formatDate(item.startedAt)}`} · {item.completedSets} séries</Text></View><Text style={styles.sessionAction}>{item.status === 'Completed' ? 'Ver resumo' : 'Continuar sessão'}</Text></Pressable>)}
    </Card>
  </View>;
}

export function StudentHomeScreen() {
  const session = useInviteSessionStore((state) => state.session);
  const clear = useInviteSessionStore((state) => state.clear);
  const queryClient = useQueryClient();
  const studentKey = session?.studentId;
  const training = useQuery({ queryKey: ['student', studentKey, 'training'], queryFn: () => inviteApi.training(session!.accessToken), enabled: Boolean(session) });
  const message = useQuery({ queryKey: ['student', studentKey, 'trainer-message'], queryFn: () => inviteApi.activeTrainerMessage(session!.accessToken), enabled: Boolean(session) });
  const nutrition = useQuery({ queryKey: ['student', studentKey, 'nutrition'], queryFn: () => inviteApi.nutrition(session!.accessToken), enabled: Boolean(session) });
  const weight = useQuery({ queryKey: ['student', studentKey, 'weight'], queryFn: () => inviteApi.weight(session!.accessToken), enabled: Boolean(session) });
  const hydration = useQuery({ queryKey: ['student', studentKey, 'hydration'], queryFn: () => inviteApi.hydration(session!.accessToken), enabled: Boolean(session) });
  const addHydration = useMutation({ mutationFn: (amountMl: number) => inviteApi.addHydration(session!.accessToken, amountMl), onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['student', studentKey, 'hydration'] }) });
  const branding = useQuery({ queryKey: ['student', studentKey, 'branding'], queryFn: () => inviteApi.branding(session!.accessToken), enabled: Boolean(session) });

  if (!session) return <Redirect href="/login" />;

  if (training.isLoading) return <LoadingView message="Abrindo seu dia…" />;
  if (training.isError) return <ErrorView message={training.error.message} onRetry={() => training.refetch()} />;

  const data = training.data!;
  const workouts = data.workouts;
  const history = data.history as TrainingHistoryItem[];
  const inProgress = history.find((item) => item.status === 'InProgress');
  const todayKey = localDateKey(new Date());
  const completedToday = history
    .filter((item) => item.status === 'Completed' && item.completedAt && !Number.isNaN(new Date(item.completedAt).getTime()) && localDateKey(item.completedAt) === todayKey)
    .sort((left, right) => new Date(right.completedAt!).getTime() - new Date(left.completedAt!).getTime())[0];
  const latestWeight = weight.data?.length ? weight.data[weight.data.length - 1] : undefined;
  const todayHydration = hydration.data?.filter((entry) => localDateKey(entry.recordedAt) === todayKey).reduce((total, entry) => total + entry.amountMl, 0) ?? 0;

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
        <Text style={styles.sectionTitle}>Seu treino de hoje</Text>
        <Card style={[styles.primaryCard, completedToday && !inProgress ? styles.completedTodayCard : undefined]}>
          {inProgress ? <>
            <View style={styles.contextualHeader}>
              <View accessible={false} style={styles.contextualIcon}><Ionicons name="play" size={22} color={colors.background} /></View>
              <View style={styles.cardTitleGroup}><Text style={styles.meta}>SESSÃO EM ANDAMENTO</Text><Text style={styles.primaryTitle}>{inProgress.workoutName}</Text></View>
            </View>
            <Text style={styles.copy}>Seu treino está salvo. Continue no seu ritmo, exatamente de onde parou.</Text>
            <Text style={styles.detail}>{inProgress.completedSets} {inProgress.completedSets === 1 ? 'série registrada' : 'séries registradas'}</Text>
            <Button accessibilityHint="Retoma esta sessão em andamento" onPress={() => router.push({ pathname: '/student/training/[id]', params: { id: inProgress.workoutId, start: '1' } })}>Continuar treino</Button>
          </> : completedToday ? <>
            <View style={styles.contextualHeader}>
              <View accessible={false} style={styles.completedIcon}><Ionicons name="trophy" size={25} color={colors.background} /></View>
              <View style={styles.cardTitleGroup}><Text style={styles.meta}>TREINO CONCLUÍDO</Text><Text style={styles.primaryTitle}>Você já concluiu um treino hoje</Text></View>
            </View>
            <Text style={styles.copy}>Mandou bem! Seu registro de {completedToday.workoutName} já faz parte da sua evolução.</Text>
            <Button variant="success" accessibilityHint="Abre o resumo do treino mais recente concluído hoje" onPress={() => router.push({ pathname: '/student/training/summary/[sessionId]', params: { sessionId: completedToday.sessionId } })}>Ver resumo do treino</Button>
          </> : <>
            <View style={styles.contextualHeader}>
              <View accessible={false} style={styles.contextualIcon}><Ionicons name="sparkles" size={22} color={colors.background} /></View>
              <View style={styles.cardTitleGroup}><Text style={styles.meta}>{workouts.length ? 'QUANDO ESTIVER PRONTO' : 'AGUARDANDO SEU PERSONAL'}</Text><Text style={styles.primaryTitle}>{workouts.length ? 'Seu próximo treino começa com a sua escolha.' : 'Seus treinos aparecerão aqui.'}</Text></View>
            </View>
            <Text style={styles.copy}>{workouts.length ? 'Escolha a opção que combina com o seu dia. A ordem do personal orienta, mas não limita você.' : 'Quando seu personal publicar sua rotina, você poderá escolher o treino que fizer sentido para o dia.'}</Text>
            {workouts.length ? <Button accessibilityHint="Abre a lista de treinos disponíveis" onPress={() => router.push('/student/training/start')}>Escolher treino</Button> : null}
          </>}
        </Card>
      </View>

      {history.length === 0 ? <Card style={styles.calendarEmpty}><Text style={styles.cardEyebrow}>SEUS TREINOS REALIZADOS</Text><EmptyState variant="inline" status="SEM SESSÕES AINDA" symbol="●" title="Seu histórico começa quando você treinar." message="Os dias sem sessão ficam neutros. Escolha qualquer treino quando estiver pronto." /></Card> : <FactualCalendar history={history} />}

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Seu acompanhamento</Text>
        <Card style={styles.card}>
          <Text style={styles.cardEyebrow}>MENSAGEM DO PERSONAL</Text>
          {message.isLoading ? <Text style={styles.copy}>Carregando mensagem…</Text> : message.isError ? <Text style={styles.copy}>Não foi possível carregar a mensagem agora.</Text> : message.data ? <><Text style={styles.message}>{message.data.message}</Text><Text style={styles.detail}>Enviada em {formatDate(message.data.startsAt)}</Text></> : <EmptyState variant="inline" status="SEM MENSAGEM ATIVA" symbol="●" title="Tudo certo por aqui." message="Quando seu personal enviar uma orientação, ela ficará em destaque neste espaço." />}
        </Card>

        <View style={styles.supportCards}>
          <Card style={styles.card}>
            <Text style={styles.cardEyebrow}>ALIMENTAÇÃO</Text>
            {nutrition.isLoading ? <Text style={styles.copy}>Carregando…</Text> : nutrition.isError ? <Text style={styles.copy}>Não foi possível carregar agora.</Text> : nutrition.data ? <><Text style={styles.cardTitle}>{nutrition.data.name}</Text><Text style={styles.copy}>{nutrition.data.meals.length} {nutrition.data.meals.length === 1 ? 'refeição cadastrada' : 'refeições cadastradas'}</Text>{nutrition.data.notes ? <Text style={styles.detail} numberOfLines={2}>{nutrition.data.notes}</Text> : null}</> : <EmptyState variant="inline" status="AGUARDANDO SEU PERSONAL" symbol="●" title="Seu plano alimentar aparecerá aqui." message="Você poderá consultar refeições e orientações assim que seu personal salvar o plano." />}
            <Button variant="ghost" onPress={() => router.push('/student/nutrition')}>Abrir alimentação</Button>
          </Card>
          <Card style={styles.card}>
            <Text style={styles.cardEyebrow}>PROGRESSO</Text>
            {weight.isLoading ? <Text style={styles.copy}>Carregando…</Text> : weight.isError ? <Text style={styles.copy}>Não foi possível carregar agora.</Text> : latestWeight ? <><Text style={styles.cardTitle}>{latestWeight.weightKg} kg</Text><Text style={styles.copy}>Último registro em {formatDate(latestWeight.recordedAt)}</Text><Text style={styles.detail}>{weight.data!.length} {weight.data!.length === 1 ? 'registro salvo' : 'registros salvos'}</Text></> : <EmptyState variant="inline" status="PRIMEIRO REGISTRO" symbol="+" title="Comece a acompanhar sua evolução." message="Registre seu peso para construir seu histórico ao longo do acompanhamento." />}
            <Button variant="ghost" onPress={() => router.push('/student/progress')}>Abrir progresso</Button>
          </Card>
          <Card style={styles.card}>
            <Text style={styles.cardEyebrow}>HIDRATAÇÃO</Text>
            {hydration.isLoading ? <Text style={styles.copy}>Carregando…</Text> : hydration.isError ? <Text style={styles.copy}>Não foi possível carregar agora.</Text> : <><Text style={styles.cardTitle}>{todayHydration >= 1000 ? `${(todayHydration / 1000).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} L` : `${todayHydration} ml`}</Text><Text style={styles.copy}>Registrado hoje</Text><View style={styles.hydrationActions}><Button variant="secondary" style={styles.hydrationAction} disabled={addHydration.isPending} onPress={() => addHydration.mutate(50)}>+50 ml</Button><Button variant="secondary" style={styles.hydrationAction} disabled={addHydration.isPending} onPress={() => addHydration.mutate(250)}>+250 ml</Button><Button variant="secondary" style={styles.hydrationAction} disabled={addHydration.isPending} onPress={() => addHydration.mutate(500)}>+500 ml</Button><Button variant="secondary" style={styles.hydrationAction} disabled={addHydration.isPending} onPress={() => addHydration.mutate(1000)}>+1 L</Button></View></>}
            <Button variant="ghost" onPress={() => router.push('/student/progress')}>Ver histórico de água</Button>
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
  completedTodayCard: { borderColor: colors.success },
  card: { gap: spacing.sm },
  cardHeader: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: spacing.sm },
  contextualHeader: { flexDirection: 'row', alignItems: 'center', gap: spacing.md },
  contextualIcon: { width: 46, height: 46, borderRadius: 23, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.primary },
  completedIcon: { width: 50, height: 50, borderRadius: 25, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.success },
  cardTitleGroup: { flex: 1, gap: spacing.xs },
  primaryTitle: { ...typography.headingLG, color: colors.textPrimary },
  cardTitle: { ...typography.headingMD, color: colors.textPrimary },
  cardEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 },
  meta: { ...typography.caption, color: colors.primary },
  detail: { ...typography.caption, color: colors.textMuted, lineHeight: 18 },
  message: { ...typography.bodyLG, color: colors.textPrimary, lineHeight: 24 },
  supportCards: { gap: spacing.sm },
  hydrationActions: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm },
  hydrationAction: { flexGrow: 1, flexBasis: 120, minHeight: 46, paddingHorizontal: spacing.md },
  calendarHeader: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: spacing.sm },
  calendarHeading: { flex: 1, gap: spacing.xxs },
  monthActions: { flexDirection: 'row', gap: spacing.xs },
  monthButton: { width: 40, height: 40, alignItems: 'center', justifyContent: 'center', borderWidth: 1, borderColor: colors.border, borderRadius: spacing.sm },
  monthButtonText: { fontSize: 28, lineHeight: 30, color: colors.primary },
  disabledText: { color: colors.textMuted },
  calendarCard: { gap: spacing.md },
  monthTitle: { ...typography.headingMD, color: colors.textPrimary, textTransform: 'capitalize' },
  weekLabels: { flexDirection: 'row' },
  weekLabel: { flex: 1, textAlign: 'center', ...typography.caption, color: colors.textMuted },
  calendarGrid: { flexDirection: 'row', flexWrap: 'wrap' },
  dayCell: { width: '14.2857%', minHeight: 48, alignItems: 'center', justifyContent: 'center', gap: 2, borderRadius: spacing.sm },
  selectedDay: { backgroundColor: colors.primary },
  eventDay: { borderWidth: 1, borderColor: colors.border },
  dayText: { ...typography.bodyMD, color: colors.textSecondary },
  selectedDayText: { color: colors.background, fontWeight: '700' },
  eventDots: { minHeight: 10, flexDirection: 'row', alignItems: 'center', gap: 2 },
  eventDot: { width: 6, height: 6, borderRadius: 3, backgroundColor: colors.primary },
  eventCount: { ...typography.caption, color: colors.primary, fontSize: 10 },
  calendarDetails: { gap: spacing.sm },
  sessionItem: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.sm, paddingVertical: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border },
  sessionCopy: { flex: 1, gap: spacing.xxs },
  sessionAction: { ...typography.caption, color: colors.primary, textAlign: 'right' },
  pressed: { opacity: .7 },
  calendarEmpty: { gap: spacing.sm },
});
