import { router } from 'expo-router';
import { useState } from 'react';
import * as Clipboard from 'expo-clipboard';
import { Alert, Linking, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useCreateStudentInvite } from '@/src/features/trainer/students/hooks';
import type { StudentInvite } from '@/src/api/trainer-client';

export default function TrainerInviteScreen() {
  const createInvite = useCreateStudentInvite();
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [inviteUrl, setInviteUrl] = useState<string>();
  const phoneDigits = phone.replace(/\D/g, '');
  const canSendWhatsApp = phoneDigits.length >= 8 && phoneDigits.length <= 15;

  const create = async () => {
    try {
      const invite = await createInvite.mutateAsync(email);
      // The token is authoritative. Recompose the URL defensively so a stale
      // deployment that returns only the scheme base cannot produce a blank invite.
      setInviteUrl(completeInviteUrl(invite));
    } catch (error) {
      Alert.alert('Não foi possível criar o convite', error instanceof Error ? error.message : 'Tente novamente.');
    }
  };

  const inviteMessage = inviteUrl ? buildInviteMessage(inviteUrl) : undefined;

  const copyInvite = async () => {
    if (!inviteMessage) return;
    await Clipboard.setStringAsync(inviteMessage);
    Alert.alert('Convite copiado', 'A mensagem completa está na área de transferência.');
  };

  const sendWhatsApp = async () => {
    if (!inviteUrl || !canSendWhatsApp) return;
    await Linking.openURL(`https://wa.me/${phoneDigits}?text=${encodeURIComponent(inviteMessage!)}`);
  };

  return <Screen style={styles.page}>
    <TopBar eyebrow="NOVO ALUNO" title="Envie um convite" onBack={() => router.back()} />
    <Text style={styles.copy}>Compartilhe o link para que o aluno faça o cadastro e preencha a anamnese. O convite expira em sete dias.</Text>
    <Card style={styles.card}>
      <Text style={styles.label}>E-mail do aluno <Text style={styles.optional}>(opcional)</Text></Text>
      <TextInput value={email} onChangeText={setEmail} autoCapitalize="none" autoCorrect={false} keyboardType="email-address" placeholder="aluno@email.com" placeholderTextColor={colors.textMuted} accessibilityLabel="E-mail do aluno" style={styles.input} />
      <Text style={styles.label}>Telefone para WhatsApp <Text style={styles.optional}>(opcional)</Text></Text>
      <TextInput value={phone} onChangeText={setPhone} keyboardType="phone-pad" placeholder="(11) 99999-9999" placeholderTextColor={colors.textMuted} accessibilityLabel="Telefone para WhatsApp do aluno" style={styles.input} />
      <Button loading={createInvite.isPending} onPress={() => void create()}>Gerar link de convite</Button>
    </Card>
    {inviteUrl && <Card style={styles.result}><Text style={styles.resultTitle}>Convite pronto</Text><Text style={styles.copy}>Envie o convite completo ou use o WhatsApp para compartilhar as orientações de instalação.</Text><TextInput value={inviteUrl} editable={false} selectTextOnFocus accessibilityLabel="Link de convite" style={styles.link} /><Button variant="secondary" onPress={() => void copyInvite()}>Copiar convite</Button><Button variant="secondary" disabled={!canSendWhatsApp} onPress={() => void sendWhatsApp()}>Enviar pelo WhatsApp</Button>{!canSendWhatsApp && <Text style={styles.hint}>Informe um telefone válido com DDD para enviar pelo WhatsApp.</Text>}</Card>}
  </Screen>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 22 }, card: { gap: spacing.md }, label: { ...typography.bodyLG, color: colors.textPrimary }, optional: { color: colors.textMuted }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, minHeight: 52, paddingHorizontal: spacing.md, backgroundColor: colors.background }, result: { gap: spacing.sm, borderColor: colors.primary }, resultTitle: { ...typography.headingMD, color: colors.textPrimary }, link: { ...typography.bodyMD, color: colors.primary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: spacing.md, backgroundColor: colors.background }, hint: { ...typography.caption, color: colors.textMuted, lineHeight: 18 },
});

function completeInviteUrl(invite: StudentInvite) {
  return invite.inviteUrl.includes(invite.token) ? invite.inviteUrl : `${invite.inviteUrl.replace(/\/+$/, '')}/${invite.token}`;
}

function buildInviteMessage(inviteUrl: string) {
  const androidUrl = process.env.EXPO_PUBLIC_ANDROID_DOWNLOAD_URL;
  const iosUrl = process.env.EXPO_PUBLIC_IOS_DOWNLOAD_URL;
  const installLinks = [androidUrl && `Android: ${androidUrl}`, iosUrl && `iPhone: ${iosUrl}`].filter(Boolean).join('\n');
  return [
    'Você foi convidado(a) para acompanhar seu protocolo no Personal Ultra.',
    '',
    'Já tem o app? Abra seu convite:',
    inviteUrl,
    '',
    'Ainda não tem o app? Instale-o e depois volte a esta mensagem para abrir seu convite.',
    installLinks || 'Os links de instalação serão enviados pelo seu personal.',
  ].join('\n');
}
