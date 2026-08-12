import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Alert, Image, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { z } from 'zod';
import { findExercise, useCompleteSet, useTrainingToday } from '@/src/api/hooks';
import { Button, Card, ErrorView, LoadingView, ProgressBar, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { exerciseImage } from '@/src/design/exercise-media';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { api } from '@/src/api/client';
import { useAuthStore } from '@/src/state/auth-store';
import { feedback } from '@/src/platform/feedback';
import { telemetry } from '@/src/platform/telemetry';

const setSchema = z.object({ weightKg: z.coerce.number().min(0), repetitions: z.coerce.number().int().min(1), repsInReserve: z.coerce.number().int().min(0).max(10) });
type SetForm = { weightKg: string; repetitions: string; repsInReserve: string };
const operationId = () => 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (value) => { const random = Math.floor(Math.random() * 16); return (value === 'x' ? random : (random & 0x3) | 0x8).toString(16); });

export default function ExerciseScreen() {
  const { sessionId, exerciseId } = useLocalSearchParams<{ sessionId: string; exerciseId: string }>();
  const training = useTrainingToday();
  const completeSet = useCompleteSet();
  const token = useAuthStore((state) => state.accessToken)!;
  const [notice, setNotice] = useState<string>();
  const exercise = findExercise(training.data, exerciseId);
  const form = useForm<SetForm>({ defaultValues: { weightKg: String(exercise?.recommendedLoadKg ?? ''), repetitions: String(exercise?.minimumRepetitions ?? ''), repsInReserve: '2' } });
  if (training.isLoading) return <LoadingView />;
  if (training.error || !exercise) return <ErrorView message={training.error?.message ?? 'Exercício não encontrado.'} onRetry={() => void training.refetch()} />;
  const nextSet = exercise.completedSets + 1;
  const image = exerciseImage(exercise.name);
  const submitSet = form.handleSubmit(async (values) => {
    const parsed = setSchema.safeParse(values);
    if (!parsed.success) { setNotice('Revise carga, repetições e RIR antes de registrar.'); return; }
    try {
      const result = await completeSet.mutateAsync({ sessionId, exerciseId, input: { clientOperationId: operationId(), setNumber: nextSet, ...parsed.data } });
      feedback.success();
      telemetry.event('workout_set_logged', { queued: result.queued });
      router.push({ pathname: '/(app)/rest', params: { sessionId, exerciseId, seconds: String(exercise.restSeconds), queued: String(result.queued) } });
    } catch (error) { setNotice(error instanceof Error ? error.message : 'Não foi possível registrar a série.'); }
  });
  const alternatives = async () => {
    try {
      const options = await api.exerciseAlternatives(token, sessionId, exerciseId);
      if (!options.length) return Alert.alert('Não há alternativa aprovada para este exercício.');
      Alert.alert('Substituir exercício', 'A alteração será proposta e só será aplicada após sua confirmação.', options.map((option) => ({ text: option.name, onPress: async () => { try { const proposal = await api.proposeExerciseSubstitution(token, sessionId, exerciseId, option.exerciseId); telemetry.event('exercise_substitution_proposed'); Alert.alert('Confirmar alteração', `Trocar por ${option.name}?`, [{ text: 'Cancelar', style: 'cancel' }, { text: 'Confirmar', onPress: async () => { await api.confirmCoachAction(token, proposal.id); feedback.success(); telemetry.event('coach_action_resolved', { resolution: 'confirmed' }); await training.refetch(); } }]); } catch { Alert.alert('Não foi possível propor a substituição.'); } } })));
    } catch { Alert.alert('Não foi possível buscar alternativas.'); }
  };

  return <Screen>
    <TopBar eyebrow={`${exercise.primaryMuscleGroup} · exercício ${exercise.sequence}`} title={exercise.name} onBack={() => router.replace(`/(app)/workout/${sessionId}`)} />
    {image && <View style={styles.media}><Image source={image} style={styles.mediaImage} resizeMode="cover" /><View style={styles.mediaOverlay}><Text style={styles.mediaText}>EXECUÇÃO CONTROLADA · FOCO NA TÉCNICA</Text></View></View>}
    <Pressable onPress={() => void alternatives()}><Text style={styles.alternative}>Precisa trocar este exercício?</Text></Pressable>
    <Card style={styles.prescription}><View><Text style={styles.prescriptionLabel}>PRESCRIÇÃO</Text><Text style={styles.prescriptionValue}>{exercise.prescribedSets} × {exercise.minimumRepetitions}–{exercise.maximumRepetitions}</Text></View><View><Text style={styles.prescriptionLabel}>DESCANSO</Text><Text style={styles.prescriptionValue}>{exercise.restSeconds}s</Text></View><Tag tone="primary">alvo {exercise.recommendedLoadKg} kg</Tag></Card>
    <View style={styles.progressGroup}><Text style={styles.progressLabel}>Séries concluídas: {exercise.completedSets}/{exercise.prescribedSets}</Text><ProgressBar value={exercise.completedSets / exercise.prescribedSets} /></View>
    {Array.from({ length: exercise.prescribedSets }, (_, index) => <Card key={index} style={styles.setHistory}><Text style={styles.setTitle}>Série {index + 1}</Text><Text style={styles.setStatus}>{index < exercise.completedSets ? 'Registrada' : index === exercise.completedSets ? 'Próxima' : 'Pendente'}</Text></Card>)}
    {nextSet <= exercise.prescribedSets ? <Card style={styles.formCard}><Text style={styles.formTitle}>Registrar série {nextSet}</Text><Field label="Carga (kg)" keyboardType="decimal-pad" value={form.watch('weightKg')} onChangeText={(text) => form.setValue('weightKg', text)} /><Field label="Repetições" keyboardType="number-pad" value={form.watch('repetitions')} onChangeText={(text) => form.setValue('repetitions', text)} /><Field label="RIR" keyboardType="number-pad" value={form.watch('repsInReserve')} onChangeText={(text) => form.setValue('repsInReserve', text)} />{notice && <Text style={styles.error}>{notice}</Text>}<Button onPress={submitSet} loading={completeSet.isPending}>Concluir série</Button></Card> : <Card style={styles.done}><Text style={styles.doneTitle}>Exercício concluído.</Text><Button variant="secondary" onPress={() => router.back()}>Voltar ao treino</Button></Card>}
  </Screen>;
}

