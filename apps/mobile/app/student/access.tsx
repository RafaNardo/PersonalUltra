import { router } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

export default function StudentAccessScreen() {
  const session = useInviteSessionStore((state) => state.session); const clear = useInviteSessionStore((state) => state.clear);
  const message = useQuery({ queryKey: ['student', 'trainer-message', session?.studentId], queryFn: () => inviteApi.activeTrainerMessage(session!.accessToken), enabled: Boolean(session?.accessToken) });
  if (!session) { router.replace('/login'); return null; }
  if (message.isLoading) return <LoadingView message="Abrindo seu acompanhamento…" />;
  if (message.isError) return <ErrorView message={message.error.message} onRetry={() => message.refetch()} />;
  return <Screen style={styles.page}><View style={styles.hero}><Text style={styles.eyebrow}>SEU ACOMPANHAMENTO</Text><Text style={styles.title}>Olá, {session.firstName}.</Text><Text style={styles.copy}>Seu personal já recebeu seu ponto de partida. Em breve, ele liberará seu protocolo.</Text></View>{message.data && <Card style={styles.message}><Text style={styles.messageEyebrow}>MENSAGEM DO SEU PERSONAL</Text><Text style={styles.messageText}>{message.data.message}</Text></Card>}<Button variant="ghost" onPress={() => { clear(); router.replace('/login'); }}>Sair</Button></Screen>;
}

const styles = StyleSheet.create({ page: { justifyContent: 'center', gap: spacing.xxl }, hero: { gap: spacing.sm }, eyebrow: { ...typography.caption, color: colors.signalGreen, letterSpacing: 1 }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyLG, color: colors.textSecondary, lineHeight: 24 }, message: { gap: spacing.sm, borderColor: colors.primary }, messageEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, messageText: { ...typography.bodyLG, color: colors.textPrimary, lineHeight: 24 } });
