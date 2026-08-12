import { router } from 'expo-router';
import { Alert, StyleSheet, Text, View } from 'react-native';
import { Button, Card, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
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
    <TopBar eyebrow="PRÓXIMO PASSO" title="Seu acompanhamento começa aqui." />
    <View style={styles.content}>
      <View style={styles.mark}><Text style={styles.markText}>PU</Text></View>
      <Tag tone="primary">AGUARDANDO PROTOCOLO</Tag>
      <Text style={styles.title}>Seu personal está preparando seu protocolo.</Text>
      <Text style={styles.copy}>Em breve, você verá aqui seus treinos, sua alimentação e as orientações definidas especialmente para o seu acompanhamento.</Text>
      <Card style={styles.card}><Text style={styles.cardTitle}>Enquanto isso</Text><Text style={styles.cardCopy}>Seu cadastro foi recebido. Quando o personal disponibilizar seu protocolo, ele aparecerá neste espaço.</Text></Card>
      <View style={styles.actions}>
        <Button variant="secondary" loading={resetDemo.isPending} onPress={confirmReset}>Recomeçar cadastro</Button>
        <Button variant="ghost" disabled={resetDemo.isPending} onPress={() => { signOut(); router.replace('/login'); }}>Sair</Button>
      </View>
    </View>
  </Screen>;
}

const styles = StyleSheet.create({
  screen: { justifyContent: 'center' }, content: { alignItems: 'center', gap: spacing.lg },
  mark: { width: 88, height: 88, borderRadius: 44, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.primary }, markText: { ...typography.headingLG, color: colors.background },
  title: { ...typography.displayLG, color: colors.textPrimary, textAlign: 'center' }, copy: { ...typography.bodyLG, color: colors.textSecondary, textAlign: 'center', lineHeight: 24 },
  card: { width: '100%', gap: spacing.xs, backgroundColor: colors.surfaceElevated, borderColor: colors.border }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, cardCopy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, actions: { width: '100%', gap: spacing.sm },
});
