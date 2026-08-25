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
  const { id = '' } = useLocalSearchParams<{ id: string }>();
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
  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="ALIMENTAÇÃO" title={`${firstName} ${student.data!.lastName}`} onBack={() => dirty ? Alert.alert('Descartar alterações?', 'O plano continuará como estava antes desta edição.', [{ text: 'Continuar editando', style: 'cancel' }, { text: 'Descartar', style: 'destructive', onPress: () => router.back() }]) : router.back()} />
    <Text style={styles.copy}>Monte o plano completo. Ao salvar, ele fica disponível para o aluno no aplicativo.</Text>
    <NutritionEditor key={nutrition.data?.id ?? 'new'} initialValue={initial} pending={save.isPending} error={save.error?.message} submitLabel={nutrition.data ? 'Atualizar plano' : 'Salvar e disponibilizar'} onDirtyChange={handleDirtyChange} onSubmit={(input) => Alert.alert(nutrition.data ? 'Atualizar plano?' : 'Salvar e disponibilizar?', `${firstName} verá este plano assim que você salvar.`, [{ text: 'Cancelar', style: 'cancel' }, { text: 'Confirmar', onPress: () => save.mutate(input, { onSuccess: () => { Alert.alert('Plano salvo', `A alimentação de ${firstName} foi atualizada.`); router.replace({ pathname: '/trainer/students/[id]', params: { id, section: 'nutrition' } }); } }) }])} />
  </Screen>;
}
const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 } });
