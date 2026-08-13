import Ionicons from '@expo/vector-icons/Ionicons';
import { router, useLocalSearchParams } from 'expo-router';
import { Alert, Image, Pressable, StyleSheet, Text, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useDeleteTrainerTemplate, useTrainerTemplate } from '@/src/features/trainer/training/hooks';
import { createTemplateDraft } from '@/src/features/trainer/training/template-draft-storage';
import { feedback } from '@/src/platform/feedback';
import { exerciseMediaSource } from '@/src/shared/training/exercise-media';

export default function TrainerTemplateDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const template = useTrainerTemplate(id);
  const remove = useDeleteTrainerTemplate();

  if (template.isLoading) return <LoadingView message="Abrindo detalhes do modelo…" />;
  if (template.isError) return <ErrorView message={template.error.message} onRetry={() => template.refetch()} />;
  const value = template.data!;

  const copyAsDraft = async () => {
    try {
      const draft = await createTemplateDraft(value);
      feedback.success();
      router.push({ pathname: '/trainer/training/[id]', params: { id: 'new', draftId: draft.id } });
    } catch {
      feedback.warning();
      Alert.alert('Não foi possível preparar a cópia', 'Tente novamente. O modelo original não foi alterado.');
    }
  };
  const deleteTemplate = () => Alert.alert('Excluir modelo?', `${value.name} será removido definitivamente da sua biblioteca. Os treinos já criados para alunos continuarão disponíveis.`, [
    { text: 'Cancelar', style: 'cancel' },
    { text: 'Excluir modelo', style: 'destructive', onPress: async () => {
      try { await remove.mutateAsync(value.id); feedback.success(); router.replace('/trainer/training/templates'); }
      catch (error) { feedback.warning(); Alert.alert('Não foi possível excluir', error instanceof Error ? error.message : 'Tente novamente.'); }
    } },
  ]);

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="DETALHES DO MODELO" title={value.name} onBack={() => router.back()} />
    {value.notes ? <Text style={styles.intro}>{value.notes}</Text> : <Text style={styles.intro}>Modelo reutilizável da sua biblioteca.</Text>}
    <Card style={styles.summary}><View><Text style={styles.summaryLabel}>ESTRUTURA</Text><Text style={styles.summaryValue}>{value.exercises?.length ?? 0} {(value.exercises?.length ?? 0) === 1 ? 'exercício configurado' : 'exercícios configurados'}</Text></View><Ionicons name="albums-outline" size={28} color={colors.primary} /></Card>
    <View style={styles.list}>{(value.exercises ?? []).map((exercise) => {
      const source = exerciseMediaSource(exercise.imageRef);
      return <Card key={`${exercise.exerciseId}-${exercise.sequence}`} style={styles.exercise}><View style={styles.exerciseHeader}>{source ? <Image source={source} resizeMode="cover" style={styles.thumbnail} /> : <View style={styles.thumbnail} />}<View style={styles.identity}><Text style={styles.exerciseName}>{exercise.sequence}. {exercise.name}</Text><Text style={styles.context}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text><Text style={styles.prescription}>{exercise.sets} séries · {exercise.repetitionsMin}–{exercise.repetitionsMax} reps · {exercise.restSeconds}s</Text></View></View>{exercise.notes ? <Text style={styles.notes}>{exercise.notes}</Text> : null}</Card>;
    })}</View>
    <Button onPress={() => router.push({ pathname: '/trainer/training/[id]', params: { id: value.id } })}>Editar modelo</Button>
    <Button variant="secondary" onPress={() => void copyAsDraft()}>Criar novo a partir deste</Button>
    <Pressable disabled={remove.isPending} accessibilityRole="button" accessibilityLabel="Excluir modelo" onPress={deleteTemplate} style={({ pressed }) => [styles.deleteButton, pressed && styles.pressed]}><Ionicons name="trash-outline" size={19} color={colors.danger} /><Text style={styles.deleteText}>{remove.isPending ? 'Excluindo…' : 'Excluir modelo'}</Text></Pressable>
    <Text style={styles.footer}>Excluir ou editar este modelo não altera os treinos já atribuídos aos alunos.</Text>
  </Screen>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, intro: { ...typography.bodyLG, color: colors.textSecondary, lineHeight: 24 }, summary: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.md, borderColor: colors.primary, backgroundColor: '#20160F' }, summaryLabel: { ...typography.caption, color: colors.primary, letterSpacing: .7 }, summaryValue: { ...typography.headingMD, color: colors.textPrimary, marginTop: spacing.xxs }, list: { gap: spacing.sm }, exercise: { gap: spacing.sm }, exerciseHeader: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm }, thumbnail: { width: 68, height: 68, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, identity: { flex: 1, gap: spacing.xxs }, exerciseName: { ...typography.bodyLG, color: colors.textPrimary, fontFamily: 'MontserratSemiBold' }, context: { ...typography.caption, color: colors.textMuted }, prescription: { ...typography.bodyMD, color: colors.primary }, notes: { ...typography.bodyMD, color: colors.textSecondary, paddingTop: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border }, deleteButton: { minHeight: 48, flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: spacing.xs, borderWidth: 1, borderColor: colors.danger, borderRadius: radius.sm }, pressed: { opacity: .72 }, deleteText: { ...typography.caption, color: colors.danger }, footer: { ...typography.caption, color: colors.textMuted, textAlign: 'center', lineHeight: 18 },
});
