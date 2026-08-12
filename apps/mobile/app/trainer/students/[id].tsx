import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { Alert, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useCreateTrainerMessage, useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { feedback } from '@/src/platform/feedback';

export default function TrainerStudentDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const student = useTrainerStudent(id);
  const createMessage = useCreateTrainerMessage(id);
  const [message, setMessage] = useState('');
  if (student.isLoading) return <LoadingView message="Carregando o aluno…" />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;

  const data = student.data!;
  const sendMessage = async () => {
    if (!message.trim()) return;
    try {
      await createMessage.mutateAsync(message.trim());
      setMessage('');
      feedback.success();
    } catch (error) {
      Alert.alert('Não foi possível enviar', error instanceof Error ? error.message : 'Tente novamente.');
    }
  };
  return <Screen style={styles.page}>
    <TopBar eyebrow="DETALHE DO ALUNO" title={`${data.firstName} ${data.lastName}`} onBack={() => router.back()} />
    <Card style={styles.card}>
      <Text style={styles.cardTitle}>Resumo</Text>
      <View style={styles.row}><Text style={styles.label}>E-mail</Text><Text style={styles.value}>{data.email ?? 'Não informado'}</Text></View>
      <View style={styles.row}><Text style={styles.label}>Aluno desde</Text><Text style={styles.value}>{new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium' }).format(new Date(data.startedAt))}</Text></View>
    </Card>
    <Card style={styles.card}>
      <Text style={styles.cardTitle}>Anamnese</Text>
      <Tag tone={data.anamnesisStatus === 'Completed' ? 'success' : 'neutral'}>{anamnesisLabel(data.anamnesisStatus)}</Tag>
      <Text style={styles.copy}>{anamnesisCopy(data.anamnesisStatus)}</Text>
    </Card>
    <Card style={styles.card}>
      <Text style={styles.cardTitle}>Mensagem para {data.firstName}</Text>
      <Text style={styles.copy}>Ela aparecerá no acompanhamento do aluno assim que essa etapa for liberada.</Text>
      <TextInput value={message} onChangeText={setMessage} multiline maxLength={1000} placeholder="Ex.: Bora treinar hoje." placeholderTextColor={colors.textMuted} accessibilityLabel="Mensagem para o aluno" style={styles.input} />
      <Button loading={createMessage.isPending} disabled={!message.trim()} onPress={() => void sendMessage()}>Enviar mensagem</Button>
    </Card>
  </Screen>;
}

function anamnesisLabel(status: 'NotStarted' | 'InProgress' | 'Completed') {
  return status === 'Completed' ? 'CONCLUÍDA' : status === 'InProgress' ? 'EM PREENCHIMENTO' : 'AGUARDANDO ANAMNESE';
}

function anamnesisCopy(status: 'NotStarted' | 'InProgress' | 'Completed') {
  return status === 'Completed' ? 'As informações da anamnese já estão disponíveis para consulta.' : status === 'InProgress' ? 'O aluno começou a preencher a anamnese.' : 'A anamnese ainda não foi iniciada pelo aluno.';
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, card: { gap: spacing.md }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, row: { gap: spacing.xxs }, label: { ...typography.caption, color: colors.textMuted }, value: { ...typography.bodyLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 112, borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: spacing.md, textAlignVertical: 'top' },
});
