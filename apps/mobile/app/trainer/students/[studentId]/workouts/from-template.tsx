import { router, useLocalSearchParams } from 'expo-router';
import { useMemo, useState } from 'react';
import { Alert, Image, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, ListItem, LoadingView, SearchField, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useApplyTrainerTemplate, useTrainerTemplate, useTrainerTemplates } from '@/src/features/trainer/training/hooks';
import { feedback } from '@/src/platform/feedback';
import { exerciseMediaSource } from '@/src/shared/training/exercise-media';

const weekdays = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'];

export default function ApplyTemplateToStudentScreen() {
  const { studentId } = useLocalSearchParams<{ studentId: string }>();
  const [selectedTemplateId, setSelectedTemplateId] = useState<string>();
  const [templateSearch, setTemplateSearch] = useState('');
  const [recommendedDay, setRecommendedDay] = useState(1);
  const [isRecommended, setIsRecommended] = useState(false);
  const student = useTrainerStudent(studentId ?? '');
  const templates = useTrainerTemplates();
  const selectedTemplate = useTrainerTemplate(selectedTemplateId ?? '', Boolean(selectedTemplateId));
  const apply = useApplyTrainerTemplate();
  const filteredTemplates = useMemo(() => {
    const term = templateSearch.trim().toLocaleLowerCase('pt-BR');
    return term ? (templates.data ?? []).filter((template) => template.name.toLocaleLowerCase('pt-BR').includes(term)) : (templates.data ?? []);
  }, [templateSearch, templates.data]);

  if (!studentId) return <ErrorView message="Não foi possível identificar o aluno deste treino." />;
  if (student.isLoading || templates.isLoading) return <LoadingView message="Preparando os modelos…" />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;
  if (templates.isError) return <ErrorView message={templates.error.message} onRetry={() => templates.refetch()} />;
  if (selectedTemplateId && selectedTemplate.isLoading) return <LoadingView message="Abrindo o modelo escolhido…" />;
  if (selectedTemplateId && selectedTemplate.isError) return <ErrorView message={selectedTemplate.error.message} onRetry={() => selectedTemplate.refetch()} />;

  const studentName = `${student.data!.firstName} ${student.data!.lastName}`;
  const applyTemplate = async () => {
    if (!selectedTemplateId) return;
    try {
      const workout = await apply.mutateAsync({ templateId: selectedTemplateId, studentId, recommendedDay, isRecommended });
      feedback.success();
      router.replace({ pathname: '/trainer/students/[id]', params: { id: studentId, section: 'training' } });
      Alert.alert('Treino adicionado', `${workout.name} foi adicionado a ${student.data!.firstName} com ${workout.exerciseCount} exercícios.`);
    } catch (error) {
      feedback.warning();
      Alert.alert('Não foi possível adicionar o treino', error instanceof Error ? error.message : 'Tente novamente.');
    }
  };

  if (selectedTemplateId && selectedTemplate.data) {
    const template = selectedTemplate.data;
    const exercises = template.exercises ?? [];
    return <Screen withinTabs style={styles.page}>
      <TopBar eyebrow="ETAPA 2 DE 2 · REVISAR" title={template.name} onBack={() => setSelectedTemplateId(undefined)} action={<Tag tone="neutral">MODELO</Tag>} />
      <Text style={styles.intro}>Confira o conteúdo e escolha como este treino entrará na semana de {student.data!.firstName}.</Text>

      <Card style={styles.summaryCard}>
        <View style={styles.summaryHeader}><View style={styles.identity}><Text style={styles.modelName}>{template.name}</Text><Text style={styles.meta}>{exercises.length} {exercises.length === 1 ? 'exercício configurado' : 'exercícios configurados'}</Text></View><Tag tone="success">PRONTO PARA USAR</Tag></View>
        {template.notes ? <Text style={styles.copy}>{template.notes}</Text> : null}
        <View style={styles.exerciseList}>{exercises.map((exercise) => {
          const source = exerciseMediaSource(exercise.imageRef);
          return <View key={`${exercise.exerciseId}-${exercise.sequence}`} style={styles.exercise}>
            {source ? <Image source={source} style={styles.thumbnail} /> : <View style={styles.thumbnail} />}
            <View style={styles.identity}><Text style={styles.exerciseName}>{exercise.name}</Text><Text style={styles.meta}>{exercise.sets} séries · {exercise.repetitionsMin}–{exercise.repetitionsMax} reps · {exercise.restSeconds}s</Text></View>
          </View>;
        })}</View>
      </Card>

      <Card style={styles.configuration}>
        <Text style={styles.sectionTitle}>Organizar na semana</Text>
        <Text style={styles.copy}>Escolha o dia de referência. O aluno ainda poderá iniciar qualquer treino disponível.</Text>
        <Text style={styles.label}>Dia recomendado</Text>
        <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.days}>{weekdays.map((day, index) => <Pressable key={day} accessibilityRole="radio" accessibilityState={{ checked: recommendedDay === index + 1 }} onPress={() => setRecommendedDay(index + 1)} style={[styles.day, recommendedDay === index + 1 && styles.daySelected]}><Text style={[styles.dayText, recommendedDay === index + 1 && styles.dayTextSelected]}>{day}</Text></Pressable>)}</ScrollView>
        <Pressable accessibilityRole="checkbox" accessibilityState={{ checked: isRecommended }} onPress={() => setIsRecommended((value) => !value)} style={styles.recommendedToggle}>
          <View style={[styles.checkbox, isRecommended && styles.checkboxSelected]}>{isRecommended ? <Text style={styles.check}>✓</Text> : null}</View>
          <View style={styles.identity}><Text style={styles.exerciseName}>Destacar como próximo treino</Text><Text style={styles.copy}>Ele aparecerá como recomendação principal no início do aluno.</Text></View>
        </Pressable>
      </Card>

      <Button loading={apply.isPending} disabled={apply.isPending || exercises.length === 0} onPress={() => void applyTemplate()}>Adicionar aos treinos de {student.data!.firstName}</Button>
      <Button variant="ghost" disabled={apply.isPending} onPress={() => setSelectedTemplateId(undefined)}>Escolher outro modelo</Button>
    </Screen>;
  }

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="ETAPA 1 DE 2 · ESCOLHER" title="Adicionar por modelo" onBack={() => router.back()} />
    <Card style={styles.studentCard}><Text style={styles.label}>NOVO TREINO PARA</Text><Text style={styles.studentName}>{studentName}</Text><Text style={styles.copy}>Escolha uma prescrição pronta. Antes de adicionar, você poderá revisar os exercícios e definir o dia.</Text></Card>

    {templates.data!.length > 0 ? <SearchField value={templateSearch} onChangeText={setTemplateSearch} placeholder="Buscar modelo…" accessibilityLabel="Buscar modelo para o aluno" /> : null}
    {templates.data!.length === 0 ? <EmptyState status="SEM MODELOS DISPONÍVEIS" symbol="+" title="Crie um modelo antes de usar este atalho." message="A biblioteca permite montar prescrições reutilizáveis. Depois, volte ao aluno para aplicá-las." actionLabel="Abrir biblioteca de modelos" onAction={() => router.push('/trainer/training/templates')} /> : filteredTemplates.length === 0 ? <EmptyState variant="inline" status="NENHUM RESULTADO" symbol="⌕" title="Não encontramos esse modelo." message="Tente outro nome ou limpe a busca para consultar todas as opções." actionLabel="Limpar busca" onAction={() => setTemplateSearch('')} /> : <View style={styles.list}>{filteredTemplates.map((template) => {
      const count = template.exerciseCount ?? template.exercises?.length ?? 0;
      return <ListItem key={template.id} title={template.name} metadata={`${count} ${count === 1 ? 'exercício' : 'exercícios'}${template.updatedAt ? ` · atualizado em ${new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' }).format(new Date(template.updatedAt))}` : ''}`} description={count === 0 ? 'Adicione exercícios na biblioteca antes de usar este modelo.' : template.notes || undefined} actionLabel={count === 0 ? 'Modelo vazio' : 'Escolher'} disabled={count === 0} onPress={() => { setSelectedTemplateId(template.id); feedback.selection(); }} accessibilityLabel={`Escolher modelo ${template.name}`} accessibilityHint={count === 0 ? 'Modelo sem exercícios; edite-o na biblioteca antes de usar' : 'Abre a revisão antes de adicionar ao aluno'} />;
    })}</View>}

    <Button variant="ghost" onPress={() => router.push('/trainer/training/templates')}>Gerenciar biblioteca de modelos</Button>
  </Screen>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md },
  intro: { ...typography.bodyLG, color: colors.textSecondary, lineHeight: 25 },
  studentCard: { gap: spacing.xs, backgroundColor: colors.surfaceElevated, borderColor: colors.primary },
  studentName: { ...typography.headingLG, color: colors.textPrimary },
  label: { ...typography.caption, color: colors.primary, letterSpacing: 1 },
  copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
  list: { gap: spacing.sm },
  summaryCard: { gap: spacing.md },
  summaryHeader: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: spacing.sm },
  identity: { flex: 1, gap: spacing.xxs },
  modelName: { ...typography.headingMD, color: colors.textPrimary },
  meta: { ...typography.caption, color: colors.titanium },
  exerciseList: { gap: spacing.xs, borderTopWidth: 1, borderTopColor: colors.border, paddingTop: spacing.sm },
  exercise: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, paddingVertical: spacing.xs },
  thumbnail: { width: 58, height: 58, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated },
  exerciseName: { ...typography.bodyLG, color: colors.textPrimary, fontFamily: 'MontserratSemiBold' },
  configuration: { gap: spacing.md, backgroundColor: colors.surfaceElevated },
  sectionTitle: { ...typography.headingMD, color: colors.textPrimary },
  days: { gap: spacing.xs },
  day: { minWidth: 48, minHeight: 44, alignItems: 'center', justifyContent: 'center', borderRadius: radius.pill, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface },
  daySelected: { borderColor: colors.primary, backgroundColor: '#3A1D0C' },
  dayText: { ...typography.caption, color: colors.textSecondary },
  dayTextSelected: { color: colors.primary },
  recommendedToggle: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm },
  checkbox: { width: 28, height: 28, alignItems: 'center', justifyContent: 'center', borderRadius: radius.sm, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface },
  checkboxSelected: { borderColor: colors.primary, backgroundColor: colors.primary },
  check: { ...typography.caption, color: colors.textPrimary },
});
