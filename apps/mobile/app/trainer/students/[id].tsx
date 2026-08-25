import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Alert, Linking, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useCreateTrainerMessage, useTrainerAnamnesis, useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useReorderTrainerStudentWorkouts, useTrainerStudentWorkouts } from '@/src/features/trainer/training/hooks';
import { feedback } from '@/src/platform/feedback';
import { trainerClient } from '@/src/api/trainer-client';
import type { TrainerNutrition, TrainerNutritionMeal } from '@/src/api/trainer-client';
import { useSaveTrainerNutrition, useTrainerNutrition } from '@/src/features/trainer/nutrition/hooks';
import { formatNutritionQuantity } from '@/src/shared/nutrition';

type StudentSection = 'summary' | 'training' | 'nutrition';

export default function TrainerStudentDetailScreen() {
  const { id, section: initialSection } = useLocalSearchParams<{ id: string; section?: StudentSection }>();
  const [section, setSection] = useState<StudentSection>(initialSection ?? 'summary');
  const [message, setMessage] = useState('');
  const student = useTrainerStudent(id);
  const createMessage = useCreateTrainerMessage(id);
  const anamnesis = useTrainerAnamnesis(id, student.data?.anamnesisStatus === 'Completed');
  const workouts = useTrainerStudentWorkouts(id);
  const reorderWorkouts = useReorderTrainerStudentWorkouts(id ?? '');
  const history = useQuery({ queryKey: ['trainer', 'students', id, 'training-history'], queryFn: () => trainerClient.trainingHistory(id!), enabled: Boolean(id) });
  const nutrition = useTrainerNutrition(id ?? '');
  const saveNutrition = useSaveTrainerNutrition(id ?? '');
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

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="ALUNO" title={`${data.firstName} ${data.lastName}`} onBack={() => router.back()} />
    <Text style={styles.identityCopy}>{anamnesis.data?.goal ? `Objetivo: ${anamnesis.data.goal}` : 'Acompanhamento individual'}</Text>
    <View accessibilityRole="tablist" style={styles.tabs}>
      <StudentTab label="Resumo" selected={section === 'summary'} onPress={() => setSection('summary')} />
      <StudentTab label="Treinos" selected={section === 'training'} onPress={() => setSection('training')} />
      <StudentTab label="Alimentação" selected={section === 'nutrition'} onPress={() => setSection('nutrition')} />
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
        <Text style={styles.cardTitle}>Evolução de peso</Text>
        {weight.isLoading ? <Text style={styles.copy}>Carregando medições…</Text> : weight.isError ? <><Text style={styles.errorText}>Não foi possível carregar as medições.</Text><Button variant="secondary" onPress={() => weight.refetch()}>Tentar novamente</Button></> : weight.data?.length ? <><Text style={styles.value}>Último peso: {weight.data.at(-1)?.weightKg} kg</Text><Text style={styles.copy}>{weight.data.length} {weight.data.length === 1 ? 'registro disponível' : 'registros disponíveis'}</Text></> : <EmptyState variant="inline" status="SEM MEDIÇÕES" symbol="●" title="O acompanhamento de peso ainda não começou." message="Os registros informados pelo aluno aparecerão aqui." />}
      </Card>
      <Card style={styles.card}>
        <Text style={styles.cardTitle}>Mensagem para {data.firstName}</Text>
        <Text style={styles.copy}>Ela aparecerá no acompanhamento do aluno.</Text>
        <TextInput value={message} onChangeText={setMessage} multiline maxLength={1000} placeholder="Ex.: Bora treinar hoje." placeholderTextColor={colors.textMuted} accessibilityLabel="Mensagem para o aluno" style={styles.input} />
        <Button loading={createMessage.isPending} disabled={!message.trim()} onPress={() => void sendMessage()}>Enviar mensagem</Button>
      </Card>
    </>}

    {section === 'training' && <>
      <View style={styles.sectionHeader}><View><Text style={styles.sectionTitle}>Treinos disponíveis</Text><Text style={styles.copy}>Organize a ordem sugerida para {data.firstName}. A lista não define dias obrigatórios.</Text></View></View>
      <Button onPress={() => router.push({ pathname: '/trainer/students/[studentId]/workouts/add', params: { studentId: id! } })}>+ Adicionar treino</Button>
      {workouts.isLoading && <Card><Text style={styles.copy}>Carregando treinos…</Text></Card>}
      {workouts.isError && <Card style={styles.card}><Text style={styles.errorText}>Não foi possível carregar os treinos.</Text><Button variant="secondary" onPress={() => workouts.refetch()}>Tentar novamente</Button></Card>}
      {!workouts.isLoading && !workouts.isError && workouts.data?.length === 0 && <EmptyState status="PRIMEIRA PRESCRIÇÃO" symbol="+" title="Prepare o primeiro treino deste aluno." message="Escolha como deseja começar e revise a prescrição antes de disponibilizá-la." actionLabel="Adicionar treino" onAction={() => router.push({ pathname: '/trainer/students/[studentId]/workouts/add', params: { studentId: id! } })} />}
      {workouts.data?.map((workout, index, items) => <WorkoutOrderItem key={workout.id} workout={workout} index={index} count={items.length} busy={reorderWorkouts.isPending} onMove={(to) => {
        const reordered = items.map((item) => item.id);
        const [moved] = reordered.splice(index, 1);
        reordered.splice(to, 0, moved);
        void reorderWorkouts.mutateAsync(reordered).then(() => feedback.success()).catch((error) => Alert.alert('Não foi possível reorganizar', error instanceof Error ? error.message : 'Tente novamente.'));
      }} onPress={() => router.push({ pathname: '/trainer/students/[studentId]/workouts/[workoutId]', params: { studentId: id!, workoutId: workout.id } })} />)}
      <Card style={styles.card}>
        <Text style={styles.cardTitle}>Histórico recente</Text>
        {history.isLoading && <Text style={styles.copy}>Carregando histórico…</Text>}
        {history.isError && <><Text style={styles.errorText}>Não foi possível carregar o histórico.</Text><Button variant="secondary" onPress={() => history.refetch()}>Tentar novamente</Button></>}
        {!history.isLoading && !history.isError && !history.data?.sessions.length && <EmptyState variant="inline" status="HISTÓRICO EM FORMAÇÃO" symbol="●" title="As sessões concluídas aparecerão aqui." message="O histórico será atualizado quando o aluno começar a executar os treinos prescritos." />}
        {history.data?.sessions.slice(0, 5).map((item) => <View key={item.sessionId} style={styles.historySession}><Text style={styles.copy}>{item.workoutName} · {item.status === 'Completed' ? 'Concluído' : 'Em andamento'} · {item.completedSets} registros detalhados</Text>{item.exercises.flatMap((exercise) => exercise.sets.length ? exercise.sets.map((set) => <Text key={`${exercise.sequence}-${set.setNumber}`} style={styles.historySet}>{exercise.name} · {exercise.trackingMode === 'Duration' ? `bloco ${set.setNumber}: ${formatDuration(set.durationSeconds)}` : `série ${set.setNumber}: ${formatRepetitionPerformance(set.weightKg, set.repetitions)}`}</Text>) : exercise.confirmedWithoutDetails ? [<Text key={`${exercise.sequence}-confirmed`} style={styles.historySet}>{exercise.name} · concluído sem detalhes</Text>] : [])}</View>)}
      </Card>
    </>}

    {section === 'nutrition' && <>
      <View style={styles.sectionHeader}><View><Text style={styles.sectionTitle}>Alimentação atual</Text><Text style={styles.copy}>Organize as refeições e disponibilize a versão mais recente para {data.firstName}.</Text></View></View>
      {nutrition.isLoading ? <Card><Text style={styles.copy}>Carregando alimentação…</Text></Card> : nutrition.isError ? <Card style={styles.card}><Text style={styles.errorText}>Não foi possível carregar a alimentação.</Text><Button variant="secondary" onPress={() => nutrition.refetch()}>Tentar novamente</Button></Card> : !nutrition.data ? <EmptyState status="ALIMENTAÇÃO PENDENTE" symbol="+" title="Prepare a primeira alimentação deste aluno." message="Escolha um preset ou comece do zero e revise tudo antes de disponibilizar." actionLabel="Adicionar alimentação" onAction={() => router.push({ pathname: '/trainer/students/[studentId]/nutrition/add', params: { studentId: id! } })} /> : <>
        <Card style={styles.summaryCard}><Pressable accessibilityRole="button" accessibilityLabel="Editar resumo do plano alimentar" accessibilityHint="Altera nome, orientações e metas diárias sem editar as refeições" onPress={() => router.push({ pathname: '/trainer/students/nutrition/[id]', params: { id: id!, edit: 'summary' } })} style={({ pressed }) => [styles.summaryPressable, pressed && styles.pressed]}><Text style={styles.eyebrow}>RESUMO DO PLANO · TOQUE PARA EDITAR</Text><Text style={styles.cardTitle}>{nutrition.data.name}</Text>{nutrition.data.notes ? <Text style={styles.copy}>{nutrition.data.notes}</Text> : null}{dailyGoalsSummary(nutrition.data) ? <Text style={styles.goalsSummary}>{dailyGoalsSummary(nutrition.data)}</Text> : <Text style={styles.planMeta}>Adicione calorias e macros como referência diária.</Text>}<Text style={styles.planMeta}>Responsável: {nutrition.data.responsibleTrainerName} · atualizado em {new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' }).format(new Date(nutrition.data.updatedAt))}</Text><Text style={styles.openHint}>Editar resumo e metas →</Text></Pressable></Card>
        {[...nutrition.data.meals].sort((a, b) => a.sequence - b.sequence).map((meal, index, meals) => <NutritionMealCard key={meal.id} meal={meal} index={index} count={meals.length} busy={saveNutrition.isPending} onEdit={() => router.push({ pathname: '/trainer/students/[studentId]/nutrition/meals/[mealId]', params: { studentId: id!, mealId: meal.id } })} onMove={(to) => {
          const reordered = [...meals];
          const [moved] = reordered.splice(index, 1);
          reordered.splice(to, 0, moved);
          saveNutrition.mutate(nutritionPlanInput(nutrition.data!, reordered), { onSuccess: () => feedback.success(), onError: (error) => Alert.alert('Não foi possível reorganizar', error.message) });
        }} />)}
        <Button onPress={() => router.push({ pathname: '/trainer/students/[studentId]/nutrition/add', params: { studentId: id! } })}>+ Adicionar refeição</Button>
        <Button variant="secondary" onPress={() => router.push({ pathname: '/trainer/students/nutrition/[id]', params: { id: id! } })}>Editar plano completo</Button>
      </>}
    </>}
  </Screen>;
}

