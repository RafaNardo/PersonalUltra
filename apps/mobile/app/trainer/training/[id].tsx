import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useRef, useState } from 'react';
import { Alert, Image, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import type { TrainerExerciseCatalogItem, WorkoutExercise } from '@/src/api/trainer-client';
import { Button, Card, EmptyState, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useApplyTrainerTemplate, useCreateTrainerTemplate, useDuplicateTrainerTemplate, useTrainerExerciseCatalog, useTrainerTemplate, useUpdateTrainerTemplate } from '@/src/features/trainer/training/hooks';
import { initialExercisePrescription, parseExercisePrescription, validateExercisePrescription, type ExercisePrescriptionDraft } from '@/src/features/trainer/training/prescription';
import { feedback } from '@/src/platform/feedback';
import { exerciseMediaSource } from '@/src/shared/training/exercise-media';

const weekdays = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'];

export default function TrainerTemplateEditorScreen() {
  const { id, studentId } = useLocalSearchParams<{ id: string; studentId?: string }>();
  const isNew = id === 'new';
  const template = useTrainerTemplate(id, !isNew);
  const student = useTrainerStudent(studentId ?? '');
  const create = useCreateTrainerTemplate();
  const update = useUpdateTrainerTemplate(id);
  const duplicate = useDuplicateTrainerTemplate();
  const apply = useApplyTrainerTemplate();
  const [name, setName] = useState('');
  const [notes, setNotes] = useState('');
  const [exercises, setExercises] = useState<WorkoutExercise[]>([]);
  const [dirty, setDirty] = useState(isNew);
  const [search, setSearch] = useState('');
  const [showCatalog, setShowCatalog] = useState(false);
  const [configuration, setConfiguration] = useState<{ catalog?: TrainerExerciseCatalogItem; index?: number }>();
  const [prescription, setPrescription] = useState<ExercisePrescriptionDraft>({ ...initialExercisePrescription });
  const [recommendedDay, setRecommendedDay] = useState(1);
  const [isRecommended, setIsRecommended] = useState(false);
  const initialized = useRef(false);
  const catalog = useTrainerExerciseCatalog(search, undefined, showCatalog);

  useEffect(() => {
    if (!template.data || initialized.current) return;
    initialized.current = true;
    setName(template.data.name);
    setNotes(template.data.notes);
    setExercises(template.data.exercises ?? []);
    setDirty(false);
  }, [template.data]);

  if ((!isNew && template.isLoading) || (studentId && student.isLoading)) return <LoadingView message="Abrindo modelo…" />;
  if (!isNew && template.isError) return <ErrorView message={template.error.message} onRetry={() => template.refetch()} />;
  if (studentId && student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;

  const prescriptionErrors = validateExercisePrescription(prescription);
  const prescriptionValid = Object.keys(prescriptionErrors).length === 0;
  const saving = create.isPending || update.isPending;
  const canSave = Boolean(name.trim()) && name.trim().length <= 200 && notes.length <= 2000 && exercises.length > 0 && exercises.length <= 30 && dirty;
  const changeName = (value: string) => { setName(value); setDirty(true); };
  const changeNotes = (value: string) => { setNotes(value); setDirty(true); };
  const move = (from: number, to: number) => {
    if (to < 0 || to >= exercises.length) return;
    const next = [...exercises];
    const [item] = next.splice(from, 1);
    next.splice(to, 0, item);
    setExercises(next.map((exercise, index) => ({ ...exercise, sequence: index + 1 })));
    setDirty(true);
    feedback.selection();
  };
  const remove = (index: number) => Alert.alert('Remover exercício?', `${exercises[index].name} será retirado do modelo.`, [{ text: 'Cancelar', style: 'cancel' }, { text: 'Remover', style: 'destructive', onPress: () => { setExercises((current) => current.filter((_, itemIndex) => itemIndex !== index).map((exercise, itemIndex) => ({ ...exercise, sequence: itemIndex + 1 }))); setDirty(true); } }]);
  const configureExisting = (index: number) => {
    const exercise = exercises[index];
    setPrescription({ sets: String(exercise.sets), repetitionsMin: String(exercise.repetitionsMin), repetitionsMax: String(exercise.repetitionsMax), restSeconds: String(exercise.restSeconds), notes: exercise.notes ?? '' });
    setConfiguration({ index });
    setShowCatalog(false);
  };
  const configureCatalog = (exercise: TrainerExerciseCatalogItem) => {
    setPrescription({ ...initialExercisePrescription });
    setConfiguration({ catalog: exercise });
  };
  const commitConfiguration = () => {
    const parsed = parseExercisePrescription(prescription);
    if (!parsed || !configuration) return;
    if (configuration.index !== undefined) {
      setExercises((current) => current.map((exercise, index) => index === configuration.index ? { ...exercise, ...parsed } : exercise));
    } else if (configuration.catalog) {
      const exercise = configuration.catalog;
      setExercises((current) => [...current, { exerciseId: exercise.id, name: exercise.name, primaryMuscleGroup: exercise.primaryMuscleGroup, equipment: exercise.equipment, imageRef: exercise.imageRef, instructions: exercise.instructions, sequence: current.length + 1, ...parsed }]);
    }
    setConfiguration(undefined);
    setShowCatalog(false);
    setSearch('');
    setDirty(true);
    feedback.success();
  };
  const save = async () => {
    const input = { name: name.trim(), notes: notes.trim(), exercises: exercises.map((exercise, index) => ({ exerciseId: exercise.exerciseId, sequence: index + 1, sets: exercise.sets, repetitionsMin: exercise.repetitionsMin, repetitionsMax: exercise.repetitionsMax, restSeconds: exercise.restSeconds, notes: exercise.notes })) };
    try {
      const saved = isNew ? await create.mutateAsync(input) : await update.mutateAsync(input);
      setDirty(false);
      feedback.success();
      if (isNew) router.replace({ pathname: '/trainer/training/[id]', params: { id: saved.id, ...(studentId ? { studentId } : {}) } });
      else Alert.alert('Modelo salvo', 'A biblioteca foi atualizada. Treinos já aplicados aos alunos não foram alterados.');
    } catch (error) {
      feedback.warning();
      Alert.alert('Não foi possível salvar', error instanceof Error ? error.message : 'Tente novamente.');
    }
  };
  const duplicateCurrent = async () => {
    try {
      const copy = await duplicate.mutateAsync(id);
      feedback.success();
      router.replace({ pathname: '/trainer/training/[id]', params: { id: copy.id, ...(studentId ? { studentId } : {}) } });
    } catch (error) { Alert.alert('Não foi possível duplicar', error instanceof Error ? error.message : 'Tente novamente.'); }
  };
  const applyToStudent = async () => {
    if (!studentId) return;
    try {
      const workout = await apply.mutateAsync({ templateId: id, studentId, recommendedDay, isRecommended });
      feedback.success();
      router.replace({ pathname: '/trainer/students/[studentId]/workouts/[workoutId]', params: { studentId, workoutId: workout.id } });
      Alert.alert('Modelo aplicado', `${workout.name} foi criado com ${workout.exerciseCount} exercícios e já pode ser editado para o aluno.`);
    } catch (error) {
      feedback.warning();
      Alert.alert('Não foi possível aplicar', error instanceof Error ? error.message : 'Tente novamente.');
    }
  };

  return <Screen style={styles.page}>
    <TopBar eyebrow={isNew ? 'NOVO ACELERADOR' : 'MODELO DE TREINO'} title={isNew ? 'Criar modelo' : name || 'Modelo'} onBack={() => router.back()} action={!isNew ? <Tag tone="neutral">MODELO</Tag> : undefined} />
    <Card style={styles.form}>
      <Field label="Nome do modelo" value={name} onChangeText={changeName} maxLength={200} placeholder="Ex.: Upper A" />
      <Field label="Observações" value={notes} onChangeText={changeNotes} maxLength={2000} placeholder="Uso interno do Trainer" multiline />
    </Card>

    <View style={styles.sectionHeader}><View><Text style={styles.sectionTitle}>Exercícios</Text><Text style={styles.copy}>{exercises.length} de 30 configurados</Text></View><Button variant="secondary" disabled={exercises.length >= 30} style={styles.compactButton} onPress={() => { setConfiguration(undefined); setShowCatalog(true); }}>+ Adicionar</Button></View>
    {exercises.length === 0 ? <EmptyState title="Modelo sem exercícios" message="Escolha exercícios do catálogo e configure a prescrição completa." /> : <View style={styles.list}>{exercises.map((exercise, index) => <TemplateExerciseCard key={`${exercise.exerciseId}-${index}`} exercise={exercise} index={index} count={exercises.length} onEdit={() => configureExisting(index)} onRemove={() => remove(index)} onMove={(to) => move(index, to)} />)}</View>}

    {showCatalog && <Card style={styles.catalogPanel}>
      <View style={styles.sectionHeader}><Text style={styles.sectionTitle}>Catálogo Ultra</Text><Pressable accessibilityRole="button" accessibilityLabel="Fechar catálogo" onPress={() => setShowCatalog(false)}><Text style={styles.close}>Fechar</Text></Pressable></View>
      <TextInput value={search} onChangeText={setSearch} maxLength={100} autoCapitalize="none" placeholder="Buscar exercício…" placeholderTextColor={colors.textMuted} accessibilityLabel="Buscar no catálogo" style={styles.search} />
      {catalog.isLoading ? <Text style={styles.copy}>Carregando catálogo…</Text> : catalog.isError ? <><Text style={styles.error}>Não foi possível carregar o catálogo.</Text><Button variant="secondary" onPress={() => catalog.refetch()}>Tentar novamente</Button></> : catalog.data!.length === 0 ? <Text style={styles.copy}>Nenhum exercício encontrado.</Text> : <View style={styles.catalogList}>{catalog.data!.map((exercise) => <Pressable key={exercise.id} accessibilityRole="button" accessibilityLabel={`Configurar ${exercise.name}`} onPress={() => configureCatalog(exercise)} style={({ pressed }) => [styles.catalogItem, pressed && styles.pressed]}>{exerciseMediaSource(exercise.imageRef) ? <Image source={exerciseMediaSource(exercise.imageRef)} style={styles.catalogImage} /> : <View style={styles.catalogImage} />}<View style={styles.catalogIdentity}><Text style={styles.exerciseName}>{exercise.name}</Text><Text style={styles.exerciseContext}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text></View><Text style={styles.chevron}>›</Text></Pressable>)}</View>}
    </Card>}

    {configuration && <PrescriptionPanel name={configuration.catalog?.name ?? exercises[configuration.index!]?.name ?? 'Exercício'} draft={prescription} errors={prescriptionErrors} onChange={setPrescription} onCancel={() => setConfiguration(undefined)} onSave={commitConfiguration} disabled={!prescriptionValid} />}

    <Button loading={saving} disabled={!canSave || saving} onPress={() => void save()}>{isNew ? 'Salvar modelo' : 'Salvar alterações do modelo'}</Button>
    {!isNew && <Button variant="secondary" loading={duplicate.isPending} disabled={dirty || saving} onPress={() => void duplicateCurrent()}>Duplicar como novo modelo</Button>}

    {studentId && !isNew && <Card style={styles.applyCard}>
      <Text style={styles.sectionTitle}>Aplicar a {student.data?.firstName ?? 'este aluno'}</Text>
      <Text style={styles.copy}>Isso criará um novo treino editável, sem vínculo futuro com o modelo.</Text>
      <Text style={styles.label}>Dia recomendado</Text>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.days}>{weekdays.map((day, index) => <Pressable key={day} accessibilityRole="radio" accessibilityState={{ checked: recommendedDay === index + 1 }} onPress={() => setRecommendedDay(index + 1)} style={[styles.day, recommendedDay === index + 1 && styles.daySelected]}><Text style={[styles.dayText, recommendedDay === index + 1 && styles.dayTextSelected]}>{day}</Text></Pressable>)}</ScrollView>
      <Pressable accessibilityRole="checkbox" accessibilityState={{ checked: isRecommended }} onPress={() => setIsRecommended((value) => !value)} style={styles.recommendedToggle}><View style={[styles.checkbox, isRecommended && styles.checkboxSelected]}>{isRecommended && <Text style={styles.check}>✓</Text>}</View><View style={styles.catalogIdentity}><Text style={styles.exerciseName}>Marcar como treino recomendado</Text><Text style={styles.copy}>Substitui a recomendação atual deste aluno.</Text></View></Pressable>
      {dirty ? <Text style={styles.warning}>Salve as alterações do modelo antes de aplicar.</Text> : null}
      <Button loading={apply.isPending} disabled={dirty || apply.isPending} onPress={() => void applyToStudent()}>Aplicar como novo treino</Button>
    </Card>}
  </Screen>;
}

function TemplateExerciseCard({ exercise, index, count, onEdit, onRemove, onMove }: { exercise: WorkoutExercise; index: number; count: number; onEdit: () => void; onRemove: () => void; onMove: (to: number) => void }) {
  const source = exerciseMediaSource(exercise.imageRef);
  return <Card style={styles.exerciseCard}><View style={styles.exerciseHeader}>{source ? <Image source={source} style={styles.thumbnail} /> : <View style={styles.thumbnail} />}<View style={styles.catalogIdentity}><Text style={styles.exerciseName}>{exercise.name}</Text><Text style={styles.exerciseContext}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text><Text style={styles.prescription}>{exercise.sets} séries · {exercise.repetitionsMin}–{exercise.repetitionsMax} reps · {exercise.restSeconds}s</Text></View></View>{exercise.notes ? <Text style={styles.copy}>{exercise.notes}</Text> : null}<View style={styles.actions}><OrderButton label={`Mover ${exercise.name} para cima`} disabled={index === 0} symbol="↑" onPress={() => onMove(index - 1)} /><OrderButton label={`Mover ${exercise.name} para baixo`} disabled={index === count - 1} symbol="↓" onPress={() => onMove(index + 1)} /><Pressable accessibilityRole="button" onPress={onEdit} style={styles.textButton}><Text style={styles.edit}>Editar</Text></Pressable><Pressable accessibilityRole="button" onPress={onRemove} style={styles.textButton}><Text style={styles.remove}>Remover</Text></Pressable></View></Card>;
}

function PrescriptionPanel({ name, draft, errors, onChange, onCancel, onSave, disabled }: { name: string; draft: ExercisePrescriptionDraft; errors: ReturnType<typeof validateExercisePrescription>; onChange: (draft: ExercisePrescriptionDraft) => void; onCancel: () => void; onSave: () => void; disabled: boolean }) {
  const update = (field: keyof ExercisePrescriptionDraft, value: string) => onChange({ ...draft, [field]: value });
  return <Card style={styles.configuration}><Text style={styles.sectionTitle}>{name}</Text><View style={styles.numberRow}><NumberField label="Séries" value={draft.sets} error={errors.sets} onChange={(value) => update('sets', value)} /><NumberField label="Rep. mín." value={draft.repetitionsMin} error={errors.repetitionsMin} onChange={(value) => update('repetitionsMin', value)} /><NumberField label="Rep. máx." value={draft.repetitionsMax} error={errors.repetitionsMax} onChange={(value) => update('repetitionsMax', value)} /></View><NumberField label="Descanso em segundos" value={draft.restSeconds} error={errors.restSeconds} onChange={(value) => update('restSeconds', value)} /><Field label="Observações do Trainer" value={draft.notes} onChangeText={(value) => update('notes', value)} maxLength={1000} placeholder="Orientações de execução" multiline error={errors.notes} /><Button disabled={disabled} onPress={onSave}>Confirmar configuração</Button><Button variant="ghost" onPress={onCancel}>Cancelar</Button></Card>;
}

function Field({ label, error, ...props }: React.ComponentProps<typeof TextInput> & { label: string; error?: string }) { return <View style={styles.field}><Text style={styles.label}>{label}</Text><TextInput {...props} placeholderTextColor={colors.textMuted} style={[styles.input, props.multiline && styles.multiline, error && styles.inputError]} />{error ? <Text style={styles.error}>{error}</Text> : null}</View>; }
function NumberField({ label, value, error, onChange }: { label: string; value: string; error?: string; onChange: (value: string) => void }) { return <View style={styles.numberField}><Text style={styles.label}>{label}</Text><TextInput value={value} onChangeText={onChange} keyboardType="number-pad" maxLength={4} selectTextOnFocus style={[styles.input, styles.numberInput, error && styles.inputError]} />{error ? <Text style={styles.error}>{error}</Text> : null}</View>; }
function OrderButton({ label, disabled, symbol, onPress }: { label: string; disabled: boolean; symbol: string; onPress: () => void }) { return <Pressable disabled={disabled} accessibilityRole="button" accessibilityLabel={label} accessibilityState={{ disabled }} onPress={onPress} style={[styles.orderButton, disabled && styles.disabled]}><Text style={styles.orderText}>{symbol}</Text></Pressable>; }

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, form: { gap: spacing.md }, field: { gap: spacing.xs }, label: { ...typography.caption, color: colors.titanium }, input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 50, paddingHorizontal: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, multiline: { minHeight: 96, paddingTop: spacing.md, textAlignVertical: 'top' }, inputError: { borderColor: colors.danger }, error: { ...typography.caption, color: colors.danger }, warning: { ...typography.caption, color: colors.warning }, sectionHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.sm }, sectionTitle: { ...typography.headingMD, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, compactButton: { minHeight: 44, paddingHorizontal: spacing.md }, list: { gap: spacing.sm }, exerciseCard: { gap: spacing.md }, exerciseHeader: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm }, thumbnail: { width: 80, height: 80, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, catalogIdentity: { flex: 1, gap: spacing.xxs }, exerciseName: { ...typography.bodyLG, color: colors.textPrimary, fontFamily: 'MontserratSemiBold' }, exerciseContext: { ...typography.caption, color: colors.textMuted }, prescription: { ...typography.bodyMD, color: colors.primary }, actions: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs, paddingTop: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border }, orderButton: { width: 44, height: 44, alignItems: 'center', justifyContent: 'center', borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, disabled: { opacity: .3 }, orderText: { ...typography.headingMD, color: colors.titaniumLight }, textButton: { minHeight: 44, justifyContent: 'center', paddingHorizontal: spacing.xs }, edit: { ...typography.caption, color: colors.primary }, remove: { ...typography.caption, color: colors.danger }, catalogPanel: { gap: spacing.md, borderColor: colors.primary }, close: { ...typography.caption, color: colors.primary }, search: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 50, paddingHorizontal: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, catalogList: { gap: spacing.xs }, catalogItem: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm, minHeight: 70, padding: spacing.xs, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, catalogImage: { width: 62, height: 62, borderRadius: radius.sm, backgroundColor: colors.surface }, chevron: { fontSize: 24, color: colors.primary }, pressed: { opacity: .76 }, configuration: { gap: spacing.md, borderColor: colors.primary, backgroundColor: '#20160F' }, numberRow: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.xs }, numberField: { flex: 1, gap: spacing.xs }, numberInput: { textAlign: 'center', paddingHorizontal: spacing.xs }, applyCard: { gap: spacing.md, backgroundColor: colors.surfaceElevated }, days: { gap: spacing.xs }, day: { minWidth: 48, minHeight: 44, alignItems: 'center', justifyContent: 'center', borderRadius: radius.pill, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface }, daySelected: { borderColor: colors.primary, backgroundColor: '#3A1D0C' }, dayText: { ...typography.caption, color: colors.textSecondary }, dayTextSelected: { color: colors.primary }, recommendedToggle: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm }, checkbox: { width: 28, height: 28, alignItems: 'center', justifyContent: 'center', borderRadius: radius.sm, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface }, checkboxSelected: { borderColor: colors.primary, backgroundColor: colors.primary }, check: { ...typography.caption, color: colors.textPrimary },
});
