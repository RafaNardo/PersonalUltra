import { router } from 'expo-router';
import { useState } from 'react';
import { Alert, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useCreateStudentInvite } from '@/src/features/trainer/students/hooks';

export default function TrainerInviteScreen() {
  const createInvite = useCreateStudentInvite();
  const [email, setEmail] = useState('');
  const [inviteUrl, setInviteUrl] = useState<string>();

  const create = async () => {
    try {
      const invite = await createInvite.mutateAsync(email);
      setInviteUrl(invite.inviteUrl);
    } catch (error) {
      Alert.alert('Não foi possível criar o convite', error instanceof Error ? error.message : 'Tente novamente.');
    }
  };

  return <Screen style={styles.page}>
    <TopBar eyebrow="NOVO ALUNO" title="Envie um convite" onBack={() => router.back()} />
    <Text style={styles.copy}>Compartilhe o link para que o aluno faça o cadastro e preencha a anamnese. O convite expira em sete dias.</Text>
    <Card style={styles.card}>
      <Text style={styles.label}>E-mail do aluno <Text style={styles.optional}>(opcional)</Text></Text>
      <TextInput value={email} onChangeText={setEmail} autoCapitalize="none" autoCorrect={false} keyboardType="email-address" placeholder="aluno@email.com" placeholderTextColor={colors.textMuted} accessibilityLabel="E-mail do aluno" style={styles.input} />
      <Button loading={createInvite.isPending} onPress={() => void create()}>Gerar link de convite</Button>
    </Card>
    {inviteUrl && <Card style={styles.result}><Text style={styles.resultTitle}>Convite pronto</Text><Text style={styles.copy}>Copie e envie este link ao aluno.</Text><TextInput value={inviteUrl} editable={false} selectTextOnFocus accessibilityLabel="Link de convite para copiar" style={styles.link} /></Card>}
  </Screen>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 22 }, card: { gap: spacing.md }, label: { ...typography.bodyLG, color: colors.textPrimary }, optional: { color: colors.textMuted }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, minHeight: 52, paddingHorizontal: spacing.md, backgroundColor: colors.background }, result: { gap: spacing.sm, borderColor: colors.primary }, resultTitle: { ...typography.headingMD, color: colors.textPrimary }, link: { ...typography.bodyMD, color: colors.primary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: spacing.md, backgroundColor: colors.background },
});
