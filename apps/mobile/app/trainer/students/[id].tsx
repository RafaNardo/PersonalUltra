import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Alert, Linking, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useCreateTrainerMessage, useTrainerAnamnesis, useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useTrainerStudentWorkouts } from '@/src/features/trainer/training/hooks';
import { feedback } from '@/src/platform/feedback';
import { trainerClient } from '@/src/api/trainer-client';

type StudentSection = 'summary' | 'training' | 'progress';

export default function TrainerStudentDetailScreen() {
  const { id, section: initialSection } = useLocalSearchParams<{ id: string; section?: StudentSection }>();
  const [section, setSection] = useState<StudentSection>(initialSection ?? 'summary');
  const [message, setMessage] = useState('');
  const student = useTrainerStudent(id);
  const createMessage = useCreateTrainerMessage(id);
  const anamnesis = useTrainerAnamnesis(id, student.data?.anamnesisStatus === 'Completed');
  const workouts = useTrainerStudentWorkouts(id);
  const history = useQuery({ queryKey: ['trainer', 'students', id, 'training-history'], queryFn: () => trainerClient.trainingHistory(id!), enabled: Boolean(id) });
  const nutrition = useQuery({ queryKey: ['trainer', 'students', id, 'nutrition'], queryFn: () => trainerClient.nutrition(id!), enabled: Boolean(id) });
  const weight = useQuery({ queryKey: ['trainer', 'students', id, 'weight'], queryFn: () => trainerClient.weight(id!), enabled: Boolean(id) });

  useEffect(() => { if (initialSection) setSection(initialSection); }, [initialSection]);

  if (student.isLoading) return <LoadingView message="Carregando o aluno…" />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;

  const data = student.data!;
  const sendMessage = async () => {
    if (!message.trim()) return;
    try {
      await createMessage.mutateAsync(message.trim());
      setMessage('');
      feedback.success();
    } catch (error) {
      Alert.alert('Não foi possível enviar', error instanceof Error ? error.message : 'Tente novamente.');
    }
  };

  return <Screen style={styles.page}>
    <TopBar eyebrow="ALUNO" title={`${data.firstName} ${data.lastName}`} onBack={() => router.back()} />
    <Text style={styles.identityCopy}>{anamnesis.data?.goal ? `Objetivo: ${anamnesis.data.goal}` : 'Acompanhamento individual'}</Text>
    <View accessibilityRole="tablist" style={styles.tabs}>
      <StudentTab label="Resumo" selected={section === 'summary'} onPress={() => setSection('summary')} />
      <StudentTab label="Treinos" selected={section === 'training'} onPress={() => setSection('training')} />
      <StudentTab label="Evolução" selected={section === 'progress'} onPress={() => setSection('progress')} />
    </View>

    {section === 'summary' && <>
      <Card style={styles.card}>
        <Text style={styles.cardTitle}>Resumo</Text>
        <View style={styles.row}><Text style={styles.label}>E-mail</Text><Text style={styles.value}>{data.email ?? 'Não informado'}</Text></View>
        <View style={styles.row}><Text style={styles.label}>Telefone</Text><Text style={styles.value}>{data.phone ?? 'Não informado'}</Text></View>
        <View style={styles.row}><Text style={styles.label}>Aluno desde</Text><Text style={styles.value}>{new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium' }).format(new Date(data.startedAt))}</Text></View>
        {data.phone && <Button variant="secondary" onPress={() => void Linking.openURL(`https://wa.me/${data.phone!.replace(/\D/g, '')}`)}>Abrir conversa no WhatsApp</Button>}
      </Card>
      <Card style={styles.card}>
        <Text style={styles.cardTitle}>Anamnese</Text>
        <Tag tone={data.anamnesisStatus === 'Completed' ? 'success' : 'neutral'}>{anamnesisLabel(data.anamnesisStatus)}</Tag>
        <Text style={styles.copy}>{anamnesisCopy(data.anamnesisStatus)}</Text>
        {anamnesis.data && <View style={styles.anamnesis}><Detail label="Objetivo" value={anamnesis.data.goal} /><Detail label="Experiência" value={anamnesis.data.experienceLevel} /><Detail label="Rotina" value={`${anamnesis.data.trainingDaysPerWeek} dias · ${anamnesis.data.sessionDurationMinutes} min`} /><Detail label="Local" value={`${anamnesis.data.trainingLocation} · ${anamnesis.data.equipmentNotes}`} /><Detail label="Dados físicos" value={`${anamnesis.data.heightCm} cm · ${anamnesis.data.weightKg} kg`} /><Detail label="Cuidados" value={anamnesis.data.healthConditions} /><Detail label="Limitações" value={anamnesis.data.movementRestrictions} /><Detail label="Dor atual" value={anamnesis.data.currentPainDescription} /><Detail label="Nutrição" value={`${anamnesis.data.nutritionPreferences} · ${anamnesis.data.nutritionRestrictions}`} /></View>}
      </Card>
      <Card style={styles.card}>
        <Text style={styles.cardTitle}>Mensagem para {data.firstName}</Text>
        <Text style={styles.copy}>Ela aparecerá no acompanhamento do aluno.</Text>
        <TextInput value={message} onChangeText={setMessage} multiline maxLength={1000} placeholder="Ex.: Bora treinar hoje." placeholderTextColor={colors.textMuted} accessibilityLabel="Mensagem para o aluno" style={styles.input} />
        <Button loading={createMessage.isPending} disabled={!message.trim()} onPress={() => void sendMessage()}>Enviar mensagem</Button>
      </Card>
    </>}

    {section === 'training' && <>
      <View style={styles.sectionHeader}><View><Text style={styles.sectionTitle}>Treinos da semana</Text><Text style={styles.copy}>Prescrições disponíveis para {data.firstName}.</Text></View></View>
      <Button variant="secondary" onPress={() => router.push({ pathname: '/trainer/training/templates', params: { studentId: id! } })}>Aplicar a partir de um modelo</Button>
      {workouts.isLoading && <Card><Text style={styles.copy}>Carregando treinos…</Text></Card>}
      {workouts.isError && <Card style={styles.card}><Text style={styles.errorText}>Não foi possível carregar os treinos.</Text><Button variant="secondary" onPress={() => workouts.refetch()}>Tentar novamente</Button></Card>}
      {!workouts.isLoading && !workouts.isError && workouts.data?.length === 0 && <EmptyState title="Nenhum treino prescrito" message="Os treinos deste aluno aparecerão aqui quando forem disponibilizados." />}
      {workouts.data?.map((workout) => <Pressable key={workout.id} accessibilityRole="button" accessibilityLabel={`Abrir treino ${workout.name}`} accessibilityHint="Mostra os exercícios prescritos para este aluno" onPress={() => router.push({ pathname: '/trainer/students/[studentId]/workouts/[workoutId]', params: { studentId: id!, workoutId: workout.id } })} style={({ pressed }) => pressed && styles.pressed}>
        <Card style={styles.workoutCard}>
          <View style={styles.workoutHeader}><Text style={styles.workoutName}>{workout.name}</Text>{workout.isRecommended && <Tag tone="success">RECOMENDADO</Tag>}</View>
          <Text style={styles.workoutMeta}>{weekday(workout.recommendedDay)} · {workout.exerciseCount} {workout.exerciseCount === 1 ? 'exercício' : 'exercícios'}</Text>
          {workout.notes ? <Text numberOfLines={2} style={styles.copy}>{workout.notes}</Text> : null}
          <Text style={styles.openLink}>Abrir treino ›</Text>
        </Card>
      </Pressable>)}
      <Card style={styles.card}>
        <Text style={styles.cardTitle}>Histórico recente</Text>
        {history.isLoading && <Text style={styles.copy}>Carregando histórico…</Text>}
        {history.isError && <><Text style={styles.errorText}>Não foi possível carregar o histórico.</Text><Button variant="secondary" onPress={() => history.refetch()}>Tentar novamente</Button></>}
        {!history.isLoading && !history.isError && !history.data?.sessions.length && <Text style={styles.copy}>Nenhuma sessão registrada ainda.</Text>}
        {history.data?.sessions.slice(0, 5).map((item) => <View key={item.sessionId} style={styles.historySession}><Text style={styles.copy}>{item.workoutName} · {item.status === 'Completed' ? 'Concluído' : 'Em andamento'} · {item.completedSets} séries</Text>{item.exercises.flatMap((exercise) => exercise.sets.map((set) => <Text key={`${exercise.sequence}-${set.setNumber}`} style={styles.historySet}>{exercise.name} · série {set.setNumber}: {set.weightKg} kg × {set.repetitions} reps</Text>))}</View>)}
      </Card>
    </>}

    {section === 'progress' && <Card style={styles.card}>
      <Text style={styles.cardTitle}>Alimentação e progresso</Text>
      {nutrition.isLoading || weight.isLoading ? <Text style={styles.copy}>Carregando evolução…</Text> : <><Text style={styles.copy}>{nutrition.data?.name ?? 'Nenhum plano alimentar cadastrado.'}</Text><Text style={styles.copy}>{weight.data?.length ? `Último peso: ${weight.data.at(-1)?.weightKg} kg · ${weight.data.length} registros` : 'Nenhum registro de peso ainda.'}</Text></>}
      {(nutrition.isError || weight.isError) && <Text style={styles.errorText}>Alguns dados de evolução não puderam ser carregados.</Text>}
      <Button variant="secondary" onPress={() => router.push({ pathname: '/trainer/students/nutrition/[id]', params: { id: id! } })}>Editar alimentação</Button>
    </Card>}
  </Screen>;
}

function StudentTab({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
  return <Pressable accessibilityRole="tab" accessibilityState={{ selected }} onPress={onPress} style={[styles.tab, selected && styles.tabSelected]}><Text style={[styles.tabText, selected && styles.tabTextSelected]}>{label}</Text></Pressable>;
}

function weekday(day: number) {
  return ['Dia não definido', 'Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado', 'Domingo'][day] ?? 'Dia não definido';
}

function anamnesisLabel(status: 'NotStarted' | 'InProgress' | 'Completed') {
  return status === 'Completed' ? 'CONCLUÍDA' : status === 'InProgress' ? 'EM PREENCHIMENTO' : 'AGUARDANDO ANAMNESE';
}

function anamnesisCopy(status: 'NotStarted' | 'InProgress' | 'Completed') {
  return status === 'Completed' ? 'As informações da anamnese já estão disponíveis para consulta.' : status === 'InProgress' ? 'O aluno começou a preencher a anamnese.' : 'A anamnese ainda não foi iniciada pelo aluno.';
}

function Detail({ label, value }: { label: string; value: string }) { return <View style={styles.detail}><Text style={styles.label}>{label}</Text><Text style={styles.detailValue}>{value}</Text></View>; }

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, identityCopy: { ...typography.bodyMD, color: colors.textSecondary, marginTop: -spacing.sm }, tabs: { flexDirection: 'row', padding: spacing.xxs, gap: spacing.xxs, borderRadius: radius.md, backgroundColor: colors.surface }, tab: { flex: 1, alignItems: 'center', paddingVertical: spacing.sm, borderRadius: radius.sm }, tabSelected: { backgroundColor: colors.surfaceElevated }, tabText: { ...typography.caption, color: colors.textMuted }, tabTextSelected: { color: colors.primary }, card: { gap: spacing.md }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, row: { gap: spacing.xxs }, label: { ...typography.caption, color: colors.textMuted }, value: { ...typography.bodyLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, errorText: { ...typography.bodyMD, color: colors.danger }, anamnesis: { gap: spacing.sm }, detail: { gap: spacing.xxs }, detailValue: { ...typography.bodyMD, color: colors.textPrimary }, input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 112, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, padding: spacing.md, textAlignVertical: 'top' }, sectionHeader: { flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between', gap: spacing.md }, sectionTitle: { ...typography.headingMD, color: colors.textPrimary }, workoutCard: { gap: spacing.xs }, workoutHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.sm }, workoutName: { ...typography.headingMD, color: colors.textPrimary, flex: 1 }, workoutMeta: { ...typography.caption, color: colors.titanium }, openLink: { ...typography.caption, color: colors.primary, marginTop: spacing.xxs }, pressed: { opacity: .78 }, historySession: { gap: spacing.xxs, paddingTop: spacing.xs, borderTopWidth: 1, borderTopColor: colors.border }, historySet: { ...typography.caption, color: colors.titanium },
});
