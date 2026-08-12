import { router } from 'expo-router';
import { Image, StyleSheet, Text, TextInput, View } from 'react-native';
import { useState } from 'react';
import { Button, Card } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { inviteApi } from '@/src/features/student/invite/api';
import { useMutation } from '@tanstack/react-query';
import { feedback } from '@/src/platform/feedback';
import { telemetry } from '@/src/platform/telemetry';

export default function LoginScreen() {
  const login = useMutation({ mutationFn: inviteApi.studentLogin });
  const saveSession = useInviteSessionStore((state) => state.save);
  const [email, setEmail] = useState('');
  const handleLogin = async () => {
    const normalizedEmail = email.trim();
    if (!normalizedEmail) return;
    const session = await login.mutateAsync(normalizedEmail);
    feedback.success();
    telemetry.event('demo_login_completed');
    saveSession(session);
    router.replace('/student-access');
  };

  return <Screen style={styles.screen}>
    <View style={styles.hero}><Image source={require('../assets/brand/personal-ultra-logo-horizontal.png')} resizeMode="contain" style={styles.logo} /><Text style={styles.title}>Acompanhamento{`\n`}que evolui com você.</Text><Text style={styles.copy}>Treino, alimentação e orientações do seu personal em um só lugar.</Text></View>
    <Card style={styles.card}><Text style={styles.cardTitle}>Já sou aluno</Text><Text style={styles.cardCopy}>Informe o e-mail usado no seu cadastro para acessar seu acompanhamento.</Text><TextInput value={email} onChangeText={setEmail} autoCapitalize="none" autoCorrect={false} keyboardType="email-address" placeholder="seuemail@exemplo.com" placeholderTextColor={colors.textMuted} accessibilityLabel="Seu e-mail" style={styles.input} />{login.error && <Text style={styles.error}>{login.error.message}</Text>}<Button onPress={handleLogin} loading={login.isPending} disabled={!email.trim()}>Entrar</Button><Button variant="ghost" onPress={() => router.push('/invite/code')}>Tenho um convite</Button><Button variant="ghost" onPress={() => router.replace('/demo-role-switch')}>Trocar contexto</Button></Card>
  </Screen>;
}

const styles = StyleSheet.create({
  screen: { justifyContent: 'space-between', paddingVertical: spacing.xxxl }, hero: { gap: spacing.lg }, logo: { width: 220, height: 124, alignSelf: 'center' }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyLG, color: colors.textSecondary, maxWidth: 310 },
  card: { gap: spacing.md }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, cardCopy: { ...typography.bodyMD, color: colors.textSecondary }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, paddingHorizontal: spacing.md, paddingVertical: spacing.md }, error: { ...typography.bodyMD, color: colors.danger },
});
