import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useMemo, useState } from 'react';
import { Image, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useTrainerPrescriptionSettings } from '@/src/features/trainer/settings/hooks';
import { useTrainerExerciseCatalog, useTrainerStudentWorkout } from '@/src/features/trainer/training/hooks';
import { hasPrescriptionErrors, initialExercisePrescription, prescriptionDraftFromDefaults, validateExercisePrescription, type ExercisePrescriptionDraft } from '@/src/features/trainer/training/prescription';
import { useWorkoutEditorStore, workoutEditorKey } from '@/src/features/trainer/training/workout-editor-store';
import { feedback } from '@/src/platform/feedback';
import { exerciseMediaSource } from '@/src/shared/training/exercise-media';

export default function TrainerExerciseConfigurationScreen() {
  const { studentId, workoutId, exerciseId, workoutExerciseId } = useLocalSearchParams<{ studentId: string; workoutId: string; exerciseId: string; workoutExerciseId?: string }>();
  const key = workoutEditorKey(studentId, workoutId);
  const student = useTrainerStudent(studentId);
  const workout = useTrainerStudentWorkout(studentId, workoutId);
  const editor = useWorkoutEditorStore((state) => state.drafts[key]);
  const initialize = useWorkoutEditorStore((state) => state.initialize);
  const addExercise = useWorkoutEditorStore((state) => state.addExercise);
  const updateExercise = useWorkoutEditorStore((state) => state.updateExercise);
  const editingExercise = editor?.exercises.find((item) => item.clientId === workoutExerciseId);
  const catalog = useTrainerExerciseCatalog('', undefined, !workoutExerciseId);
  const settings = useTrainerPrescriptionSettings(!workoutExerciseId);
  const [draft, setDraft] = useState<ExercisePrescriptionDraft>({ ...initialExercisePrescription });
  const errors = useMemo(() => validateExercisePrescription(draft), [draft]);

  useEffect(() => { if (workout.data) initialize(key, workout.data); }, [initialize, key, workout.data]);
  useEffect(() => {
    if (!editingExercise) return;
    setDraft({ sets: String(editingExercise.sets), repetitionsMin: String(editingExercise.repetitionsMin), repetitionsMax: String(editingExercise.repetitionsMax), restSeconds: String(editingExercise.restSeconds), notes: editingExercise.notes });
  }, [editingExercise?.clientId]);
  useEffect(() => {
    if (editingExercise || !settings.data) return;
    setDraft(prescriptionDraftFromDefaults(settings.data));
  }, [editingExercise, settings.data]);

  if (student.isLoading || workout.isLoading || (!workoutExerciseId && (catalog.isLoading || settings.isLoading)) || (workout.data && !editor)) return <LoadingView message="Carregando o exercício…" />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;
  if (workout.isError) return <ErrorView message={workout.error.message} onRetry={() => workout.refetch()} />;
  if (!workoutExerciseId && catalog.isError) return <ErrorView message={catalog.error.message} onRetry={() => catalog.refetch()} />;
  if (!workoutExerciseId && settings.isError) return <ErrorView message={settings.error.message} onRetry={() => settings.refetch()} />;

  const catalogExercise = catalog.data?.find((item) => item.id === exerciseId);
  const exercise = editingExercise ?? catalogExercise;
  if (!exercise) return <ErrorView message={workoutExerciseId ? 'Este item não está mais no rascunho do treino.' : 'Este exercício não está disponível no catálogo ativo.'} onRetry={workoutExerciseId ? undefined : () => catalog.refetch()} />;

  const source = exerciseMediaSource(exercise.imageRef);
  const update = (field: keyof ExercisePrescriptionDraft, value: string) => setDraft((current) => ({ ...current, [field]: value }));
  const invalid = hasPrescriptionErrors(errors);
  const atLimit = !editingExercise && editor!.exercises.length >= 30;
  const workoutEditorRoute = { pathname: '/trainer/students/[studentId]/workouts/[workoutId]' as const, params: { studentId: studentId!, workoutId: workoutId! } };
  const catalogRoute = { pathname: '/trainer/students/[studentId]/workouts/[workoutId]/catalog' as const, params: { studentId: studentId!, workoutId: workoutId! } };
  const leaveConfiguration = () => router.replace(editingExercise ? workoutEditorRoute : catalogRoute);
  const saveDraft = () => {
    const changed = editingExercise
      ? updateExercise(key, editingExercise.clientId, draft)
      : catalogExercise ? addExercise(key, catalogExercise, draft) : false;
    if (!changed) return;
    feedback.success();
    router.replace(workoutEditorRoute);
  };

  return <Screen style={styles.page}>
    <TopBar eyebrow={`${student.data!.firstName} ${student.data!.lastName} · ${workout.data!.name}`} title={exercise.name} onBack={leaveConfiguration} />
    <Text style={styles.meta}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text>
    <View style={styles.heroFrame}>{source ? <Image source={source} accessibilityLabel={`Demonstração do exercício ${exercise.name}`} resizeMode="cover" style={styles.heroImage} /> : <View accessibilityLabel="Imagem indisponível" style={styles.heroFallback}><Text style={styles.heroFallbackText}>Imagem indisponível</Text></View>}</View>
    {exercise.instructions ? <Card style={styles.instructions}><Text style={styles.sectionEyebrow}>INSTRUÇÕES</Text><Text style={styles.instructionsCopy}>{exercise.instructions}</Text></Card> : null}

    <View style={styles.sectionHeader}><Text style={styles.sectionTitle}>Configuração</Text><Text style={styles.sectionHint}>Prescrição do Trainer</Text></View>
    <Card style={styles.form}>
      <NumberField label="Séries" value={draft.sets} min={1} max={20} onChange={(value) => update('sets', value)} error={errors.sets} />
      <View style={styles.repetitionRow}>
        <View style={styles.repetitionField}><RangeNumberField label="Repetições mín." value={draft.repetitionsMin} onChange={(value) => update('repetitionsMin', value)} error={errors.repetitionsMin} /></View>
        <Text accessibilityElementsHidden style={styles.rangeSeparator}>—</Text>
        <View style={styles.repetitionField}><RangeNumberField label="Repetições máx." value={draft.repetitionsMax} onChange={(value) => update('repetitionsMax', value)} error={errors.repetitionsMax} /></View>
      </View>
      <NumberField label="Descanso (segundos)" value={draft.restSeconds} min={0} max={900} step={15} onChange={(value) => update('restSeconds', value)} error={errors.restSeconds} />
      <View style={styles.field}>
        <View style={styles.labelRow}><Text style={styles.label}>Observações do Trainer</Text><Text style={styles.counter}>{draft.notes.length}/1000</Text></View>
        <TextInput value={draft.notes} onChangeText={(value) => update('notes', value)} maxLength={1001} multiline textAlignVertical="top" placeholder="Ex.: manter escápulas retraídas." placeholderTextColor={colors.textMuted} accessibilityLabel="Observações do Trainer" style={[styles.notes, errors.notes && styles.inputError]} />
        {errors.notes ? <Text accessibilityRole="alert" style={styles.error}>{errors.notes}</Text> : null}
      </View>
    </Card>

    <Card style={[styles.transition, (invalid || atLimit) && styles.transitionInvalid]}>
      <Text style={styles.transitionTitle}>{invalid ? 'Revise a configuração' : atLimit ? 'Limite do treino atingido' : editingExercise ? 'Pronto para atualizar' : 'Pronto para incluir'}</Text>
      <Text style={styles.transitionCopy}>{invalid ? 'Corrija os campos destacados antes de continuar.' : atLimit ? 'Este treino já possui o limite de 30 exercícios.' : 'A alteração ficará no editor e só chegará ao aluno após Publicar alterações.'}</Text>
    </Card>
    <Button disabled={invalid || atLimit} accessibilityHint="Mantém a configuração no editor até a publicação" onPress={saveDraft}>{editingExercise ? 'Atualizar configuração' : 'Adicionar ao treino'}</Button>
    <Button variant="ghost" onPress={leaveConfiguration}>{editingExercise ? 'Voltar ao treino' : 'Voltar ao catálogo'}</Button>
  </Screen>;
}

