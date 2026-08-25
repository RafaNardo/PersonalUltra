import { router, useLocalSearchParams } from 'expo-router';
import { useCallback, useMemo, useState } from 'react';
import { Alert, StyleSheet, Text } from 'react-native';
import { ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { NutritionEditor, nutritionDraft } from './editor';
import { useSaveTrainerNutrition, useTrainerNutrition } from './hooks';

export function TrainerNutritionScreen() {
  const { id = '', edit } = useLocalSearchParams<{ id: string; edit?: 'summary' }>();
  const nutrition = useTrainerNutrition(id);
  const student = useTrainerStudent(id);
  const save = useSaveTrainerNutrition(id);
  const [dirty, setDirty] = useState(false);
  const initial = useMemo(() => nutritionDraft(nutrition.data), [nutrition.data]);
  const handleDirtyChange = useCallback((value: boolean) => setDirty(value), []);
  if (nutrition.isLoading || student.isLoading) return <LoadingView message="Abrindo alimentação…" />;
  if (nutrition.isError) return <ErrorView message={nutrition.error.message} onRetry={() => nutrition.refetch()} />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;
  const firstName = student.data!.firstName;
  const returnToNutrition = () => router.replace({ pathname: '/trainer/students/[id]', params: { id, section: 'nutrition' } });
  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow={edit === 'summary' ? 'RESUMO DO PLANO' : 'ALIMENTAÇÃO'} title={`${firstName} ${student.data!.lastName}`} onBack={() => dirty ? Alert.alert('Descartar alterações?', 'O plano continuará como estava antes desta edição.', [{ text: 'Continuar editando', style: 'cancel' }, { text: 'Descartar', style: 'destructive', onPress: returnToNutrition }]) : returnToNutrition()} />
    <Text style={styles.copy}>{edit === 'summary' ? 'Atualize o nome, as orientações e as metas diárias. As refeições já cadastradas serão preservadas.' : 'Monte o plano completo. Ao salvar, ele fica disponível para o aluno no aplicativo.'}</Text>
    <NutritionEditor key={`${nutrition.data?.id ?? 'new'}-${edit ?? 'full'}`} initialValue={initial} pending={save.isPending} error={save.error?.message} submitLabel={edit === 'summary' ? 'Salvar resumo' : nutrition.data ? 'Atualizar plano' : 'Salvar e disponibilizar'} summaryOnly={edit === 'summary'} onDirtyChange={handleDirtyChange} onSubmit={(input) => Alert.alert(edit === 'summary' ? 'Salvar resumo?' : nutrition.data ? 'Atualizar plano?' : 'Salvar e disponibilizar?', `${firstName} verá este plano assim que você salvar.`, [{ text: 'Cancelar', style: 'cancel' }, { text: 'Confirmar', onPress: () => save.mutate(input, { onSuccess: () => { Alert.alert('Plano salvo', `A alimentação de ${firstName} foi atualizada.`); router.replace({ pathname: '/trainer/students/[id]', params: { id, section: 'nutrition' } }); } }) }])} />
  </Screen>;
}
const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 } });
