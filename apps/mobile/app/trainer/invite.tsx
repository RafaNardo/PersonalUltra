import { router } from 'expo-router';
import { useState } from 'react';
import * as Clipboard from 'expo-clipboard';
import { Alert, Linking, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useCreateStudentInvite } from '@/src/features/trainer/students/hooks';
import type { StudentInvite } from '@/src/api/trainer-client';
import { PhoneInput } from '@/src/shared/forms/phone-input';

export default function TrainerInviteScreen() {
  const createInvite = useCreateStudentInvite();
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [inviteCode, setInviteCode] = useState<string>();
  const phoneDigits = phone.replace(/\D/g, '');
  const canSendWhatsApp = phoneDigits.length >= 8 && phoneDigits.length <= 15;

  const create = async () => {
    try {
      const invite = await createInvite.mutateAsync(email);
      setInviteCode(formatInviteCode(invite.inviteCode));
    } catch (error) {
      Alert.alert('Não foi possível criar o convite', error instanceof Error ? error.message : 'Tente novamente.');
    }
  };

  const inviteMessage = inviteCode ? buildInviteMessage(inviteCode) : undefined;

  const copyInvite = async () => {
    if (!inviteMessage) return;
    await Clipboard.setStringAsync(inviteMessage);
    Alert.alert('Convite copiado', 'A mensagem completa está na área de transferência.');
  };

  const sendWhatsApp = async () => {
    if (!inviteCode || !canSendWhatsApp) return;
    await Linking.openURL(`https://wa.me/${phoneDigits}?text=${encodeURIComponent(inviteMessage!)}`);
  };

  return <Screen style={styles.page}>
    <TopBar eyebrow="NOVO ALUNO" title="Envie um convite" onBack={() => router.back()} />
    <Text style={styles.copy}>Compartilhe o código para que o aluno faça o cadastro e preencha a anamnese. O convite expira em sete dias.</Text>
    <Card style={styles.card}>
      <Text style={styles.label}>E-mail do aluno <Text style={styles.optional}>(opcional)</Text></Text>
      <TextInput value={email} onChangeText={setEmail} autoCapitalize="none" autoCorrect={false} keyboardType="email-address" placeholder="aluno@email.com" placeholderTextColor={colors.textMuted} accessibilityLabel="E-mail do aluno" style={styles.input} />
      <Text style={styles.label}>Telefone para WhatsApp <Text style={styles.optional}>(opcional)</Text></Text>
      <PhoneInput value={phone} onChangeText={setPhone} placeholder="(11) 99999-9999" placeholderTextColor={colors.textMuted} accessibilityLabel="Telefone para WhatsApp do aluno" style={styles.input} />
      <Button loading={createInvite.isPending} onPress={() => void create()}>Gerar link de convite</Button>
    </Card>
    {inviteCode && <Card style={styles.result}><Text style={styles.resultTitle}>Convite pronto</Text><Text style={styles.copy}>O aluno instala o app, escolhe “Tenho um convite” e informa este código.</Text><TextInput value={inviteCode} editable={false} selectTextOnFocus accessibilityLabel="Código de convite" style={styles.code} /><Button variant="secondary" onPress={() => void copyInvite()}>Copiar convite</Button><Button variant="secondary" disabled={!canSendWhatsApp} onPress={() => void sendWhatsApp()}>Enviar pelo WhatsApp</Button>{!canSendWhatsApp && <Text style={styles.hint}>Informe um telefone válido com DDD para enviar pelo WhatsApp.</Text>}</Card>}
  </Screen>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 22 }, card: { gap: spacing.md }, label: { ...typography.bodyLG, color: colors.textPrimary }, optional: { color: colors.textMuted }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, minHeight: 52, paddingHorizontal: spacing.md, backgroundColor: colors.background }, result: { gap: spacing.sm, borderColor: colors.primary }, resultTitle: { ...typography.headingMD, color: colors.textPrimary }, code: { ...typography.displayLG, color: colors.primary, letterSpacing: 3, textAlign: 'center', borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: spacing.md, backgroundColor: colors.background }, hint: { ...typography.caption, color: colors.textMuted, lineHeight: 18 },
});

function formatInviteCode(code: string) { return `${code.slice(0, 3)}-${code.slice(3)}`; }

function buildInviteMessage(inviteCode: string) {
  const androidUrl = process.env.EXPO_PUBLIC_ANDROID_DOWNLOAD_URL;
  const iosUrl = process.env.EXPO_PUBLIC_IOS_DOWNLOAD_URL;
  const installLinks = [androidUrl && `Android: ${androidUrl}`, iosUrl && `iPhone: ${iosUrl}`].filter(Boolean).join('\n');
  return [
    'Você foi convidado(a) para acompanhar seu protocolo no Personal Ultra.',
    '',
    'Instale o app, escolha “Tenho um convite” e informe este código:',
    inviteCode,
    '',
    'Ainda não tem o app? Instale-o e depois volte a esta mensagem para consultar o código.',
    installLinks || 'Os links de instalação serão enviados pelo seu personal.',
  ].join('\n');
}