function NumberField({ label, value, min, max, step = 1, onChange, error }: { label: string; value: string; min: number; max: number; step?: number; onChange: (value: string) => void; error?: string }) {
  const adjust = (direction: -1 | 1) => {
    const current = /^\d+$/.test(value.trim()) ? Number(value) : min;
    onChange(String(Math.min(max, Math.max(min, current + direction * step))));
  };
  return <View style={styles.field}>
    <Text style={styles.label}>{label}</Text>
    <View style={[styles.stepper, error && styles.inputError]}>
      <Pressable accessibilityRole="button" accessibilityLabel={`Diminuir ${label}`} hitSlop={4} onPress={() => adjust(-1)} style={({ pressed }) => [styles.stepButton, pressed && styles.stepPressed]}><Text style={styles.stepText}>−</Text></Pressable>
      <TextInput value={value} onChangeText={onChange} maxLength={4} keyboardType="number-pad" selectTextOnFocus accessibilityLabel={label} style={styles.numberInput} />
      <Pressable accessibilityRole="button" accessibilityLabel={`Aumentar ${label}`} hitSlop={4} onPress={() => adjust(1)} style={({ pressed }) => [styles.stepButton, pressed && styles.stepPressed]}><Text style={styles.stepText}>+</Text></Pressable>
    </View>
    {error ? <Text accessibilityRole="alert" style={styles.error}>{error}</Text> : null}
  </View>;
}

