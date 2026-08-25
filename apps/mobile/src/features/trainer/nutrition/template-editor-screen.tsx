import { router, useLocalSearchParams } from 'expo-router';
import { useCallback, useMemo, useState } from 'react';
import { Alert, StyleSheet, Text } from 'react-native';
import { Button, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { feedback } from '@/src/platform/feedback';
import { NutritionEditor, nutritionDraft } from './editor';
import { useCreateNutritionTemplate, useDeleteNutritionTemplate, useDuplicateNutritionTemplate, useNutritionTemplate, useUpdateNutritionTemplate } from './hooks';

export function NutritionTemplateEditorScreen() {
  const { id = 'new' } = useLocalSearchParams<{ id: string }>(); const creating = id === 'new';
  const query = useNutritionTemplate(id, !creating); const create = useCreateNutritionTemplate(); const update = useUpdateNutritionTemplate(id); const duplicate = useDuplicateNutritionTemplate(); const remove = useDeleteNutritionTemplate();
  const [dirty, setDirty] = useState(false);
  const initial = useMemo(() => nutritionDraft(query.data), [query.data]);
  const handleDirtyChange = useCallback((value: boolean) => setDirty(value), []);
  if (!creating && query.isLoading) return <LoadingView message="Abrindo preset…" />;
  if (!creating && query.isError) return <ErrorView message={query.error.message} onRetry={() => query.refetch()} />;
  const mutation = creating ? create : update;
  const done = () => { feedback.success(); router.replace('/trainer/nutrition/templates'); };
  return <Screen withinTabs style={styles.page}><TopBar eyebrow={creating ? 'NOVO PRESET' : 'EDITAR PRESET'} title={creating ? 'Preset de alimentação' : query.data!.name} onBack={() => dirty ? Alert.alert('Descartar alterações?', 'O preset continuará como estava antes desta edição.', [{ text: 'Continuar editando', style: 'cancel' }, { text: 'Descartar', style: 'destructive', onPress: () => router.back() }]) : router.back()} /><Text style={styles.copy}>Organize as refeições e itens. O preset só muda planos futuros; os já aplicados permanecem independentes.</Text><NutritionEditor key={id} initialValue={initial} pending={mutation.isPending} error={mutation.error?.message} submitLabel={creating ? 'Criar preset' : 'Salvar preset'} onDirtyChange={handleDirtyChange} onSubmit={(input) => mutation.mutate(input, { onSuccess: done })} />{!creating ? <><Button variant="secondary" loading={duplicate.isPending} onPress={() => duplicate.mutate(id, { onSuccess: (copy) => { feedback.success(); router.replace({ pathname: '/trainer/nutrition/templates/[id]', params: { id: copy.id } }); } })}>Duplicar preset</Button><Button variant="ghost" loading={remove.isPending} onPress={() => Alert.alert('Excluir preset?', 'Os planos já aplicados aos alunos não serão alterados.', [{ text: 'Cancelar', style: 'cancel' }, { text: 'Excluir', style: 'destructive', onPress: () => remove.mutate(id, { onSuccess: done }) }])}>Excluir preset</Button></> : null}</Screen>;
}
const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 } });
