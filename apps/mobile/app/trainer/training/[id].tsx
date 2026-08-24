import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useRef, useState } from 'react';
import { Alert, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import type { TrainerExerciseCatalogItem, WorkoutExercise } from '@/src/api/trainer-client';
import { Button, Card, EmptyState, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useTrainerPrescriptionSettings } from '@/src/features/trainer/settings/hooks';
import { ExerciseCatalogBrowser, type ExerciseMuscleGroup } from '@/src/features/trainer/training/exercise-catalog-browser';
import { useCreateTrainerTemplate, useTrainerExerciseCatalog, useTrainerTemplate, useUpdateTrainerTemplate } from '@/src/features/trainer/training/hooks';
import { initialExercisePrescription, parseExercisePrescription, prescriptionDraftFromDefaults, validateExercisePrescription, type ExercisePrescriptionDraft } from '@/src/features/trainer/training/prescription';
import { loadTemplateDraft, removeTemplateDraft, saveTemplateDraft } from '@/src/features/trainer/training/template-draft-storage';
import { feedback } from '@/src/platform/feedback';
import { ExerciseImage } from '@/src/shared/training/exercise-image';

export default function TrainerTemplateEditorScreen() {
  const { id, draftId } = useLocalSearchParams<{ id: string; draftId?: string }>();
  const isNew = id === 'new';
  const template = useTrainerTemplate(id, !isNew);
  const create = useCreateTrainerTemplate();
  const update = useUpdateTrainerTemplate(id);
  const [name, setName] = useState('');
  const [notes, setNotes] = useState('');
  const [exercises, setExercises] = useState<WorkoutExercise[]>([]);
  const [dirty, setDirty] = useState(isNew);
  const [search, setSearch] = useState('');
  const [muscleGroup, setMuscleGroup] = useState<ExerciseMuscleGroup>('Todos');
  const [showCatalog, setShowCatalog] = useState(false);
  const [configuration, setConfiguration] = useState<{ catalog?: TrainerExerciseCatalogItem; index?: number }>();
  const [prescription, setPrescription] = useState<ExercisePrescriptionDraft>({ ...initialExercisePrescription });
  const initialized = useRef(false);
  const draftWrite = useRef<Promise<unknown>>(Promise.resolve());
  const draftPersistenceDisabled = useRef(false);
  const [localDraftId, setLocalDraftId] = useState<string>();
  const [draftLoading, setDraftLoading] = useState(Boolean(isNew && draftId));
  const catalog = useTrainerExerciseCatalog(search, muscleGroup === 'Todos' ? undefined : muscleGroup, showCatalog);
  const settings = useTrainerPrescriptionSettings();

  useEffect(() => {
    if (!template.data || initialized.current) return;
    initialized.current = true;
    setName(template.data.name);
    setNotes(template.data.notes);
    setExercises(template.data.exercises ?? []);
    setDirty(false);
  }, [template.data]);
  useEffect(() => {
    if (!isNew || !draftId || initialized.current) return;
    void loadTemplateDraft(draftId).then((stored) => {
      if (!stored) return;
      initialized.current = true;
      setLocalDraftId(stored.id);
      setName(stored.name);
      setNotes(stored.notes);
      setExercises(stored.exercises);
      setDirty(true);
    }).finally(() => setDraftLoading(false));
  }, [isNew, draftId]);
  useEffect(() => {
    if (!localDraftId || !draftId || !dirty) return;
    const timeout = setTimeout(() => {
      if (draftPersistenceDisabled.current) return;
      draftWrite.current = draftWrite.current.then(async () => {
        const stored = await loadTemplateDraft(draftId);
        if (stored) await saveTemplateDraft({ ...stored, name, notes, exercises });
      }).catch(() => undefined);
    }, 250);
    return () => clearTimeout(timeout);
  }, [localDraftId, draftId, name, notes, exercises, dirty]);

  if ((!isNew && template.isLoading) || settings.isLoading || draftLoading) return <LoadingView message="Abrindo preset…" />;
  if (!isNew && template.isError) return <ErrorView message={template.error.message} onRetry={() => template.refetch()} />;
  if (settings.isError) return <ErrorView message={settings.error.message} onRetry={() => settings.refetch()} />;

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
  const remove = (index: number) => Alert.alert('Remover exercício?', `${exercises[index].name} será retirado do preset.`, [{ text: 'Cancelar', style: 'cancel' }, { text: 'Remover', style: 'destructive', onPress: () => { setExercises((current) => current.filter((_, itemIndex) => itemIndex !== index).map((exercise, itemIndex) => ({ ...exercise, sequence: itemIndex + 1 }))); setDirty(true); } }]);
  const configureExisting = (index: number) => {
    const exercise = exercises[index];
    setPrescription({ trackingMode: exercise.trackingMode, targetDurationSeconds: String(exercise.targetDurationSeconds ?? 600), sets: String(exercise.sets), repetitionsMin: String(exercise.repetitionsMin), repetitionsMax: String(exercise.repetitionsMax), restSeconds: String(exercise.restSeconds), notes: exercise.notes ?? '' });
    setConfiguration({ index });
    setShowCatalog(false);
  };
  const configureCatalog = (exercise: TrainerExerciseCatalogItem) => {
    const defaults = prescriptionDraftFromDefaults(settings.data!);
    setPrescription({ ...defaults, trackingMode: exercise.defaultTrackingMode, targetDurationSeconds: String(exercise.defaultDurationSeconds ?? 600), sets: exercise.defaultTrackingMode === 'Duration' ? '1' : defaults.sets });
    setConfiguration({ catalog: exercise });
    setShowCatalog(false);
  };
  const commitConfiguration = () => {
    const parsed = parseExercisePrescription(prescription);
    if (!parsed || !configuration) return;
    if (configuration.index !== undefined) {
      setExercises((current) => current.map((exercise, index) => index === configuration.index ? { ...exercise, ...parsed } : exercise));
    } else if (configuration.catalog) {
      const exercise = configuration.catalog;
      setExercises((current) => [...current, { exerciseId: exercise.id, name: exercise.name, primaryMuscleGroup: exercise.primaryMuscleGroup, equipment: exercise.equipment, imageRef: exercise.imageRef, imageUrl: exercise.imageUrl, instructions: exercise.instructions, sequence: current.length + 1, ...parsed }]);
    }
    setConfiguration(undefined);
    setShowCatalog(false);
    setSearch('');
    setMuscleGroup('Todos');
    setDirty(true);
    feedback.success();
  };
  const save = async () => {
    const input = { name: name.trim(), notes: notes.trim(), exercises: exercises.map((exercise, index) => ({ exerciseId: exercise.exerciseId, sequence: index + 1, sets: exercise.sets, repetitionsMin: exercise.repetitionsMin, repetitionsMax: exercise.repetitionsMax, restSeconds: exercise.restSeconds, notes: exercise.notes, trackingMode: exercise.trackingMode, targetDurationSeconds: exercise.targetDurationSeconds })) };
    try {
      draftPersistenceDisabled.current = true;
      const saved = isNew ? await create.mutateAsync(input) : await update.mutateAsync(input);
      if (draftId) { await draftWrite.current; await removeTemplateDraft(draftId).catch(() => undefined); }
      setDirty(false);
      feedback.success();
      if (isNew) router.replace({ pathname: '/trainer/training/[id]', params: { id: saved.id } });
      else Alert.alert('Preset salvo', 'A biblioteca foi atualizada. Treinos já aplicados aos alunos não foram alterados.');
    } catch (error) {
      draftPersistenceDisabled.current = false;
      feedback.warning();
      Alert.alert('Não foi possível salvar', error instanceof Error ? error.message : 'Tente novamente.');
    }
  };

  if (showCatalog) return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow={name.trim() || (isNew ? 'NOVO PRESET' : 'PRESET DE TREINO')} title="Adicionar exercício" onBack={() => setShowCatalog(false)} />
    {catalog.isLoading && !catalog.data ? <LoadingView message="Abrindo o catálogo…" /> : catalog.isError ? <ErrorView message={catalog.error.message} onRetry={() => catalog.refetch()} /> : <ExerciseCatalogBrowser results={catalog.data ?? []} search={search} muscleGroup={muscleGroup} isFetching={catalog.isFetching} onSearchChange={setSearch} onMuscleGroupChange={setMuscleGroup} onSelect={configureCatalog} />}
  </Screen>;

  if (configuration) {
    const exercise = configuration.catalog ?? exercises[configuration.index!];
    const cancelConfiguration = () => { const adding = Boolean(configuration.catalog); setConfiguration(undefined); if (adding) setShowCatalog(true); };
    return <Screen withinTabs style={styles.page}>
      <TopBar eyebrow={name.trim() || (isNew ? 'NOVO PRESET' : 'PRESET DE TREINO')} title="Configurar exercício" onBack={cancelConfiguration} />
      <Card style={styles.exerciseOverview}>
        <View style={styles.heroFrame}><ExerciseImage imageRef={exercise.imageRef} imageUrl={exercise.imageUrl} contentFit="contain" accessibilityLabel={`Demonstração do exercício ${exercise.name}`} style={styles.heroImage} /></View>
        <View style={styles.heroIdentity}><Text style={styles.heroName}>{exercise.name}</Text><Text style={styles.exerciseMeta}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text></View>
      </Card>
      {exercise.instructions ? <Card style={styles.instructions}><Text style={styles.sectionEyebrow}>INSTRUÇÕES</Text><Text style={styles.instructionsCopy}>{exercise.instructions}</Text></Card> : null}
      <View style={styles.sectionHeader}><Text style={styles.sectionTitle}>Configuração</Text><Text style={styles.sectionHint}>Prescrição do preset</Text></View>
      <PrescriptionPanel draft={prescription} errors={prescriptionErrors} onChange={setPrescription} onCancel={cancelConfiguration} onSave={commitConfiguration} disabled={!prescriptionValid} />
    </Screen>;
  }

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow={isNew ? 'NOVO PRESET' : 'PRESET DE TREINO'} title={isNew ? 'Criar preset' : name || 'Preset'} onBack={() => router.back()} action={!isNew ? <Tag tone="neutral">PRESET</Tag> : undefined} />
    <Card style={styles.form}>
      <Field label="Nome do preset" value={name} onChangeText={changeName} maxLength={200} placeholder="Ex.: Upper A" />
      <Field label="Observações" value={notes} onChangeText={changeNotes} maxLength={2000} placeholder="Uso interno do Trainer" multiline />
    </Card>

    <View style={styles.sectionHeader}><View><Text style={styles.sectionTitle}>Exercícios</Text><Text style={styles.copy}>{exercises.length} de 30 configurados</Text></View><Button variant="secondary" disabled={exercises.length >= 30} style={styles.compactButton} onPress={() => { setConfiguration(undefined); setShowCatalog(true); }}>+ Adicionar exercício</Button></View>
    {exercises.length === 0 ? <EmptyState status="PRESET EM CONSTRUÇÃO" symbol="+" title="Adicione o primeiro exercício." message="Escolha no catálogo e configure séries, repetições, descanso e observações." /> : <View style={styles.list}>{exercises.map((exercise, index) => <TemplateExerciseCard key={`${exercise.exerciseId}-${index}`} exercise={exercise} index={index} count={exercises.length} onEdit={() => configureExisting(index)} onRemove={() => remove(index)} onMove={(to) => move(index, to)} />)}</View>}

    <Button loading={saving} disabled={!canSave || saving} onPress={() => void save()}>{isNew ? 'Salvar preset' : 'Salvar alterações do preset'}</Button>
  </Screen>;
}

function TemplateExerciseCard({ exercise, index, count, onEdit, onRemove, onMove }: { exercise: WorkoutExercise; index: number; count: number; onEdit: () => void; onRemove: () => void; onMove: (to: number) => void }) {
  const target = exercise.trackingMode === 'Duration' ? `${formatDuration(exercise.targetDurationSeconds ?? 0)} por bloco` : `${exercise.repetitionsMin}–${exercise.repetitionsMax} reps`;
  return <Card style={styles.exerciseCard}><View style={styles.exerciseHeader}><ExerciseImage imageRef={exercise.imageRef} imageUrl={exercise.imageUrl} contentFit="contain" accessibilityLabel={`Imagem do exercício ${exercise.name}`} style={styles.thumbnail} /><View style={styles.catalogIdentity}><Text style={styles.exerciseName}>{exercise.name}</Text><Text style={styles.exerciseContext}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text><Text style={styles.prescription}>{exercise.sets} {exercise.trackingMode === 'Duration' ? 'blocos' : 'séries'} · {target} · {exercise.restSeconds}s</Text></View></View>{exercise.notes ? <Text style={styles.copy}>{exercise.notes}</Text> : null}<View style={styles.actions}><OrderButton label={`Mover ${exercise.name} para cima`} disabled={index === 0} symbol="↑" onPress={() => onMove(index - 1)} /><OrderButton label={`Mover ${exercise.name} para baixo`} disabled={index === count - 1} symbol="↓" onPress={() => onMove(index + 1)} /><Pressable accessibilityRole="button" onPress={onEdit} style={styles.textButton}><Text style={styles.edit}>Editar</Text></Pressable><Pressable accessibilityRole="button" onPress={onRemove} style={styles.textButton}><Text style={styles.remove}>Remover</Text></Pressable></View></Card>;
}

function PrescriptionPanel({ draft, errors, onChange, onCancel, onSave, disabled }: { draft: ExercisePrescriptionDraft; errors: ReturnType<typeof validateExercisePrescription>; onChange: (draft: ExercisePrescriptionDraft) => void; onCancel: () => void; onSave: () => void; disabled: boolean }) {
  const update = (field: keyof ExercisePrescriptionDraft, value: string) => onChange({ ...draft, [field]: value });
  return <Card style={styles.configuration}><View style={styles.modeRow}><ModeButton selected={draft.trackingMode === 'Repetitions'} label="Repetições" onPress={() => update('trackingMode', 'Repetitions')} /><ModeButton selected={draft.trackingMode === 'Duration'} label="Por tempo" onPress={() => update('trackingMode', 'Duration')} /></View><NumberField label={draft.trackingMode === 'Duration' ? 'Blocos' : 'Séries'} value={draft.sets} min={1} max={20} error={errors.sets} onChange={(value) => update('sets', value)} />{draft.trackingMode === 'Duration' ? <NumberField label="Duração de cada bloco (segundos)" value={draft.targetDurationSeconds} min={5} max={86400} step={15} error={errors.targetDurationSeconds} onChange={(value) => update('targetDurationSeconds', value)} /> : <View style={styles.numberRow}><NumberField label="Repetições mín." value={draft.repetitionsMin} min={1} max={100} error={errors.repetitionsMin} onChange={(value) => update('repetitionsMin', value)} /><Text accessibilityElementsHidden style={styles.rangeSeparator}>—</Text><NumberField label="Repetições máx." value={draft.repetitionsMax} min={1} max={100} error={errors.repetitionsMax} onChange={(value) => update('repetitionsMax', value)} /></View>}<NumberField label="Descanso (segundos)" value={draft.restSeconds} min={0} max={900} step={15} error={errors.restSeconds} onChange={(value) => update('restSeconds', value)} /><Field label="Observações do Trainer" value={draft.notes} onChangeText={(value) => update('notes', value)} maxLength={1000} placeholder="Orientações de execução" multiline error={errors.notes} /><Button disabled={disabled} onPress={onSave}>Confirmar configuração</Button><Button variant="ghost" onPress={onCancel}>Cancelar</Button></Card>;
}

function ModeButton({ selected, label, onPress }: { selected: boolean; label: string; onPress: () => void }) { return <Pressable accessibilityRole="radio" accessibilityState={{ checked: selected }} onPress={onPress} style={[styles.modeButton, selected && styles.modeButtonSelected]}><Text style={[styles.modeText, selected && styles.modeTextSelected]}>{label}</Text></Pressable>; }
function formatDuration(seconds: number) { if (seconds >= 60 && seconds % 60 === 0) return `${seconds / 60} min`; return `${seconds}s`; }

function Field({ label, error, ...props }: React.ComponentProps<typeof TextInput> & { label: string; error?: string }) { return <View style={styles.field}><Text style={styles.label}>{label}</Text><TextInput {...props} placeholderTextColor={colors.textMuted} style={[styles.input, props.multiline && styles.multiline, error && styles.inputError]} />{error ? <Text style={styles.error}>{error}</Text> : null}</View>; }
function NumberField({ label, value, min, max, step = 1, error, onChange }: { label: string; value: string; min: number; max: number; step?: number; error?: string; onChange: (value: string) => void }) {
  const adjust = (direction: -1 | 1) => {
    const current = /^\d+$/.test(value.trim()) ? Number(value) : min;
    onChange(String(Math.min(max, Math.max(min, current + direction * step))));
  };
  return <View style={styles.numberField}><Text style={styles.label}>{label}</Text><View style={[styles.stepper, error && styles.inputError]}><Pressable accessibilityRole="button" accessibilityLabel={`Diminuir ${label}`} onPress={() => adjust(-1)} style={({ pressed }) => [styles.stepButton, pressed && styles.stepPressed]}><Text style={styles.stepText}>−</Text></Pressable><TextInput value={value} onChangeText={onChange} keyboardType="number-pad" maxLength={4} selectTextOnFocus accessibilityLabel={label} style={styles.numberInput} /><Pressable accessibilityRole="button" accessibilityLabel={`Aumentar ${label}`} onPress={() => adjust(1)} style={({ pressed }) => [styles.stepButton, pressed && styles.stepPressed]}><Text style={styles.stepText}>+</Text></Pressable></View>{error ? <Text accessibilityRole="alert" style={styles.error}>{error}</Text> : null}</View>;
}
function OrderButton({ label, disabled, symbol, onPress }: { label: string; disabled: boolean; symbol: string; onPress: () => void }) { return <Pressable disabled={disabled} accessibilityRole="button" accessibilityLabel={label} accessibilityState={{ disabled }} onPress={onPress} style={[styles.orderButton, disabled && styles.disabled]}><Text style={styles.orderText}>{symbol}</Text></Pressable>; }

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, form: { gap: spacing.md }, field: { gap: spacing.xs }, label: { ...typography.caption, color: colors.titanium }, input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 50, paddingHorizontal: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, multiline: { minHeight: 96, paddingTop: spacing.md, textAlignVertical: 'top' }, inputError: { borderColor: colors.danger }, error: { ...typography.caption, color: colors.danger }, sectionHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.sm }, sectionTitle: { ...typography.headingMD, color: colors.textPrimary }, sectionHint: { ...typography.caption, color: colors.textMuted }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, exerciseOverview: { padding: 0, overflow: 'hidden', gap: 0 }, heroFrame: { width: '100%', height: 220, overflow: 'hidden', backgroundColor: colors.surfaceElevated }, heroImage: { width: '100%', height: '100%' }, heroIdentity: { gap: spacing.xxs, padding: spacing.md }, heroName: { ...typography.headingMD, color: colors.textPrimary }, exerciseMeta: { ...typography.caption, color: colors.primary }, instructions: { gap: spacing.xs }, sectionEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: .8 }, instructionsCopy: { ...typography.bodyMD, color: colors.titaniumLight, lineHeight: 22 }, compactButton: { minHeight: 44, paddingHorizontal: spacing.md }, list: { gap: spacing.sm }, exerciseCard: { gap: spacing.md }, exerciseHeader: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm }, thumbnail: { width: 80, height: 80, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, catalogIdentity: { flex: 1, gap: spacing.xxs }, exerciseName: { ...typography.bodyLG, color: colors.textPrimary, fontFamily: 'MontserratSemiBold' }, exerciseContext: { ...typography.caption, color: colors.textMuted }, prescription: { ...typography.bodyMD, color: colors.primary }, actions: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs, paddingTop: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border }, orderButton: { width: 44, height: 44, alignItems: 'center', justifyContent: 'center', borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, disabled: { opacity: .3 }, orderText: { ...typography.headingMD, color: colors.titaniumLight }, textButton: { minHeight: 44, justifyContent: 'center', paddingHorizontal: spacing.xs }, edit: { ...typography.caption, color: colors.primary }, remove: { ...typography.caption, color: colors.danger }, configuration: { gap: spacing.lg }, modeRow: { flexDirection: 'row', gap: spacing.xs }, modeButton: { flex: 1, minHeight: 46, alignItems: 'center', justifyContent: 'center', borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, modeButtonSelected: { borderColor: colors.primary, backgroundColor: '#3A1D0C' }, modeText: { ...typography.caption, color: colors.textSecondary }, modeTextSelected: { color: colors.primary }, numberRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs }, rangeSeparator: { ...typography.headingMD, color: colors.textMuted, paddingTop: spacing.lg }, numberField: { flex: 1, gap: spacing.xs }, stepper: { minHeight: 52, flexDirection: 'row', alignItems: 'stretch', overflow: 'hidden', borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, stepButton: { width: 44, alignItems: 'center', justifyContent: 'center' }, stepPressed: { backgroundColor: '#3A1D0C' }, stepText: { ...typography.headingMD, color: colors.primary }, numberInput: { ...typography.headingMD, color: colors.textPrimary, flex: 1, minWidth: 34, textAlign: 'center', borderLeftWidth: 1, borderRightWidth: 1, borderColor: colors.border },
});
