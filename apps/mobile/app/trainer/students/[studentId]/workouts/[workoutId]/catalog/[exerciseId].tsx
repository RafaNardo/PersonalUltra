import { router, useLocalSearchParams } from 'expo-router';
import { useMemo, useState } from 'react';
import { Image, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useTrainerExerciseCatalog, useTrainerStudentWorkout } from '@/src/features/trainer/training/hooks';
import { hasPrescriptionErrors, initialExercisePrescription, validateExercisePrescription, type ExercisePrescriptionDraft } from '@/src/features/trainer/training/prescription';
import { exerciseMediaSource } from '@/src/shared/training/exercise-media';

export default function TrainerExerciseConfigurationScreen() {
  const { studentId, workoutId, exerciseId } = useLocalSearchParams<{ studentId: string; workoutId: string; exerciseId: string }>();
  const student = useTrainerStudent(studentId);
  const workout = useTrainerStudentWorkout(studentId, workoutId);
  const catalog = useTrainerExerciseCatalog();
  const [draft, setDraft] = useState<ExercisePrescriptionDraft>({ ...initialExercisePrescription });
  const errors = useMemo(() => validateExercisePrescription(draft), [draft]);

  if (student.isLoading || workout.isLoading || catalog.isLoading) return <LoadingView message="Carregando o exercício…" />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;
  if (workout.isError) return <ErrorView message={workout.error.message} onRetry={() => workout.refetch()} />;
  if (catalog.isError) return <ErrorView message={catalog.error.message} onRetry={() => catalog.refetch()} />;

  const exercise = catalog.data?.find((item) => item.id === exerciseId);
  if (!exercise) return <ErrorView message="Este exercício não está disponível no catálogo ativo." onRetry={() => catalog.refetch()} />;

  const source = exerciseMediaSource(exercise.imageRef);
  const update = (field: keyof ExercisePrescriptionDraft, value: string) => setDraft((current) => ({ ...current, [field]: value }));
  const invalid = hasPrescriptionErrors(errors);

  return <Screen style={styles.page}>
    <TopBar eyebrow={`${student.data!.firstName} ${student.data!.lastName} · ${workout.data!.name}`} title={exercise.name} onBack={() => router.back()} />
    <Text style={styles.meta}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text>
    {source ? <Image source={source} accessibilityLabel={`Demonstração do exercício ${exercise.name}`} resizeMode="cover" style={styles.heroImage} /> : <View accessibilityLabel="Imagem indisponível" style={styles.heroFallback}><Text style={styles.heroFallbackText}>Imagem indisponível</Text></View>}
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

    <Card style={[styles.transition, invalid && styles.transitionInvalid]}>
      <Text style={styles.transitionTitle}>{invalid ? 'Revise a configuração' : 'Configuração válida'}</Text>
      <Text style={styles.transitionCopy}>{invalid ? 'Corrija os campos destacados antes de continuar.' : 'Revise os dados da prescrição. A inclusão será habilitada junto com a edição completa deste treino.'}</Text>
    </Card>
    <Button disabled accessibilityHint="A inclusão ainda não está disponível nesta versão">Adicionar ao treino</Button>
    <Button variant="ghost" onPress={() => router.back()}>Voltar ao catálogo</Button>
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
  return <View style={styles.field}>
    <Text style={styles.label}>{label}</Text>
    <TextInput value={value} onChangeText={onChange} maxLength={3} keyboardType="number-pad" selectTextOnFocus accessibilityLabel={label} style={[styles.rangeInput, error && styles.inputError]} />
    {error ? <Text accessibilityRole="alert" style={styles.error}>{error}</Text> : null}
  </View>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, meta: { ...typography.caption, color: colors.primary, marginTop: -spacing.sm }, heroImage: { width: '100%', aspectRatio: 1.45, borderRadius: radius.lg, backgroundColor: colors.surfaceElevated }, heroFallback: { width: '100%', aspectRatio: 1.45, alignItems: 'center', justifyContent: 'center', borderRadius: radius.lg, backgroundColor: colors.surfaceElevated }, heroFallbackText: { ...typography.bodyMD, color: colors.textMuted }, instructions: { gap: spacing.xs }, sectionEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: .8 }, instructionsCopy: { ...typography.bodyMD, color: colors.titaniumLight, lineHeight: 22 }, sectionHeader: { flexDirection: 'row', alignItems: 'baseline', justifyContent: 'space-between', gap: spacing.sm }, sectionTitle: { ...typography.headingMD, color: colors.textPrimary }, sectionHint: { ...typography.caption, color: colors.textMuted }, form: { gap: spacing.lg }, field: { gap: spacing.xs }, labelRow: { flexDirection: 'row', justifyContent: 'space-between', gap: spacing.sm }, label: { ...typography.caption, color: colors.titanium }, counter: { ...typography.caption, color: colors.textMuted }, stepper: { minHeight: 52, flexDirection: 'row', alignItems: 'stretch', overflow: 'hidden', borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, stepButton: { width: 52, alignItems: 'center', justifyContent: 'center' }, stepPressed: { backgroundColor: '#3A1D0C' }, stepText: { ...typography.headingLG, color: colors.primary }, numberInput: { ...typography.headingMD, color: colors.textPrimary, flex: 1, minWidth: 54, textAlign: 'center', borderLeftWidth: 1, borderRightWidth: 1, borderColor: colors.border }, repetitionRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs }, repetitionField: { flex: 1 }, rangeSeparator: { ...typography.headingMD, color: colors.textMuted, paddingTop: spacing.lg }, rangeInput: { ...typography.headingMD, color: colors.textPrimary, minHeight: 52, textAlign: 'center', borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, notes: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 112, padding: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, inputError: { borderColor: colors.danger }, error: { ...typography.caption, color: colors.danger }, transition: { gap: spacing.xs, borderColor: colors.primary, backgroundColor: '#24170F' }, transitionInvalid: { borderColor: colors.danger, backgroundColor: '#251216' }, transitionTitle: { ...typography.caption, color: colors.textPrimary }, transitionCopy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
});
