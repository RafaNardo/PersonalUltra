import { router } from 'expo-router';
import { useMemo, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Button, EmptyState, ErrorView, ListItem, LoadingView, SearchField } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainerTemplates } from '@/src/features/trainer/training/hooks';

export default function TrainerTemplateLibraryScreen() {
  const templates = useTrainerTemplates();
  const [search, setSearch] = useState('');
  const filtered = useMemo(() => {
    const term = search.trim().toLocaleLowerCase('pt-BR');
    return term ? (templates.data ?? []).filter((template) => template.name.toLocaleLowerCase('pt-BR').includes(term)) : (templates.data ?? []);
  }, [templates.data, search]);

  if (templates.isLoading) return <LoadingView message="Carregando modelos…" />;
  if (templates.isError) return <ErrorView message={templates.error.message} onRetry={() => templates.refetch()} />;

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="BIBLIOTECA DO PERSONAL" title="Modelos de treino" onBack={() => router.back()} />
    <Text style={styles.copy}>Encontre rapidamente uma estrutura pronta, consulte os detalhes e escolha como reutilizá-la.</Text>
    <Button onPress={() => router.push({ pathname: '/trainer/training/[id]', params: { id: 'new' } })}>+ Novo modelo</Button>
    {templates.data!.length > 0 ? <SearchField value={search} onChangeText={setSearch} placeholder="Buscar modelo…" accessibilityLabel="Buscar modelo por nome" /> : null}
    {templates.data!.length === 0 ? <EmptyState status="BIBLIOTECA VAZIA" symbol="+" title="Crie seu primeiro modelo de treino." message="Use o catálogo para montar uma prescrição reutilizável e acelerar os próximos atendimentos." actionLabel="Criar modelo" onAction={() => router.push({ pathname: '/trainer/training/[id]', params: { id: 'new' } })} /> : filtered.length === 0 ? <EmptyState variant="inline" status="NENHUM RESULTADO" symbol="⌕" title="Não encontramos esse modelo." message="Tente outro nome ou limpe a busca para ver toda a biblioteca." actionLabel="Limpar busca" onAction={() => setSearch('')} /> : <View style={styles.list}>{filtered.map((template) => <ListItem key={template.id} title={template.name} metadata={`${template.exerciseCount ?? 0} ${(template.exerciseCount ?? 0) === 1 ? 'exercício' : 'exercícios'}${template.updatedAt ? ` · ${formatDate(template.updatedAt)}` : ''}`} onPress={() => router.push({ pathname: '/trainer/training/templates/[id]', params: { id: template.id } })} accessibilityLabel={`Abrir detalhes do modelo ${template.name}`} accessibilityHint="Mostra exercícios e ações do modelo" />)}</View>}
  </Screen>;
}

function formatDate(value: string) { return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' }).format(new Date(value)); }

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, list: { gap: spacing.xs },
});
