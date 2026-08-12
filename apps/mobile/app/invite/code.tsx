import { router } from 'expo-router';
import { useState } from 'react';
import { Alert, StyleSheet, Text, TextInput, View } from 'react-native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Button, Card } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { PhoneInput } from '@/src/shared/forms/phone-input';

export default function InviteCodeScreen() {
  const [code, setCode] = useState(''); const normalizedCode = code.replace(/\D/g, '').slice(0, 6);
  const queryClient = useQueryClient();
  const [firstName, setFirstName] = useState(''); const [lastName, setLastName] = useState(''); const [email, setEmail] = useState(''); const [phone, setPhone] = useState('');
  const invite = useQuery({ queryKey: ['student-invite-code', normalizedCode], queryFn: () => inviteApi.resolveCode(normalizedCode), enabled: normalizedCode.length === 6 });
  const accept = useMutation({ mutationFn: () => inviteApi.acceptCode(normalizedCode, { firstName, lastName, email: email || invite.data?.email, phone }) });
  const saveSession = useInviteSessionStore((state) => state.save);
  const start = async () => { try { const session = await accept.mutateAsync(); queryClient.removeQueries({ queryKey: ['student-invite-code', normalizedCode] }); saveSession(session); router.replace(`/invite/${normalizedCode}/anamnesis`); } catch (error) { Alert.alert('Não foi possível criar o cadastro', error instanceof Error ? error.message : 'Tente novamente.'); } };

  return <Screen style={styles.page}><TopBar eyebrow="CONVITE" title="Tenho um convite" onBack={() => router.back()} /><Text style={styles.copy}>Informe o código de seis dígitos que seu personal enviou.</Text><TextInput value={code} onChangeText={setCode} keyboardType="number-pad" placeholder="234-567" placeholderTextColor={colors.textMuted} accessibilityLabel="Código de convite" style={styles.code} />{normalizedCode.length === 6 && invite.isError && <Card style={styles.errorCard}><Text style={styles.errorTitle}>Não reconhecemos seu código de convite.</Text><Text style={styles.copy}>Por favor, solicite um novo convite para o seu treinador.</Text><Button variant="secondary" onPress={() => setCode('')}>Inserir outro código</Button></Card>}{invite.data && !invite.isError && <Card style={styles.card}><Text style={styles.cardTitle}>Convite de {invite.data.trainerName}</Text><TextInput value={firstName} onChangeText={setFirstName} placeholder="Seu nome" placeholderTextColor={colors.textMuted} style={styles.input} /><TextInput value={lastName} onChangeText={setLastName} placeholder="Sobrenome (opcional)" placeholderTextColor={colors.textMuted} style={styles.input} />{!invite.data.email && <TextInput value={email} onChangeText={setEmail} autoCapitalize="none" keyboardType="email-address" placeholder="Seu e-mail" placeholderTextColor={colors.textMuted} style={styles.input} />}<PhoneInput value={phone} onChangeText={setPhone} placeholder="Telefone com DDD" placeholderTextColor={colors.textMuted} style={styles.input} /><Button loading={accept.isPending} disabled={!firstName.trim() || !phone.trim() || (!invite.data.email && !email.trim())} onPress={() => void start()}>Criar cadastro</Button></Card>}</Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 22 }, code: { ...typography.displayLG, color: colors.primary, letterSpacing: 3, textAlign: 'center', borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: spacing.md, backgroundColor: colors.surface }, card: { gap: spacing.md }, errorCard: { gap: spacing.md, borderColor: colors.primary }, errorTitle: { ...typography.headingMD, color: colors.textPrimary }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, minHeight: 52, padding: spacing.md } });
