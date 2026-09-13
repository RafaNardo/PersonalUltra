import { useUser } from '@clerk/expo';
import { useMutation } from '@tanstack/react-query';
import { router } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

export default function StudentOnboardingScreen() {
  const { user } = useUser(); const save = useInviteSessionStore((state) => state.save);
  const [code, setCode] = useState(''); const [firstName, setFirstName] = useState(user?.firstName ?? ''); const [lastName, setLastName] = useState(user?.lastName ?? '');
  const claim = useMutation({ mutationFn: () => inviteApi.claimInvite({ code, firstName, lastName }), onSuccess: (result) => { if (!result.studentId) return; save({ accessToken: '', studentId: result.studentId, firstName, lastName, phone: '', email: user?.primaryEmailAddress?.emailAddress ?? '', trainerId: '' }); router.replace('/student-access'); } });
  return <Screen style={styles.page}><View style={styles.hero}><Text style={styles.eyebrow}>PRIMEIRO PASSO</Text><Text style={styles.title}>Digite o código enviado pelo seu personal.</Text><Text style={styles.copy}>O convite vincula sua conta ao acompanhamento correto.</Text></View><Card style={styles.card}><TextInput value={code} onChangeText={(value) => setCode(value.replace(/\D/g, '').slice(0, 6))} keyboardType="number-pad" placeholder="Código de 6 dígitos" placeholderTextColor={colors.textMuted} style={styles.input} /><TextInput value={firstName} onChangeText={setFirstName} placeholder="Seu nome" placeholderTextColor={colors.textMuted} style={styles.input} /><TextInput value={lastName} onChangeText={setLastName} placeholder="Sobrenome (opcional)" placeholderTextColor={colors.textMuted} style={styles.input} />{claim.error ? <Text style={styles.error}>{claim.error.message}</Text> : null}<Button loading={claim.isPending} disabled={code.length !== 6 || !firstName.trim()} onPress={() => void claim.mutateAsync()}>Vincular meu convite</Button><Button variant="ghost" onPress={() => router.back()}>Voltar</Button></Card></Screen>;
}
const styles = StyleSheet.create({ page: { justifyContent: 'center', gap: spacing.xxl }, hero: { gap: spacing.md }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1.4 }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary }, card: { gap: spacing.md }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, paddingHorizontal: spacing.md, paddingVertical: spacing.md }, error: { ...typography.bodyMD, color: colors.danger } });
