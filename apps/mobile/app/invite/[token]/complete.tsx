import { router, useLocalSearchParams } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Button, Card } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { inviteApi } from '@/src/features/student/invite/api';

export default function InviteCompleteScreen() {
  const { token } = useLocalSearchParams<{ token: string }>();
  const clear = useInviteSessionStore((state) => state.clear);
  const session = useInviteSessionStore((state) => state.session);
  const trainerMessage = useQuery({ queryKey: ['student', 'trainer-message', session?.studentId], queryFn: () => inviteApi.activeTrainerMessage(session!.accessToken), enabled: Boolean(session?.accessToken) });
  return <Screen style={styles.page}><View style={styles.hero}><Text style={styles.eyebrow}>ANAMNESE ENVIADA</Text><Text style={styles.title}>Seu personal já recebeu seu ponto de partida.</Text><Text style={styles.copy}>Em breve, ele liberará seu protocolo de acompanhamento.</Text></View>{trainerMessage.data && <Card style={styles.message}><Text style={styles.messageEyebrow}>MENSAGEM DO SEU PERSONAL</Text><Text style={styles.messageText}>{trainerMessage.data.message}</Text></Card>}<Card style={styles.card}><Text style={styles.cardText}>Você poderá voltar por este convite enquanto a demonstração estiver ativa.</Text><Button variant="secondary" onPress={() => { clear(); router.replace(`/invite/${token}`); }}>Voltar ao convite</Button></Card></Screen>;
}

const styles = StyleSheet.create({ page: { justifyContent: 'center', gap: spacing.xxl }, hero: { gap: spacing.sm }, eyebrow: { ...typography.caption, color: colors.signalGreen, letterSpacing: 1 }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyLG, color: colors.textSecondary, lineHeight: 24 }, card: { gap: spacing.md }, cardText: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, message: { gap: spacing.sm, borderColor: colors.primary }, messageEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, messageText: { ...typography.bodyLG, color: colors.textPrimary, lineHeight: 24 } });
