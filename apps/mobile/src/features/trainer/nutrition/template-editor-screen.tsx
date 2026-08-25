import { router, useLocalSearchParams } from 'expo-router';
import { useCallback, useMemo, useState } from 'react';
import { Alert, StyleSheet, Text } from 'react-native';
import { Button, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { feedback } from '@/src/platform/feedback';
import { NutritionEditor, nutritionMealTemplateDraft } from './editor';
import { useCreateNutritionTemplate, useDeleteNutritionTemplate, useDuplicateNutritionTemplate, useNutritionTemplate, useUpdateNutritionTemplate } from './hooks';

export function NutritionTemplateEditorScreen() {
  const { id = 'new', draftKey = 'initial', returnStudentId } = useLocalSearchParams<{ id: string; draftKey?: string; returnStudentId?: string }>(); const creating = id === 'new';
  const query = useNutritionTemplate(id, !creating); const create = useCreateNutritionTemplate(); const update = useUpdateNutritionTemplate(id); const duplicate = useDuplicateNutritionTemplate(); const remove = useDeleteNutritionTemplate();
  const [dirty, setDirty] = useState(false);
  const initial = useMemo(() => nutritionMealTemplateDraft(query.data), [query.data]);
  const handleDirtyChange = useCallback((value: boolean) => setDirty(value), []);
  if (!creating && query.isLoading) return <LoadingView message="Abrindo preset…" />;
  if (!creating && query.isError) return <ErrorView message={query.error.message} onRetry={() => query.refetch()} />;
  const mutation = creating ? create : update;
  const templatesRoute = { pathname: '/trainer/nutrition/templates' as const, params: returnStudentId ? { returnStudentId } : {} };
  const done = () => { feedback.success(); router.replace(templatesRoute); };
  const back = () => dirty ? Alert.alert('Descartar alterações?', 'O preset continuará como estava antes desta edição.', [{ text: 'Continuar editando', style: 'cancel' }, { text: 'Descartar', style: 'destructive', onPress: () => router.replace(templatesRoute) }]) : router.replace(templatesRoute);
  return <Screen withinTabs style={styles.page}><TopBar eyebrow={creating ? 'NOVO PRESET' : 'EDITAR PRESET'} title={creating ? 'Preset de refeição' : query.data!.name} onBack={back} /><Text style={styles.copy}>Monte uma refeição reutilizável, como “Café com ovos” ou “Café com tapioca”. Cada aplicação adiciona uma cópia independente ao plano do aluno.</Text><NutritionEditor key={`${id}-${draftKey}`} mealTemplate initialValue={initial} pending={mutation.isPending} error={mutation.error?.message} submitLabel={creating ? 'Criar preset de refeição' : 'Salvar preset'} onDirtyChange={handleDirtyChange} onSubmit={(input) => { const meal = input.meals[0]; mutation.mutate({ name: meal.name, notes: meal.notes, foods: meal.foods }, { onSuccess: done }); }} />{!creating ? <><Button variant="secondary" loading={duplicate.isPending} onPress={() => duplicate.mutate(id, { onSuccess: (copy) => { feedback.success(); router.replace({ pathname: '/trainer/nutrition/templates/[id]', params: { id: copy.id, ...(returnStudentId ? { returnStudentId } : {}) } }); } })}>Duplicar preset</Button><Button variant="ghost" loading={remove.isPending} onPress={() => Alert.alert('Excluir preset?', 'As refeições já adicionadas aos alunos não serão alteradas.', [{ text: 'Cancelar', style: 'cancel' }, { text: 'Excluir', style: 'destructive', onPress: () => remove.mutate(id, { onSuccess: done }) }])}>Excluir preset</Button></> : null}</Screen>;
}
const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 } });
