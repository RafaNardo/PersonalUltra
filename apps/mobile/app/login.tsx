import { useHostedAuth } from '@clerk/expo/hosted-auth';
import { router } from 'expo-router';
import { Image, StyleSheet, Text, View } from 'react-native';
import { useState } from 'react';
import { Button, Card } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';

export default function LoginScreen() {
  const { startHostedAuth } = useHostedAuth();
  const [pending, setPending] = useState<'sign-in' | 'sign-up'>();
  const open = async (mode: 'sign-in' | 'sign-up') => {
    setPending(mode);
    try { const result = await startHostedAuth({ mode }); if (result.createdSessionId) router.replace('/auth-choice'); }
    finally { setPending(undefined); }
  };

  return <Screen style={styles.screen}>
    <View style={styles.hero}><Image source={require('../assets/brand/personal-ultra-logo-horizontal.png')} resizeMode="contain" style={styles.logo} /><Text style={styles.title}>Acompanhamento{`\n`}que evolui com você.</Text><Text style={styles.copy}>Treino, alimentação e orientações do seu personal em um só lugar.</Text></View>
    <Card style={styles.card}><Text style={styles.cardTitle}>Boas-vindas</Text><Text style={styles.cardCopy}>Use sua conta para entrar. Se for seu primeiro acesso, crie uma conta segura.</Text><Button onPress={() => void open('sign-in')} loading={pending === 'sign-in'} disabled={Boolean(pending)}>Entrar</Button><Button variant="secondary" onPress={() => void open('sign-up')} loading={pending === 'sign-up'} disabled={Boolean(pending)}>Criar conta</Button></Card>
  </Screen>;
}

const styles = StyleSheet.create({
  screen: { justifyContent: 'space-between', paddingVertical: spacing.xxxl }, hero: { gap: spacing.lg }, logo: { width: 220, height: 124, alignSelf: 'center' }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyLG, color: colors.textSecondary, maxWidth: 310 },
  card: { gap: spacing.md }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, cardCopy: { ...typography.bodyMD, color: colors.textSecondary },
});
