import { router, useLocalSearchParams } from 'expo-router';
import { useCallback, useMemo, useState } from 'react';
import { Alert, StyleSheet, Text } from 'react-native';
import { ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { feedback } from '@/src/platform/feedback';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { NutritionEditor, nutritionMealTemplateDraft } from './editor';
import { useSaveTrainerNutrition, useTrainerNutrition } from './hooks';

export function NutritionMealEditorScreen() {
  const { studentId = '', mealId = '' } = useLocalSearchParams<{ studentId: string; mealId: string }>();
  const nutrition = useTrainerNutrition(studentId);
  const student = useTrainerStudent(studentId);
  const save = useSaveTrainerNutrition(studentId);
  const [dirty, setDirty] = useState(false);
  const handleDirtyChange = useCallback((value: boolean) => setDirty(value), []);
  const meal = nutrition.data?.meals.find((item) => item.id === mealId);
  const creating = mealId === 'new';
  const initial = useMemo(() => nutritionMealTemplateDraft(creating ? null : meal), [creating, meal]);
  const returnToNutrition = () => router.replace({ pathname: '/trainer/students/[id]', params: { id: studentId, section: 'nutrition' } });

  if (!studentId || !mealId) return <ErrorView message="Não foi possível identificar a refeição." />;
  if (nutrition.isLoading || student.isLoading) return <LoadingView message="Abrindo a refeição…" />;
  if (nutrition.isError) return <ErrorView message={nutrition.error.message} onRetry={() => nutrition.refetch()} />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;
  if (!creating && (!nutrition.data || !meal)) return <ErrorView message="Esta refeição não está mais disponível." onRetry={() => nutrition.refetch()} />;

  const back = () => dirty ? Alert.alert('Descartar alterações?', 'A refeição continuará como estava antes desta edição.', [{ text: 'Continuar editando', style: 'cancel' }, { text: 'Descartar', style: 'destructive', onPress: returnToNutrition }]) : returnToNutrition();
  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow={creating ? 'NOVA REFEIÇÃO' : 'EDITAR REFEIÇÃO'} title={creating ? `Alimentação de ${student.data!.firstName}` : meal!.name} onBack={back} />
    <Text style={styles.copy}>{creating ? 'Monte uma nova refeição. Ela será adicionada ao final da alimentação atual.' : 'Altere somente esta refeição. As outras partes da alimentação serão preservadas.'}</Text>
    <NutritionEditor key={mealId} mealTemplate initialValue={initial} pending={save.isPending} error={save.error?.message} submitLabel={creating ? 'Adicionar refeição' : 'Salvar refeição'} onDirtyChange={handleDirtyChange} onSubmit={(input) => {
      const edited = input.meals[0];
      const currentMeals = [...(nutrition.data?.meals ?? [])].sort((a, b) => a.sequence - b.sequence);
      const nextMeals = creating ? [...currentMeals.map((item) => ({ name: item.name, notes: item.notes, foods: item.foods })), edited] : currentMeals.map((item) => item.id === mealId ? edited : { name: item.name, notes: item.notes, foods: item.foods });
      const meals = nextMeals.map((item, mealIndex) => ({ name: item.name, notes: item.notes, sequence: mealIndex + 1, foods: [...item.foods].sort((a, b) => a.sequence - b.sequence).map((food, foodIndex) => ({ foodName: food.foodName, quantity: food.quantity, unit: food.unit, sequence: foodIndex + 1 })) }));
      save.mutate({ name: nutrition.data?.name ?? `Alimentação de ${student.data!.firstName}`, notes: nutrition.data?.notes ?? '', meals }, { onSuccess: () => { feedback.success(); Alert.alert(creating ? 'Refeição adicionada' : 'Refeição atualizada', `${edited.name} foi salva.`); returnToNutrition(); } });
    }} />
  </Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 } });
