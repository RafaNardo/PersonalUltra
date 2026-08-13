import Ionicons from '@expo/vector-icons/Ionicons';
import { router } from 'expo-router';
import { useMemo, useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, EmptyState, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
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

  return <Screen style={styles.page}>
    <TopBar eyebrow="BIBLIOTECA DO PERSONAL" title="Modelos de treino" onBack={() => router.back()} />
    <Text style={styles.copy}>Encontre rapidamente uma estrutura pronta, consulte os detalhes e escolha como reutilizá-la.</Text>
    <Button onPress={() => router.push({ pathname: '/trainer/training/[id]', params: { id: 'new' } })}>+ Novo modelo</Button>
    {templates.data!.length > 0 ? <View style={styles.searchShell}><Ionicons name="search-outline" size={21} color={colors.textMuted} /><TextInput value={search} onChangeText={setSearch} autoCapitalize="none" placeholder="Buscar modelo…" placeholderTextColor={colors.textMuted} accessibilityLabel="Buscar modelo por nome" style={styles.search} /></View> : null}
    {templates.data!.length === 0 ? <EmptyState status="BIBLIOTECA VAZIA" symbol="+" title="Crie seu primeiro modelo de treino." message="Use o catálogo para montar uma prescrição reutilizável e acelerar os próximos atendimentos." actionLabel="Criar modelo" onAction={() => router.push({ pathname: '/trainer/training/[id]', params: { id: 'new' } })} /> : filtered.length === 0 ? <EmptyState variant="inline" status="NENHUM RESULTADO" symbol="⌕" title="Não encontramos esse modelo." message="Tente outro nome ou limpe a busca para ver toda a biblioteca." actionLabel="Limpar busca" onAction={() => setSearch('')} /> : <View style={styles.list}>{filtered.map((template) => <Pressable key={template.id} accessibilityRole="button" accessibilityLabel={`Abrir detalhes do modelo ${template.name}`} accessibilityHint="Mostra exercícios e ações do modelo" onPress={() => router.push({ pathname: '/trainer/training/templates/[id]', params: { id: template.id } })} style={({ pressed }) => [styles.row, pressed && styles.pressed]}>
      <View style={styles.identity}><Text numberOfLines={1} style={styles.name}>{template.name}</Text><Text style={styles.meta}>{template.exerciseCount ?? 0} {(template.exerciseCount ?? 0) === 1 ? 'exercício' : 'exercícios'}{template.updatedAt ? ` · ${formatDate(template.updatedAt)}` : ''}</Text></View>
      <View style={styles.openHint}><Text style={styles.openText}>Ver detalhes</Text><Ionicons name="chevron-forward" size={20} color={colors.primary} /></View>
    </Pressable>)}</View>}
  </Screen>;
}

function formatDate(value: string) { return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' }).format(new Date(value)); }

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, searchShell: { minHeight: 50, flexDirection: 'row', alignItems: 'center', gap: spacing.sm, paddingHorizontal: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, backgroundColor: colors.surface }, search: { ...typography.bodyMD, color: colors.textPrimary, flex: 1, minHeight: 48 }, list: { gap: spacing.xs }, row: { minHeight: 76, flexDirection: 'row', alignItems: 'center', gap: spacing.sm, padding: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, backgroundColor: colors.surface }, pressed: { opacity: .76, transform: [{ scale: .99 }] }, identity: { flex: 1, gap: spacing.xxs }, name: { ...typography.bodyLG, color: colors.textPrimary, fontFamily: 'MontserratSemiBold' }, meta: { ...typography.caption, color: colors.titanium }, openHint: { flexDirection: 'row', alignItems: 'center', gap: spacing.xxs }, openText: { ...typography.caption, color: colors.primary },
});
