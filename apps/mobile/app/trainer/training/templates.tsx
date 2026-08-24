import { router } from 'expo-router';
import { useMemo, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Button, EmptyState, ErrorView, ListItem, LoadingView, SearchField } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainerTemplates } from '@/src/features/trainer/training/hooks';
import { filterTemplates, TemplateMuscleFilters, templateGroups } from '@/src/features/trainer/training/template-filters';

export default function TrainerTemplateLibraryScreen() {
  const templates = useTrainerTemplates();
  const [search, setSearch] = useState('');
  const [muscleGroup, setMuscleGroup] = useState<string>();
  const groups = useMemo(() => templateGroups(templates.data ?? []), [templates.data]);
  const filtered = useMemo(() => filterTemplates(templates.data ?? [], search, muscleGroup), [muscleGroup, search, templates.data]);

  if (templates.isLoading) return <LoadingView message="Carregando presets…" />;
  if (templates.isError) return <ErrorView message={templates.error.message} onRetry={() => templates.refetch()} />;

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="BIBLIOTECA DO PERSONAL" title="Presets de treino" onBack={() => router.back()} />
    <Text style={styles.copy}>Encontre rapidamente uma estrutura pronta, consulte os detalhes e escolha como reutilizá-la.</Text>
    <Button onPress={() => router.push({ pathname: '/trainer/training/[id]', params: { id: 'new' } })}>+ Novo preset</Button>
    {templates.data!.length > 0 ? <SearchField value={search} onChangeText={setSearch} placeholder="Buscar preset…" accessibilityLabel="Buscar preset por nome" /> : null}
    <TemplateMuscleFilters groups={groups} selected={muscleGroup} onSelect={setMuscleGroup} />
    {templates.data!.length === 0 ? <EmptyState status="BIBLIOTECA VAZIA" symbol="+" title="Crie seu primeiro preset de treino." message="Use o catálogo para montar uma prescrição reutilizável e acelerar os próximos atendimentos." actionLabel="Criar preset" onAction={() => router.push({ pathname: '/trainer/training/[id]', params: { id: 'new' } })} /> : filtered.length === 0 ? <EmptyState variant="inline" status="NENHUM RESULTADO" symbol="⌕" title="Não encontramos esse preset." message="Tente outro nome, escolha outro grupo muscular ou limpe os filtros." actionLabel="Limpar filtros" onAction={() => { setSearch(''); setMuscleGroup(undefined); }} /> : <View style={styles.list}>{filtered.map((template) => <ListItem key={template.id} title={template.name} metadata={`${template.exerciseCount ?? 0} ${(template.exerciseCount ?? 0) === 1 ? 'exercício' : 'exercícios'}${template.updatedAt ? ` · ${formatDate(template.updatedAt)}` : ''}`} description={template.muscleGroups?.join(' · ') || undefined} onPress={() => router.push({ pathname: '/trainer/training/templates/[id]', params: { id: template.id } })} accessibilityLabel={`Abrir detalhes do preset ${template.name}`} accessibilityHint="Mostra exercícios e ações do preset" />)}</View>}
  </Screen>;
}

function formatDate(value: string) { return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' }).format(new Date(value)); }

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, list: { gap: spacing.xs },
});
