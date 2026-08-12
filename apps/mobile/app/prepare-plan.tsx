import { router } from 'expo-router';
import { Alert, Image, StyleSheet, Text, View } from 'react-native';
import { Button, Card } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useResetCurrentMemberDemo } from '@/src/features/student/api/hooks';
import { useAuthStore } from '@/src/features/student/state/auth-store';
import { feedback } from '@/src/platform/feedback';

export default function AwaitingProtocolScreen() {
  const resetDemo = useResetCurrentMemberDemo();
  const signOut = useAuthStore((state) => state.signOut);
  const reset = async () => {
    try {
      await resetDemo.mutateAsync();
      signOut();
      feedback.success();
      router.replace('/login');
    } catch {
      Alert.alert('Não foi possível recomeçar', 'Confira sua conexão e tente novamente.');
    }
  };
  const confirmReset = () => Alert.alert('Recomeçar demonstração?', 'Seu cadastro e os dados desta jornada de teste serão apagados. Você voltará ao login para começar novamente.', [
    { text: 'Cancelar', style: 'cancel' },
    { text: 'Recomeçar', style: 'destructive', onPress: () => void reset() },
  ]);
  return <Screen style={styles.screen}>
    <View style={styles.hero}>
      <Image source={require('../assets/brand/personal-ultra-logo-horizontal.png')} resizeMode="contain" style={styles.logo} />
      <Text style={styles.eyebrow}>SEU CADASTRO FOI ENVIADO</Text>
      <Text style={styles.title}>Agora é com o{`\n`}seu personal.</Text>
      <Text style={styles.copy}>Ele vai transformar suas respostas em um protocolo pensado para a sua rotina e seus objetivos.</Text>
    </View>
    <View style={styles.content}>
      <Card style={styles.card}>
        <Text style={styles.cardTitle}>O que acontece agora</Text>
        <Text style={styles.cardCopy}>Seu personal revisa seu cadastro, organiza o protocolo e libera tudo neste espaço.</Text>
      </Card>
      <View style={styles.includes}>
        <Text style={styles.includesTitle}>Quando estiver pronto, você terá acesso a</Text>
        <Text style={styles.include}>• Seus treinos e registros</Text>
        <Text style={styles.include}>• Sua estratégia alimentar</Text>
        <Text style={styles.include}>• Orientações para evoluir</Text>
      </View>
      <View style={styles.actions}>
        <Button variant="secondary" loading={resetDemo.isPending} onPress={confirmReset}>Recomeçar cadastro</Button>
        <Button variant="ghost" disabled={resetDemo.isPending} onPress={() => { signOut(); router.replace('/login'); }}>Sair</Button>
      </View>
    </View>
  </Screen>;
}

const styles = StyleSheet.create({
  screen: { paddingVertical: spacing.xl, gap: spacing.xxl }, hero: { alignItems: 'center', gap: spacing.md, paddingTop: spacing.sm }, logo: { width: 156, height: 54, marginBottom: spacing.md },
  eyebrow: { ...typography.caption, color: colors.signalGreen, letterSpacing: 1.1 }, title: { ...typography.displayLG, color: colors.textPrimary, textAlign: 'center' }, copy: { ...typography.bodyLG, color: colors.textSecondary, textAlign: 'center', lineHeight: 24, maxWidth: 320 },
  content: { gap: spacing.xl }, card: { gap: spacing.xs, backgroundColor: colors.surfaceElevated, borderColor: colors.border }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, cardCopy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
  includes: { gap: spacing.sm, paddingHorizontal: spacing.xs }, includesTitle: { ...typography.headingMD, color: colors.textPrimary }, include: { ...typography.bodyMD, color: colors.textSecondary }, actions: { gap: spacing.sm, marginTop: spacing.sm },
});