function RangeNumberField({ label, value, onChange, error }: { label: string; value: string; onChange: (value: string) => void; error?: string }) {
  const adjust = (direction: -1 | 1) => {
    const current = /^\d+$/.test(value.trim()) ? Number(value) : 1;
    onChange(String(Math.min(100, Math.max(1, current + direction))));
  };
  return <View style={styles.field}>
    <Text style={styles.label}>{label}</Text>
    <View style={[styles.rangeStepper, error && styles.inputError]}>
      <Pressable accessibilityRole="button" accessibilityLabel={`Diminuir ${label}`} hitSlop={4} onPress={() => adjust(-1)} style={({ pressed }) => [styles.rangeStepButton, pressed && styles.stepPressed]}><Text style={styles.rangeStepText}>−</Text></Pressable>
      <TextInput value={value} onChangeText={onChange} maxLength={3} keyboardType="number-pad" selectTextOnFocus accessibilityLabel={label} style={styles.rangeInput} />
      <Pressable accessibilityRole="button" accessibilityLabel={`Aumentar ${label}`} hitSlop={4} onPress={() => adjust(1)} style={({ pressed }) => [styles.rangeStepButton, pressed && styles.stepPressed]}><Text style={styles.rangeStepText}>+</Text></Pressable>
    </View>
    {error ? <Text accessibilityRole="alert" style={styles.error}>{error}</Text> : null}
  </View>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, meta: { ...typography.caption, color: colors.primary, marginTop: -spacing.sm }, heroFrame: { width: '100%', maxWidth: 420, height: 210, alignSelf: 'center', overflow: 'hidden', borderRadius: radius.lg, backgroundColor: colors.surfaceElevated }, heroImage: { width: '100%', height: '100%' }, heroFallback: { width: '100%', height: '100%', alignItems: 'center', justifyContent: 'center' }, heroFallbackText: { ...typography.bodyMD, color: colors.textMuted }, instructions: { gap: spacing.xs }, sectionEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: .8 }, instructionsCopy: { ...typography.bodyMD, color: colors.titaniumLight, lineHeight: 22 }, sectionHeader: { flexDirection: 'row', alignItems: 'baseline', justifyContent: 'space-between', gap: spacing.sm }, sectionTitle: { ...typography.headingMD, color: colors.textPrimary }, sectionHint: { ...typography.caption, color: colors.textMuted }, form: { gap: spacing.lg }, field: { gap: spacing.xs }, labelRow: { flexDirection: 'row', justifyContent: 'space-between', gap: spacing.sm }, label: { ...typography.caption, color: colors.titanium }, counter: { ...typography.caption, color: colors.textMuted }, stepper: { minHeight: 52, flexDirection: 'row', alignItems: 'stretch', overflow: 'hidden', borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, stepButton: { width: 52, alignItems: 'center', justifyContent: 'center' }, stepPressed: { backgroundColor: '#3A1D0C' }, stepText: { ...typography.headingLG, color: colors.primary }, numberInput: { ...typography.headingMD, color: colors.textPrimary, flex: 1, minWidth: 54, textAlign: 'center', borderLeftWidth: 1, borderRightWidth: 1, borderColor: colors.border }, repetitionRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs }, repetitionField: { flex: 1 }, rangeSeparator: { ...typography.headingMD, color: colors.textMuted, paddingTop: spacing.lg }, rangeStepper: { minHeight: 52, flexDirection: 'row', alignItems: 'stretch', overflow: 'hidden', borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, rangeStepButton: { width: 34, alignItems: 'center', justifyContent: 'center' }, rangeStepText: { ...typography.headingMD, color: colors.primary }, rangeInput: { ...typography.headingMD, color: colors.textPrimary, flex: 1, minWidth: 34, textAlign: 'center', borderLeftWidth: 1, borderRightWidth: 1, borderColor: colors.border }, notes: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 112, padding: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, inputError: { borderColor: colors.danger }, error: { ...typography.caption, color: colors.danger }, transition: { gap: spacing.xs, borderColor: colors.primary, backgroundColor: '#24170F' }, transitionInvalid: { borderColor: colors.danger, backgroundColor: '#251216' }, transitionTitle: { ...typography.caption, color: colors.textPrimary }, transitionCopy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
});
