import { Redirect, router } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from './api';
import { useInviteSessionStore } from './session-store';

export function StudentWaitingHome() {
  const session = useInviteSessionStore((s) => s.session); const clear = useInviteSessionStore((s) => s.clear);
  const message = useQuery({ queryKey: ['student', session?.studentId, 'trainer-message'], queryFn: () => inviteApi.activeTrainerMessage(session!.accessToken), enabled: Boolean(session) });
  const branding = useQuery({ queryKey: ['student', session?.studentId, 'branding'], queryFn: () => inviteApi.branding(session!.accessToken), enabled: Boolean(session) });
  if (!session) return <Redirect href="/login" />;
  if (message.isLoading || branding.isLoading) return <LoadingView message="Abrindo seu acompanhamento…" />;
  if (message.isError) return <ErrorView message={message.error.message} onRetry={() => message.refetch()} />;
  return <Screen style={styles.page}><View style={styles.hero}><View style={styles.mark}><Text style={styles.markText}>✦</Text></View><Text style={styles.eyebrow}>BEM-VINDO AO SEU ACOMPANHAMENTO</Text><Text style={styles.title}>Olá, {session.firstName}.</Text><Text style={styles.copy}>Você deu um passo importante ao compartilhar seu ponto de partida. A partir daqui, seu acompanhamento vai ganhar forma no seu ritmo.</Text>{branding.data?.displayName ? <Text style={styles.trainer}>Com {branding.data.displayName}</Text> : null}</View>{message.data && <Card style={styles.message}><Text style={styles.messageEyebrow}>UMA MENSAGEM PARA VOCÊ</Text><Text style={styles.messageText}>{message.data.message}</Text></Card>}<Card style={styles.nextStep}><Text style={styles.nextStepTitle}>Tudo começa por aqui</Text><Text style={styles.nextStepCopy}>Quando seus conteúdos estiverem disponíveis, você os encontrará no início. Por enquanto, aproveite este momento: seu próximo passo já está dado.</Text></Card><Button accessibilityLabel="Ir para o início" accessibilityHint="Abre a página inicial do seu acompanhamento" onPress={() => router.replace('/student')}>Ir para o início</Button><Button variant="ghost" onPress={() => { clear(); router.replace('/login'); }}>Sair</Button></Screen>;
}
const styles = StyleSheet.create({ page: { justifyContent: 'center', gap: spacing.xxl }, hero: { gap: spacing.sm }, mark: { width: 64, height: 64, borderRadius: 32, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.primary, marginBottom: spacing.sm }, markText: { ...typography.headingLG, color: colors.background, fontSize: 30 }, eyebrow: { ...typography.caption, color: colors.signalGreen, letterSpacing: 1 }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyLG, color: colors.textSecondary, lineHeight: 24 }, trainer: { ...typography.bodyMD, color: colors.titaniumLight, marginTop: spacing.xs }, message: { gap: spacing.sm, borderColor: colors.primary }, messageEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, messageText: { ...typography.bodyLG, color: colors.textPrimary, lineHeight: 24 }, nextStep: { gap: spacing.xs, borderColor: colors.border }, nextStepTitle: { ...typography.headingMD, color: colors.textPrimary }, nextStepCopy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 } });