function StudentTab({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
  return <Pressable accessibilityRole="tab" accessibilityState={{ selected }} onPress={onPress} style={[styles.tab, selected && styles.tabSelected]}><Text style={[styles.tabText, selected && styles.tabTextSelected]}>{label}</Text></Pressable>;
}

function WorkoutOrderItem({ workout, index, count, busy, onMove, onPress }: { workout: { id: string; name: string; notes: string; exerciseCount: number }; index: number; count: number; busy: boolean; onMove: (to: number) => void; onPress: () => void }) {
  return <Card style={styles.workoutItem}>
    <Pressable accessibilityRole="button" accessibilityLabel={`Abrir treino ${workout.name}`} accessibilityHint="Mostra os exercícios prescritos para este aluno" onPress={onPress} style={({ pressed }) => [styles.workoutPressable, pressed && styles.pressed]}>
      <Text numberOfLines={2} style={styles.workoutTitle}>{workout.name}</Text>
      <Text style={styles.workoutMeta}>Ordem sugerida {index + 1} · {workout.exerciseCount} {workout.exerciseCount === 1 ? 'exercício' : 'exercícios'}</Text>
      {workout.notes ? <Text numberOfLines={2} style={styles.copy}>{workout.notes}</Text> : null}
      <Text style={styles.openHint}>Abrir treino</Text>
    </Pressable>
    <View style={styles.orderActions}>
      <Pressable disabled={busy || index === 0} accessibilityRole="button" accessibilityLabel={`Mover ${workout.name} para cima`} accessibilityState={{ disabled: busy || index === 0 }} onPress={() => onMove(index - 1)} style={[styles.orderButton, (busy || index === 0) && styles.disabled]}><Text style={styles.orderText}>↑</Text></Pressable>
      <Pressable disabled={busy || index === count - 1} accessibilityRole="button" accessibilityLabel={`Mover ${workout.name} para baixo`} accessibilityState={{ disabled: busy || index === count - 1 }} onPress={() => onMove(index + 1)} style={[styles.orderButton, (busy || index === count - 1) && styles.disabled]}><Text style={styles.orderText}>↓</Text></Pressable>
    </View>
  </Card>;
}

function NutritionMealCard({ meal, index, count, busy, onMove, onEdit }: { meal: TrainerNutritionMeal; index: number; count: number; busy: boolean; onMove: (to: number) => void; onEdit: () => void }) {
  return <Card style={styles.mealItem}>
    <Pressable accessibilityRole="button" accessibilityLabel={`Editar refeição ${meal.name}`} accessibilityHint="Toque para alterar os alimentos, quantidades e observações desta refeição" onPress={onEdit} style={({ pressed }) => [styles.mealPressable, pressed && styles.pressed]}>
      <Text style={styles.eyebrow}>REFEIÇÃO {index + 1}</Text><Text style={styles.cardTitle}>{meal.name}</Text>{meal.notes ? <Text style={styles.copy}>{meal.notes}</Text> : null}
      {[...meal.foods].sort((a, b) => a.sequence - b.sequence).map((food) => <View key={food.id} style={styles.foodRow}><Text style={styles.foodName}>{food.foodName}{food.alternatives?.length ? ` · ${food.alternatives.length} alternativa${food.alternatives.length === 1 ? '' : 's'}` : ''}</Text><Text style={styles.foodQuantity}>{formatNutritionQuantity(food.quantity, food.unit)}</Text></View>)}
      <Text style={styles.mealEditHint}>Toque para editar esta refeição →</Text>
    </Pressable>
    <View style={styles.orderActions}><Pressable disabled={busy || index === 0} accessibilityRole="button" accessibilityLabel={`Mover ${meal.name} para cima`} accessibilityState={{ disabled: busy || index === 0 }} onPress={() => onMove(index - 1)} style={[styles.orderButton, (busy || index === 0) && styles.disabled]}><Text style={styles.orderText}>↑</Text></Pressable><Pressable disabled={busy || index === count - 1} accessibilityRole="button" accessibilityLabel={`Mover ${meal.name} para baixo`} accessibilityState={{ disabled: busy || index === count - 1 }} onPress={() => onMove(index + 1)} style={[styles.orderButton, (busy || index === count - 1) && styles.disabled]}><Text style={styles.orderText}>↓</Text></Pressable></View>
  </Card>;
}

function nutritionPlanInput(plan: TrainerNutrition, meals: TrainerNutritionMeal[]) {
  return { name: plan.name, notes: plan.notes, dailyGoals: plan.dailyGoals, meals: meals.map((meal, mealIndex) => ({ name: meal.name, notes: meal.notes, sequence: mealIndex + 1, foods: [...meal.foods].sort((a, b) => a.sequence - b.sequence).map((food, foodIndex) => ({ foodName: food.foodName, quantity: food.quantity, unit: food.unit, sequence: foodIndex + 1, alternatives: (food.alternatives ?? []).map((alternative, alternativeIndex) => ({ foodName: alternative.foodName, quantity: alternative.quantity, unit: alternative.unit, sequence: alternativeIndex + 1, notes: alternative.notes || undefined })) })) })) };
}

function dailyGoalsSummary(plan: TrainerNutrition) {
  const goals = plan.dailyGoals;
  if (!goals) return null;
  const entries = [[goals.calories, 'kcal'], [goals.proteinGrams, 'g proteína'], [goals.carbohydratesGrams, 'g carbo'], [goals.fatGrams, 'g gordura']].filter(([value]) => value != null);
  return entries.length ? entries.map(([value, label]) => `${value} ${label}`).join(' · ') : null;
}

function anamnesisLabel(status: 'NotStarted' | 'InProgress' | 'Completed') {
  return status === 'Completed' ? 'CONCLUÍDA' : status === 'InProgress' ? 'EM PREENCHIMENTO' : 'AGUARDANDO ANAMNESE';
}

function anamnesisCopy(status: 'NotStarted' | 'InProgress' | 'Completed') {
  return status === 'Completed' ? 'As informações da anamnese já estão disponíveis para consulta.' : status === 'InProgress' ? 'O aluno começou a preencher a anamnese.' : 'A anamnese ainda não foi iniciada pelo aluno.';
}

function Detail({ label, value }: { label: string; value: string }) { return <View style={styles.detail}><Text style={styles.label}>{label}</Text><Text style={styles.detailValue}>{value}</Text></View>; }
function formatDuration(seconds?: number) { if (!seconds) return 'duração não informada'; const minutes = Math.floor(seconds / 60); const remainder = seconds % 60; return minutes ? `${minutes}min${remainder ? ` ${remainder}s` : ''}` : `${remainder}s`; }
function formatRepetitionPerformance(weightKg?: number, repetitions?: number) { return weightKg === undefined || repetitions === undefined ? 'detalhes não informados' : `${weightKg} kg × ${repetitions} reps`; }

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, identityCopy: { ...typography.bodyMD, color: colors.textSecondary, marginTop: -spacing.sm }, tabs: { flexDirection: 'row', padding: spacing.xxs, gap: spacing.xxs, borderRadius: radius.md, backgroundColor: colors.surface }, tab: { flex: 1, alignItems: 'center', paddingVertical: spacing.sm, borderRadius: radius.sm }, tabSelected: { backgroundColor: colors.surfaceElevated }, tabText: { ...typography.caption, color: colors.textMuted }, tabTextSelected: { color: colors.primary }, card: { gap: spacing.md }, summaryCard: { padding: 0, overflow: 'hidden' }, summaryPressable: { gap: spacing.sm, padding: spacing.lg }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, row: { gap: spacing.xxs }, label: { ...typography.caption, color: colors.textMuted }, value: { ...typography.bodyLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, errorText: { ...typography.bodyMD, color: colors.danger }, anamnesis: { gap: spacing.sm }, detail: { gap: spacing.xxs }, detailValue: { ...typography.bodyMD, color: colors.textPrimary }, input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 112, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, padding: spacing.md, textAlignVertical: 'top' }, sectionHeader: { flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between', gap: spacing.md }, sectionTitle: { ...typography.headingMD, color: colors.textPrimary }, workoutItem: { flexDirection: 'row', alignItems: 'stretch', gap: spacing.sm, padding: 0, overflow: 'hidden' }, workoutPressable: { flex: 1, gap: spacing.xxs, padding: spacing.md }, workoutTitle: { ...typography.headingMD, color: colors.textPrimary }, workoutMeta: { ...typography.caption, color: colors.primary }, openHint: { ...typography.caption, color: colors.titanium }, orderActions: { justifyContent: 'center', gap: spacing.xs, padding: spacing.sm, borderLeftWidth: 1, borderLeftColor: colors.border }, orderButton: { width: 40, height: 40, alignItems: 'center', justifyContent: 'center', borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, orderText: { ...typography.headingMD, color: colors.titaniumLight }, disabled: { opacity: .3 }, pressed: { opacity: .75 }, historySession: { gap: spacing.xxs, paddingTop: spacing.xs, borderTopWidth: 1, borderTopColor: colors.border }, historySet: { ...typography.caption, color: colors.titanium }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: .8 }, planMeta: { ...typography.caption, color: colors.textMuted }, goalsSummary: { ...typography.bodyMD, color: colors.titaniumLight }, mealItem: { flexDirection: 'row', alignItems: 'stretch', gap: 0, padding: 0, overflow: 'hidden' }, mealPressable: { flex: 1, gap: spacing.sm, padding: spacing.lg }, mealEditHint: { ...typography.bodyMD, color: colors.primary, fontWeight: '700' }, foodRow: { flexDirection: 'row', justifyContent: 'space-between', gap: spacing.md, paddingTop: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border }, foodName: { ...typography.bodyMD, color: colors.textPrimary, flex: 1 }, foodQuantity: { ...typography.caption, color: colors.primary },
});
