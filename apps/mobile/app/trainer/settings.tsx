import { useEffect, useMemo, useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useTrainerPrescriptionSettings, useUpdateTrainerPrescriptionSettings } from '@/src/features/trainer/settings/hooks';
import { hasPrescriptionErrors, prescriptionDraftFromDefaults, validateExercisePrescription, type ExercisePrescriptionDraft } from '@/src/features/trainer/training/prescription';
import { feedback } from '@/src/platform/feedback';

export default function TrainerSettingsScreen() {
  const settings = useTrainerPrescriptionSettings();
  const saveSettings = useUpdateTrainerPrescriptionSettings();
  const [draft, setDraft] = useState<ExercisePrescriptionDraft>();
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (settings.data) setDraft(prescriptionDraftFromDefaults(settings.data));
  }, [settings.data]);

  const errors = useMemo(() => draft ? validateExercisePrescription(draft) : {}, [draft]);
  if (settings.isLoading) return <LoadingView message="Carregando suas configurações…" />;
  if (settings.isError) return <ErrorView message={settings.error.message} onRetry={() => settings.refetch()} />;
  if (!draft) return <LoadingView message="Preparando suas configurações…" />;

  const update = (field: keyof ExercisePrescriptionDraft, value: string) => {
    setSaved(false);
    setDraft((current) => current ? { ...current, [field]: value } : current);
  };
  const save = async () => {
    if (hasPrescriptionErrors(errors)) return;
    try {
      await saveSettings.mutateAsync({
        sets: Number(draft.sets),
        repetitionsMin: Number(draft.repetitionsMin),
        repetitionsMax: Number(draft.repetitionsMax),
        restSeconds: Number(draft.restSeconds),
      });
      setSaved(true);
      feedback.success();
    } catch {
      feedback.warning();
    }
  };

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="SEU JEITO DE PRESCREVER" title="Configurações" />
    <Text style={styles.intro}>Defina um ponto de partida para novos exercícios. Você ainda poderá ajustar cada prescrição antes de publicá-la.</Text>

    <Card style={styles.card}>
      <View style={styles.sectionHeader}>
        <View style={styles.sectionText}>
          <Text style={styles.eyebrow}>PADRÕES DE TREINO</Text>
          <Text style={styles.sectionTitle}>Configuração inicial dos exercícios</Text>
        </View>
        <View style={styles.mark}><Text style={styles.markText}>↗</Text></View>
      </View>

      <Stepper label="Séries" helper="De 1 a 20" value={draft.sets} min={1} max={20} onChange={(value) => update('sets', value)} error={errors.sets} />
      <View style={styles.rangeRow}>
        <View style={styles.rangeField}><Stepper label="Rep. mínimas" helper="De 1 a 100" value={draft.repetitionsMin} min={1} max={100} onChange={(value) => update('repetitionsMin', value)} error={errors.repetitionsMin} compact /></View>
        <View style={styles.rangeField}><Stepper label="Rep. máximas" helper="De 1 a 100" value={draft.repetitionsMax} min={1} max={100} onChange={(value) => update('repetitionsMax', value)} error={errors.repetitionsMax} compact /></View>
      </View>
      <Stepper label="Descanso" helper="Em segundos · de 0 a 900" value={draft.restSeconds} min={0} max={900} step={15} onChange={(value) => update('restSeconds', value)} error={errors.restSeconds} />
    </Card>

    <Card style={styles.summary}>
      <Text style={styles.summaryLabel}>NOVOS EXERCÍCIOS COMEÇARÃO COM</Text>
      <Text style={styles.summaryValue}>{draft.sets || '—'} séries · {draft.repetitionsMin || '—'}–{draft.repetitionsMax || '—'} reps · {draft.restSeconds || '—'}s</Text>
      <Text style={styles.summaryCopy}>Presets e exercícios já salvos mantêm suas configurações atuais.</Text>
    </Card>

    <Button disabled={hasPrescriptionErrors(errors)} loading={saveSettings.isPending} onPress={() => void save()}>Salvar padrões</Button>
    {saved ? <Text accessibilityRole="alert" accessibilityLiveRegion="polite" style={styles.success}>Tudo certo. Seus novos padrões já estão ativos.</Text> : null}
    {saveSettings.isError ? <Text accessibilityRole="alert" style={styles.error}>Não foi possível salvar agora. Confira sua conexão e tente novamente.</Text> : null}
  </Screen>;
}

