import { router } from 'expo-router';
import { Image, StyleSheet, Text, TextInput, View } from 'react-native';
import { useState } from 'react';
import { useDevLogin } from '@/src/features/student/api/hooks';
import { Button, Card } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useAuthStore } from '@/src/features/student/state/auth-store';
import { feedback } from '@/src/platform/feedback';
import { telemetry } from '@/src/platform/telemetry';

export default function LoginScreen() {
  const login = useDevLogin();
  const signIn = useAuthStore((state) => state.signIn);
  const [email, setEmail] = useState('');
  const handleLogin = async () => {
    const normalizedEmail = email.trim();
    if (!normalizedEmail) return;
    const session = await login.mutateAsync(normalizedEmail);
    feedback.success();
    telemetry.event('demo_login_completed');
    signIn(session.accessToken, session.member.firstName);
    router.replace('/');
  };

  return <Screen style={styles.screen}>
    <View style={styles.hero}><Image source={require('../assets/brand/personal-ultra-logo-horizontal.png')} resizeMode="contain" style={styles.logo} /><Text style={styles.title}>Consultoria de alta{`\n`}performance.</Text><Text style={styles.copy}>Seu treino de hoje, com clareza para evoluir em cada sessão.</Text></View>
    <Card style={styles.card}><Text style={styles.cardTitle}>Comece pelo seu e-mail</Text><Text style={styles.cardCopy}>Se você já iniciou sua jornada, continuamos de onde parou. Novos alunos seguem para o onboarding.</Text><TextInput value={email} onChangeText={setEmail} autoCapitalize="none" autoCorrect={false} keyboardType="email-address" placeholder="seuemail@exemplo.com" placeholderTextColor={colors.textMuted} accessibilityLabel="Seu e-mail" style={styles.input} />{login.error && <Text style={styles.error}>{login.error.message}</Text>}<Button onPress={handleLogin} loading={login.isPending} disabled={!email.trim()}>Continuar</Button></Card>
  </Screen>;
}

const styles = StyleSheet.create({
  screen: { justifyContent: 'space-between', paddingVertical: spacing.xxxl }, hero: { gap: spacing.lg }, logo: { width: 220, height: 124, alignSelf: 'center' }, title: { ...typography.displayXL, color: colors.textPrimary }, copy: { ...typography.bodyLG, color: colors.textSecondary, maxWidth: 310 },
  card: { gap: spacing.md }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, cardCopy: { ...typography.bodyMD, color: colors.textSecondary }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, paddingHorizontal: spacing.md, paddingVertical: spacing.md }, error: { ...typography.bodyMD, color: colors.danger },
});
