import { router, useLocalSearchParams } from 'expo-router';
import { Alert, Pressable, StyleSheet, Text, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useDuplicateTrainerTemplate, useTrainerTemplates } from '@/src/features/trainer/training/hooks';
import { feedback } from '@/src/platform/feedback';

export default function TrainerTemplateLibraryScreen() {
  const { studentId } = useLocalSearchParams<{ studentId?: string }>();
  const templates = useTrainerTemplates();
  const student = useTrainerStudent(studentId ?? '');
  const duplicate = useDuplicateTrainerTemplate();

  if (templates.isLoading || (studentId && student.isLoading)) return <LoadingView message="Carregando modelos…" />;
  if (templates.isError) return <ErrorView message={templates.error.message} onRetry={() => templates.refetch()} />;
  if (studentId && student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;

  const studentName = student.data ? `${student.data.firstName} ${student.data.lastName}` : undefined;
  const open = (templateId: string) => router.push({ pathname: '/trainer/training/[id]', params: { id: templateId, ...(studentId ? { studentId } : {}) } });
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

  return <Screen style={styles.page}>
    <TopBar eyebrow="ACELERADOR OPCIONAL" title="Modelos de treino" onBack={() => router.back()} />
    <Text style={styles.copy}>{studentName ? `Escolha um modelo para aplicar a ${studentName}, ou ajuste-o antes.` : 'Crie e mantenha prescrições reutilizáveis. Os treinos dos alunos continuam sendo editados individualmente.'}</Text>
    <Button onPress={() => router.push({ pathname: '/trainer/training/[id]', params: { id: 'new', ...(studentId ? { studentId } : {}) } })}>+ Novo modelo</Button>
    {templates.data!.length === 0 ? <EmptyState title="Nenhum modelo salvo" message="Crie um modelo usando o catálogo para acelerar prescrições recorrentes." /> : <View style={styles.list}>{templates.data!.map((template) => <Card key={template.id} style={styles.card}>
      <Pressable accessibilityRole="button" accessibilityLabel={`Abrir modelo ${template.name}`} onPress={() => open(template.id)} style={({ pressed }) => pressed && styles.pressed}>
        <Text style={styles.name}>{template.name}</Text>
        <Text style={styles.meta}>{template.exerciseCount ?? 0} {(template.exerciseCount ?? 0) === 1 ? 'exercício' : 'exercícios'} · atualizado em {new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short' }).format(new Date(template.updatedAt!))}</Text>
        {template.notes ? <Text numberOfLines={2} style={styles.copy}>{template.notes}</Text> : null}
        <Text style={styles.openLink}>{studentId ? 'Configurar e aplicar ›' : 'Abrir editor ›'}</Text>
      </Pressable>
      <Button variant="secondary" loading={duplicate.isPending && duplicate.variables === template.id} disabled={duplicate.isPending} onPress={() => void duplicateTemplate(template.id, template.name)}>Duplicar modelo</Button>
    </Card>)}</View>}
    <Card style={styles.context}><Text style={styles.contextTitle}>Modelos não são planos ativos</Text><Text style={styles.copy}>Aplicar cria um novo snapshot editável para o aluno. Alterações futuras no modelo não mudam treinos já aplicados.</Text></Card>
  </Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, list: { gap: spacing.sm }, card: { gap: spacing.md }, name: { ...typography.headingMD, color: colors.textPrimary }, meta: { ...typography.caption, color: colors.titanium, marginTop: spacing.xxs }, openLink: { ...typography.caption, color: colors.primary, marginTop: spacing.sm }, pressed: { opacity: .76 }, context: { gap: spacing.xs, backgroundColor: colors.surfaceElevated }, contextTitle: { ...typography.caption, color: colors.titaniumLight } });
