import { router } from 'expo-router';
import { useState } from 'react';
import { Alert, StyleSheet, Text, TextInput, View } from 'react-native';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Button, Card, ErrorView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

export default function InviteCodeScreen() {
  const [code, setCode] = useState(''); const normalizedCode = code.replace(/\D/g, '').slice(0, 6);
  const [firstName, setFirstName] = useState(''); const [lastName, setLastName] = useState(''); const [email, setEmail] = useState(''); const [phone, setPhone] = useState('');
  const invite = useQuery({ queryKey: ['student-invite-code', normalizedCode], queryFn: () => inviteApi.resolveCode(normalizedCode), enabled: normalizedCode.length === 6 });
  const accept = useMutation({ mutationFn: () => inviteApi.acceptCode(normalizedCode, { firstName, lastName, email: email || invite.data?.email, phone }) });
  const saveSession = useInviteSessionStore((state) => state.save);
  const start = async () => { try { const session = await accept.mutateAsync(); saveSession(session); router.replace(`/invite/${normalizedCode}/anamnesis`); } catch (error) { Alert.alert('Não foi possível começar', error instanceof Error ? error.message : 'Tente novamente.'); } };

  return <Screen style={styles.page}><TopBar eyebrow="CONVITE" title="Tenho um convite" onBack={() => router.back()} /><Text style={styles.copy}>Informe o código de seis dígitos que seu personal enviou.</Text><TextInput value={code} onChangeText={setCode} keyboardType="number-pad" placeholder="234-567" placeholderTextColor={colors.textMuted} accessibilityLabel="Código de convite" style={styles.code} />{normalizedCode.length === 6 && invite.isError && <ErrorView message={invite.error.message} onRetry={() => invite.refetch()} />}{invite.data && <Card style={styles.card}><Text style={styles.cardTitle}>Convite de {invite.data.trainerName}</Text><TextInput value={firstName} onChangeText={setFirstName} placeholder="Seu nome" placeholderTextColor={colors.textMuted} style={styles.input} /><TextInput value={lastName} onChangeText={setLastName} placeholder="Sobrenome (opcional)" placeholderTextColor={colors.textMuted} style={styles.input} />{!invite.data.email && <TextInput value={email} onChangeText={setEmail} autoCapitalize="none" keyboardType="email-address" placeholder="Seu e-mail" placeholderTextColor={colors.textMuted} style={styles.input} />}<TextInput value={phone} onChangeText={setPhone} keyboardType="phone-pad" placeholder="Telefone com DDD" placeholderTextColor={colors.textMuted} style={styles.input} /><Button loading={accept.isPending} disabled={!firstName.trim() || !phone.trim() || (!invite.data.email && !email.trim())} onPress={() => void start()}>Começar anamnese</Button></Card>}</Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 22 }, code: { ...typography.displayLG, color: colors.primary, letterSpacing: 3, textAlign: 'center', borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: spacing.md, backgroundColor: colors.surface }, card: { gap: spacing.md }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, minHeight: 52, padding: spacing.md } });
