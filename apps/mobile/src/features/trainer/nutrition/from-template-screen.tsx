import { router, useLocalSearchParams } from 'expo-router';
import { useMemo, useState } from 'react';
import { Alert, StyleSheet, Text, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, ListItem, LoadingView, SearchField, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { feedback } from '@/src/platform/feedback';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useApplyNutritionTemplate, useNutritionTemplate, useNutritionTemplates } from './hooks';

export function ApplyNutritionTemplateScreen() {
  const { studentId = '' } = useLocalSearchParams<{ studentId: string }>();
  const [selected, setSelected] = useState<string>();
  const [search, setSearch] = useState('');
  const student = useTrainerStudent(studentId);
  const templates = useNutritionTemplates();
  const detail = useNutritionTemplate(selected ?? '', Boolean(selected));
  const apply = useApplyNutritionTemplate(studentId);
  const filtered = useMemo(() => {
    const term = search.trim().toLocaleLowerCase('pt-BR');
    return (templates.data ?? []).filter((item) => !term || `${item.name} ${item.notes}`.toLocaleLowerCase('pt-BR').includes(term));
  }, [search, templates.data]);

  if (!studentId) return <ErrorView message="Não foi possível identificar o aluno." />;
  if (student.isLoading || templates.isLoading) return <LoadingView message="Preparando os presets de refeição…" />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;
  if (templates.isError) return <ErrorView message={templates.error.message} onRetry={() => templates.refetch()} />;
  if (selected && detail.isLoading) return <LoadingView message="Abrindo a refeição…" />;
  if (selected && detail.isError) return <ErrorView message={detail.error.message} onRetry={() => detail.refetch()} />;

  const applyNow = () => {
    if (!selected) return;
    apply.mutate(selected, { onSuccess: (result) => {
      feedback.success();
      Alert.alert('Refeição adicionada', `${result.mealName} foi adicionada à alimentação de ${student.data!.firstName}.`);
      router.replace({ pathname: '/trainer/students/[id]', params: { id: studentId, section: 'nutrition' } });
    } });
  };

  if (selected && detail.data) return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="ETAPA 2 DE 2 · REVISAR" title={detail.data.name} onBack={() => setSelected(undefined)} action={<Tag tone="neutral">PRESET</Tag>} />
    <Text style={styles.copy}>Confira os itens antes de adicionar esta refeição ao plano de {student.data!.firstName}.</Text>
    <Card style={styles.meal}>
      <Text style={styles.label}>PRESET DE REFEIÇÃO</Text>
      <Text style={styles.title}>{detail.data.name}</Text>
      {detail.data.notes ? <Text style={styles.copy}>{detail.data.notes}</Text> : null}
      {(detail.data.foods ?? []).map((food) => <View key={food.id} style={styles.food}><Text style={styles.foodName}>{food.foodName}</Text><Text style={styles.quantity}>{food.quantity} {food.unit}</Text></View>)}
    </Card>
    {apply.error ? <Card style={styles.warning}><Text accessibilityRole="alert" style={styles.warningTitle}>Não foi possível adicionar a refeição</Text><Text style={styles.copy}>{apply.error.message}</Text></Card> : null}
    <Button loading={apply.isPending} disabled={!detail.data.foods?.length} onPress={applyNow}>Adicionar esta refeição</Button>
    <Button variant="ghost" onPress={() => setSelected(undefined)}>Escolher outro preset</Button>
  </Screen>;

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="ETAPA 1 DE 2 · ESCOLHER" title="Preset de refeição" onBack={() => router.back()} />
    <Card style={styles.context}><Text style={styles.label}>ADICIONAR PARA</Text><Text style={styles.title}>{student.data!.firstName} {student.data!.lastName}</Text><Text style={styles.copy}>Escolha uma refeição e revise os itens antes de copiar.</Text></Card>
    {templates.data!.length ? <SearchField value={search} onChangeText={setSearch} placeholder="Buscar refeição…" accessibilityLabel="Buscar preset de refeição para o aluno" /> : null}
    {!templates.data!.length ? <EmptyState status="SEM PRESETS DISPONÍVEIS" symbol="+" title="Crie um preset de refeição primeiro." message="Exemplos: Café com ovos, Café com tapioca ou Lanche rápido." actionLabel="Abrir biblioteca" onAction={() => router.push('/trainer/nutrition/templates')} /> : !filtered.length ? <EmptyState variant="inline" status="NENHUM RESULTADO" symbol="⌕" title="Não encontramos essa refeição." message="Tente outro nome." actionLabel="Limpar busca" onAction={() => setSearch('')} /> : <View style={styles.list}>{filtered.map((item) => <ListItem key={item.id} title={item.name} metadata={`${item.itemCount ?? 0} ${item.itemCount === 1 ? 'item' : 'itens'}`} description={item.notes || undefined} actionLabel="Revisar" disabled={!item.itemCount} onPress={() => setSelected(item.id)} />)}</View>}
  </Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, context: { gap: spacing.xs, borderColor: colors.primary }, label: { ...typography.caption, color: colors.primary, letterSpacing: .8 }, title: { ...typography.headingMD, color: colors.textPrimary }, list: { gap: spacing.sm }, warning: { gap: spacing.xs, borderColor: colors.danger }, warningTitle: { ...typography.headingMD, color: colors.danger }, meal: { gap: spacing.sm }, food: { flexDirection: 'row', justifyContent: 'space-between', gap: spacing.sm, paddingTop: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border }, foodName: { ...typography.bodyMD, color: colors.textPrimary, flex: 1 }, quantity: { ...typography.caption, color: colors.primary } });