function Stepper({ label, helper, value, min, max, step = 1, onChange, error, compact = false }: { label: string; helper: string; value: string; min: number; max: number; step?: number; onChange: (value: string) => void; error?: string; compact?: boolean }) {
  const adjust = (direction: -1 | 1) => {
    const current = /^\d+$/.test(value.trim()) ? Number(value) : min;
    onChange(String(Math.min(max, Math.max(min, current + direction * step))));
  };
  return <View style={styles.field}>
    <View style={styles.labelRow}><Text style={styles.label}>{label}</Text><Text style={styles.helper}>{helper}</Text></View>
    <View style={[styles.stepper, error && styles.inputError]}>
      <Pressable accessibilityRole="button" accessibilityLabel={`Diminuir ${label}`} onPress={() => adjust(-1)} style={({ pressed }) => [compact ? styles.compactButton : styles.stepButton, pressed && styles.pressed]}><Text style={styles.stepText}>−</Text></Pressable>
      <TextInput value={value} onChangeText={onChange} keyboardType="number-pad" maxLength={4} selectTextOnFocus accessibilityLabel={label} style={styles.input} />
      <Pressable accessibilityRole="button" accessibilityLabel={`Aumentar ${label}`} onPress={() => adjust(1)} style={({ pressed }) => [compact ? styles.compactButton : styles.stepButton, pressed && styles.pressed]}><Text style={styles.stepText}>+</Text></Pressable>
    </View>
    {error ? <Text accessibilityRole="alert" style={styles.error}>{error}</Text> : null}
  </View>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, intro: { ...typography.bodyLG, color: colors.textSecondary, lineHeight: 24 }, card: { gap: spacing.lg }, sectionHeader: { flexDirection: 'row', alignItems: 'center', gap: spacing.md }, sectionText: { flex: 1, gap: spacing.xxs }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: .8 }, sectionTitle: { ...typography.headingMD, color: colors.textPrimary }, mark: { width: 46, height: 46, borderRadius: 23, alignItems: 'center', justifyContent: 'center', backgroundColor: '#3A1D0C' }, markText: { ...typography.headingMD, color: colors.primary }, field: { gap: spacing.xs }, labelRow: { gap: spacing.xxs }, label: { ...typography.bodyLG, color: colors.titaniumLight, fontFamily: 'MontserratSemiBold' }, helper: { ...typography.caption, color: colors.textMuted }, stepper: { minHeight: 54, flexDirection: 'row', alignItems: 'stretch', overflow: 'hidden', borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, stepButton: { width: 58, alignItems: 'center', justifyContent: 'center' }, compactButton: { width: 38, alignItems: 'center', justifyContent: 'center' }, pressed: { backgroundColor: '#3A1D0C' }, stepText: { ...typography.headingLG, color: colors.primary }, input: { ...typography.headingMD, color: colors.textPrimary, flex: 1, minWidth: 38, textAlign: 'center', borderLeftWidth: 1, borderRightWidth: 1, borderColor: colors.border }, rangeRow: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.sm }, rangeField: { flex: 1 }, inputError: { borderColor: colors.danger }, summary: { gap: spacing.xs, borderColor: colors.primary, backgroundColor: '#20160F' }, summaryLabel: { ...typography.caption, color: colors.primary, letterSpacing: .6 }, summaryValue: { ...typography.headingMD, color: colors.textPrimary }, summaryCopy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, success: { ...typography.bodyMD, color: colors.success, textAlign: 'center' }, error: { ...typography.caption, color: colors.danger, lineHeight: 18 },
});
