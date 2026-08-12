import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { Alert, Linking, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useCreateTrainerMessage, useTrainerAnamnesis, useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { feedback } from '@/src/platform/feedback';

export default function TrainerStudentDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const student = useTrainerStudent(id);
  const createMessage = useCreateTrainerMessage(id);
  const anamnesis = useTrainerAnamnesis(id, student.data?.anamnesisStatus === 'Completed');
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
      <View style={styles.row}><Text style={styles.label}>Telefone</Text><Text style={styles.value}>{data.phone ?? 'Não informado'}</Text></View>
      <View style={styles.row}><Text style={styles.label}>Aluno desde</Text><Text style={styles.value}>{new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium' }).format(new Date(data.startedAt))}</Text></View>
      {data.phone && <Button variant="secondary" onPress={() => void Linking.openURL(`https://wa.me/${data.phone!.replace(/\D/g, '')}`)}>Abrir conversa no WhatsApp</Button>}
    </Card>
    <Card style={styles.card}>
      <Text style={styles.cardTitle}>Anamnese</Text>
      <Tag tone={data.anamnesisStatus === 'Completed' ? 'success' : 'neutral'}>{anamnesisLabel(data.anamnesisStatus)}</Tag>
      <Text style={styles.copy}>{anamnesisCopy(data.anamnesisStatus)}</Text>
      {anamnesis.data && <View style={styles.anamnesis}><Detail label="Objetivo" value={anamnesis.data.goal} /><Detail label="Experiência" value={anamnesis.data.experienceLevel} /><Detail label="Rotina" value={`${anamnesis.data.trainingDaysPerWeek} dias · ${anamnesis.data.sessionDurationMinutes} min`} /><Detail label="Local" value={`${anamnesis.data.trainingLocation} · ${anamnesis.data.equipmentNotes}`} /><Detail label="Dados físicos" value={`${anamnesis.data.heightCm} cm · ${anamnesis.data.weightKg} kg`} /><Detail label="Cuidados" value={anamnesis.data.healthConditions} /><Detail label="Limitações" value={anamnesis.data.movementRestrictions} /><Detail label="Dor atual" value={anamnesis.data.currentPainDescription} /><Detail label="Nutrição" value={`${anamnesis.data.nutritionPreferences} · ${anamnesis.data.nutritionRestrictions}`} /></View>}
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

function Detail({ label, value }: { label: string; value: string }) { return <View style={styles.detail}><Text style={styles.label}>{label}</Text><Text style={styles.detailValue}>{value}</Text></View>; }

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, card: { gap: spacing.md }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, row: { gap: spacing.xxs }, label: { ...typography.caption, color: colors.textMuted }, value: { ...typography.bodyLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, anamnesis: { gap: spacing.sm }, detail: { gap: spacing.xxs }, detailValue: { ...typography.bodyMD, color: colors.textPrimary }, input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 112, borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: spacing.md, textAlignVertical: 'top' },
});
