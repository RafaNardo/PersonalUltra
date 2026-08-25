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
  return <Screen style={styles.page}><View style={styles.hero}><Text style={styles.eyebrow}>SEU ACOMPANHAMENTO</Text><Text style={styles.title}>Seu personal já recebeu seu ponto de partida.</Text><Text style={styles.copy}>{branding.data?.displayName ? `Acompanhamento com ${branding.data.displayName}.` : 'Seu protocolo aparece aqui conforme o personal libera cada parte.'}</Text></View>{message.data && <Card style={styles.message}><Text style={styles.messageEyebrow}>MENSAGEM DO SEU PERSONAL</Text><Text style={styles.messageText}>{message.data.message}</Text></Card>}<Button onPress={() => router.push('/student/training')}>Ver meus treinos</Button><Button variant="secondary" onPress={() => router.push('/student/nutrition')}>Ver alimentação</Button><Button variant="secondary" onPress={() => router.push('/student/progress')}>Acompanhar progresso</Button><Button variant="secondary" onPress={() => router.push('/student/coach')}>Perguntar ao Coach</Button><Button variant="ghost" onPress={() => { clear(); router.replace('/login'); }}>Sair</Button></Screen>;
}
const styles = StyleSheet.create({ page: { justifyContent: 'center', gap: spacing.xxl }, hero: { gap: spacing.sm }, eyebrow: { ...typography.caption, color: colors.signalGreen, letterSpacing: 1 }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyLG, color: colors.textSecondary, lineHeight: 24 }, message: { gap: spacing.sm, borderColor: colors.primary }, messageEyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, messageText: { ...typography.bodyLG, color: colors.textPrimary, lineHeight: 24 } });
