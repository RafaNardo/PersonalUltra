import { router, useLocalSearchParams } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';
import { Button, Card } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

export default function InviteCompleteScreen() {
  const { token } = useLocalSearchParams<{ token: string }>();
  const clear = useInviteSessionStore((state) => state.clear);
  return <Screen style={styles.page}><View style={styles.hero}><Text style={styles.eyebrow}>ANAMNESE ENVIADA</Text><Text style={styles.title}>Seu personal já recebeu seu ponto de partida.</Text><Text style={styles.copy}>Em breve, ele liberará seu protocolo de acompanhamento.</Text></View><Card style={styles.card}><Text style={styles.cardText}>Você poderá voltar por este convite enquanto a demonstração estiver ativa.</Text><Button variant="secondary" onPress={() => { clear(); router.replace(`/invite/${token}`); }}>Voltar ao convite</Button></Card></Screen>;
}

const styles = StyleSheet.create({ page: { justifyContent: 'center', gap: spacing.xxl }, hero: { gap: spacing.sm }, eyebrow: { ...typography.caption, color: colors.signalGreen, letterSpacing: 1 }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyLG, color: colors.textSecondary, lineHeight: 24 }, card: { gap: spacing.md }, cardText: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 } });