function Field({ label, value, onChangeText, keyboardType }: { label: string; value: string; onChangeText: (text: string) => void; keyboardType: 'decimal-pad' | 'number-pad' }) { return <View style={styles.field}><Text style={styles.fieldLabel}>{label}</Text><TextInput value={value} onChangeText={onChangeText} keyboardType={keyboardType} selectTextOnFocus style={styles.input} placeholderTextColor={colors.textMuted} /></View>; }

const styles = StyleSheet.create({
  media: { height: 180, overflow: 'hidden', borderRadius: radius.md, backgroundColor: colors.surface }, mediaImage: { width: '100%', height: '100%', opacity: .72 }, mediaOverlay: { position: 'absolute', left: spacing.sm, bottom: spacing.sm, backgroundColor: 'rgba(0, 0, 0, .72)', borderRadius: radius.sm, paddingHorizontal: spacing.sm, paddingVertical: spacing.xs }, mediaText: { ...typography.caption, color: colors.textPrimary, fontSize: 10 }, alternative: { ...typography.bodyMD, color: colors.primary, fontFamily: 'MontserratBold' }, prescription: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', gap: spacing.lg }, prescriptionLabel: { ...typography.caption, color: colors.textMuted }, prescriptionValue: { ...typography.headingMD, color: colors.textPrimary }, progressGroup: { gap: spacing.sm }, progressLabel: { ...typography.bodyMD, color: colors.textSecondary }, setHistory: { flexDirection: 'row', justifyContent: 'space-between', paddingVertical: spacing.md }, setTitle: { ...typography.bodyLG, color: colors.textPrimary }, setStatus: { ...typography.bodyMD, color: colors.textSecondary }, formCard: { gap: spacing.md }, formTitle: { ...typography.headingMD, color: colors.textPrimary }, field: { gap: spacing.xs }, fieldLabel: { ...typography.caption, color: colors.textSecondary }, input: { height: 50, borderRadius: radius.sm, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surfaceElevated, color: colors.textPrimary, paddingHorizontal: spacing.md, ...typography.bodyLG }, error: { ...typography.bodyMD, color: colors.danger }, done: { gap: spacing.md }, doneTitle: { ...typography.headingMD, color: colors.success },
});
