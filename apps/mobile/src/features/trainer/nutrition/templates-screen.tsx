import { router } from 'expo-router';
import { useMemo, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Button, EmptyState, ErrorView, ListItem, LoadingView, SearchField } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useNutritionTemplates } from './hooks';

export function NutritionTemplatesScreen() {
  const query = useNutritionTemplates(); const [search, setSearch] = useState('');
  const filtered = useMemo(() => { const term = search.trim().toLocaleLowerCase('pt-BR'); return (query.data ?? []).filter((item) => !term || `${item.name} ${item.notes}`.toLocaleLowerCase('pt-BR').includes(term)); }, [query.data, search]);
  if (query.isLoading) return <LoadingView message="Carregando presets de alimentação…" />;
  if (query.isError) return <ErrorView message={query.error.message} onRetry={() => query.refetch()} />;
  return <Screen withinTabs style={styles.page}><TopBar eyebrow="BIBLIOTECA DO PERSONAL" title="Presets de alimentação" onBack={() => router.back()} /><Text style={styles.copy}>Crie estruturas reutilizáveis de refeições e ajuste cada cópia para o aluno.</Text><Button onPress={() => router.push({ pathname: '/trainer/nutrition/templates/[id]', params: { id: 'new' } })}>+ Novo preset</Button>{query.data!.length ? <SearchField value={search} onChangeText={setSearch} placeholder="Buscar preset…" accessibilityLabel="Buscar preset de alimentação" /> : null}{!query.data!.length ? <EmptyState status="BIBLIOTECA VAZIA" symbol="+" title="Crie seu primeiro preset de alimentação." message="Monte refeições e itens uma vez para reutilizar como ponto de partida." actionLabel="Criar preset" onAction={() => router.push({ pathname: '/trainer/nutrition/templates/[id]', params: { id: 'new' } })} /> : !filtered.length ? <EmptyState variant="inline" status="NENHUM RESULTADO" symbol="⌕" title="Não encontramos esse preset." message="Tente outro nome ou limpe a busca." actionLabel="Limpar busca" onAction={() => setSearch('')} /> : <View style={styles.list}>{filtered.map((item) => <ListItem key={item.id} title={item.name} metadata={`${item.mealCount ?? 0} refeições · ${item.foodCount ?? 0} itens`} description={item.notes || `Atualizado em ${formatDate(item.updatedAt)}`} actionLabel="Abrir preset" onPress={() => router.push({ pathname: '/trainer/nutrition/templates/[id]', params: { id: item.id } })} />)}</View>}</Screen>;
}
const formatDate = (value: string) => new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' }).format(new Date(value));
const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, list: { gap: spacing.xs } });
