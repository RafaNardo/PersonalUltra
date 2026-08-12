import { router } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from './api';
import { useInviteSessionStore } from './session-store';

export function StudentWaitingHome() {
  const session = useInviteSessionStore((state) => state.session); const clear = useInviteSessionStore((state) => state.clear);
  const trainerMessage = useQuery({ queryKey: ['student', 'trainer-message', session?.studentId], queryFn: () => inviteApi.activeTrainerMessage(session!.accessToken), enabled: Boolean(session?.accessToken) });
  if (!session) { router.replace('/login'); return null; }
  if (trainerMessage.isLoading) return <LoadingView message="Abrindo seu acompanhamento…" />;
  if (trainerMessage.isError) return <ErrorView message={trainerMessage.error.message} onRetry={() => trainerMessage.refetch()} />;
  return <Screen style={styles.page}><View style={styles.hero}><Text style={styles.eyebrow}>SEU ACOMPANHAMENTO</Text><Text style={styles.title}>Seu personal já recebeu seu ponto de partida.</Text><Text style={styles.copy}>Em breve, ele liberará seu protocolo de acompanhamento.</Text></View>{trainerMessage.data && <Card style={styles.message}><Text style={styles.messageEyebrow}>MENSAGEM DO SEU PERSONAL</Text><Text style={styles.messageText}>{trainerMessage.data.message}</Text></Card>}<Card style={styles.card}><Text style={styles.cardTitle}>O que acontece agora</Text><Text style={styles.cardText}>Seu personal revisa seu cadastro, organiza o protocolo e libera tudo neste espaço.</Text></Card><View style={styles.includes}><Text style={styles.includesTitle}>Quando estiver pronto, você terá acesso a</Text><Text style={styles.include}>• Seus treinos e registros</Text><Text style={styles.include}>• Sua estratégia alimentar</Text><Text style={styles.include}>• Orientações para evoluir</Text></View><Button variant="ghost" onPress={() => { clear(); router.replace('/login'); }}>Sair</Button></Screen>;
}

const styles = StyleSheet.create({ page: { justifyContent: 'center', gap: spacing.xxl }, hero: { gap: spacing.sm }, eyebrow: { ...typography.caption, color: colors.signalGreen, letterSpacing: 1 }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyLG, color: colors.textSecondary, lineHeight: 24 }, card: { gap: spacing.md }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, cardText: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, message: { gap: spacing.sm, borderColor: colors.primary }, messageEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, messageText: { ...typography.bodyLG, color: colors.textPrimary, lineHeight: 24 }, includes: { gap: spacing.sm, paddingHorizontal: spacing.xs }, includesTitle: { ...typography.headingMD, color: colors.textPrimary }, include: { ...typography.bodyMD, color: colors.textSecondary } });
