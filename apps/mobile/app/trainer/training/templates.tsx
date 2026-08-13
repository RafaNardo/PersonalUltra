import { router } from 'expo-router';
import { Alert, StyleSheet, Text, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useDeleteTrainerTemplate, useDuplicateTrainerTemplate, useTrainerTemplates } from '@/src/features/trainer/training/hooks';
import { feedback } from '@/src/platform/feedback';

export default function TrainerTemplateLibraryScreen() {
  const templates = useTrainerTemplates();
  const duplicate = useDuplicateTrainerTemplate();
  const remove = useDeleteTrainerTemplate();

  if (templates.isLoading) return <LoadingView message="Carregando modelos…" />;
  if (templates.isError) return <ErrorView message={templates.error.message} onRetry={() => templates.refetch()} />;

  const open = (templateId: string) => router.push({ pathname: '/trainer/training/[id]', params: { id: templateId } });
  const duplicateTemplate = async (templateId: string, name: string) => {
    try {
      const copy = await duplicate.mutateAsync(templateId);
      feedback.success();
      Alert.alert('Modelo duplicado', `${name} foi copiado. Você pode editar a nova versão sem alterar o original.`, [{ text: 'Agora não' }, { text: 'Abrir cópia', onPress: () => open(copy.id) }]);
    } catch (error) {
      feedback.warning();
      Alert.alert('Não foi possível duplicar', error instanceof Error ? error.message : 'Tente novamente.');
    }
  };
  const deleteTemplate = (templateId: string, name: string) => {
    Alert.alert(
      'Excluir modelo?',
      `${name} será removido definitivamente da sua biblioteca. Os treinos já criados para alunos continuarão disponíveis.`,
      [
        { text: 'Cancelar', style: 'cancel' },
        {
          text: 'Excluir modelo',
          style: 'destructive',
          onPress: async () => {
            try {
              await remove.mutateAsync(templateId);
              feedback.success();
              Alert.alert('Modelo excluído', 'Sua biblioteca foi atualizada. Nenhum treino de aluno foi alterado.');
            } catch (error) {
              feedback.warning();
              Alert.alert('Não foi possível excluir', error instanceof Error ? error.message : 'Tente novamente.');
            }
          },
        },
      ],
    );
  };

  return <Screen style={styles.page}>
    <TopBar eyebrow="BIBLIOTECA DO PERSONAL" title="Modelos de treino" onBack={() => router.back()} />
    <Text style={styles.copy}>Crie e mantenha prescrições reutilizáveis. Os treinos dos alunos continuam sendo editados individualmente.</Text>
    <Button onPress={() => router.push({ pathname: '/trainer/training/[id]', params: { id: 'new' } })}>+ Novo modelo</Button>
    {templates.data!.length === 0 ? <EmptyState status="BIBLIOTECA VAZIA" symbol="+" title="Crie seu primeiro modelo de treino." message="Use o catálogo para montar uma prescrição reutilizável e acelerar os próximos atendimentos." actionLabel="Criar modelo" onAction={() => router.push({ pathname: '/trainer/training/[id]', params: { id: 'new' } })} /> : <View style={styles.list}>{templates.data!.map((template) => <Card key={template.id} style={styles.card}>
      <Text style={styles.name}>{template.name}</Text>
      <Text style={styles.meta}>{template.exerciseCount ?? 0} {(template.exerciseCount ?? 0) === 1 ? 'exercício' : 'exercícios'}{template.updatedAt ? ` · atualizado em ${new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' }).format(new Date(template.updatedAt))}` : ''}</Text>
      {template.notes ? <Text numberOfLines={2} style={styles.copy}>{template.notes}</Text> : null}
      <Button onPress={() => open(template.id)}>Editar modelo</Button>
      <Button variant="secondary" loading={duplicate.isPending && duplicate.variables === template.id} disabled={duplicate.isPending} onPress={() => void duplicateTemplate(template.id, template.name)}>Duplicar modelo</Button>
      <Button variant="ghost" loading={remove.isPending && remove.variables === template.id} disabled={remove.isPending || duplicate.isPending} onPress={() => deleteTemplate(template.id, template.name)}>Excluir modelo</Button>
    </Card>)}</View>}
    <Card style={styles.context}><Text style={styles.contextTitle}>Modelos não são planos ativos</Text><Text style={styles.copy}>Aplicar cria um novo snapshot editável para o aluno. Alterações futuras no modelo não mudam treinos já aplicados.</Text></Card>
  </Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, list: { gap: spacing.sm }, card: { gap: spacing.md }, name: { ...typography.headingMD, color: colors.textPrimary }, meta: { ...typography.caption, color: colors.titanium, marginTop: spacing.xxs }, context: { gap: spacing.xs, backgroundColor: colors.surfaceElevated }, contextTitle: { ...typography.caption, color: colors.titaniumLight } });
