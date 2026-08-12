import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { Alert, StyleSheet, Text, TextInput, View } from 'react-native';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

export default function InviteEntryScreen() {
  const { token } = useLocalSearchParams<{ token: string }>();
  const invite = useQuery({ queryKey: ['student-invite', token], queryFn: () => inviteApi.resolve(token), enabled: Boolean(token) });
  const accept = useMutation({ mutationFn: (input: { firstName: string; lastName: string; email?: string }) => inviteApi.accept(token, input) });
  const saveSession = useInviteSessionStore((state) => state.save);
  const [firstName, setFirstName] = useState(''); const [lastName, setLastName] = useState(''); const [email, setEmail] = useState('');
  if (invite.isLoading) return <LoadingView message="Validando seu convite…" />;
  if (invite.isError) return <ErrorView message={invite.error.message} onRetry={() => invite.refetch()} />;
  const start = async () => { try { const session = await accept.mutateAsync({ firstName, lastName, email: email || invite.data!.email }); saveSession(session); router.replace(`/invite/${token}/anamnesis`); } catch (error) { Alert.alert('Não foi possível começar', error instanceof Error ? error.message : 'Tente novamente.'); } };
  return <Screen style={styles.page}><View style={styles.hero}><Text style={styles.eyebrow}>CONVITE PERSONAL ULTRA</Text><Text style={styles.title}>Você foi convidado por {invite.data!.trainerName}.</Text><Text style={styles.copy}>Vamos registrar seu ponto de partida para o seu personal preparar o acompanhamento.</Text></View><Card style={styles.card}><Text style={styles.cardTitle}>Antes de começar</Text><TextInput value={firstName} onChangeText={setFirstName} placeholder="Seu nome" placeholderTextColor={colors.textMuted} style={styles.input} /><TextInput value={lastName} onChangeText={setLastName} placeholder="Sobrenome (opcional)" placeholderTextColor={colors.textMuted} style={styles.input} />{!invite.data!.email && <TextInput value={email} onChangeText={setEmail} autoCapitalize="none" keyboardType="email-address" placeholder="Seu e-mail" placeholderTextColor={colors.textMuted} style={styles.input} />}<Button loading={accept.isPending} disabled={!firstName.trim() || (!invite.data!.email && !email.trim())} onPress={() => void start()}>Começar anamnese</Button></Card></Screen>;
}

const styles = StyleSheet.create({ page: { justifyContent: 'center', paddingVertical: spacing.xxl, gap: spacing.xxl }, hero: { gap: spacing.sm }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyLG, color: colors.textSecondary, lineHeight: 24 }, card: { gap: spacing.md }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: spacing.md, minHeight: 52 } });
