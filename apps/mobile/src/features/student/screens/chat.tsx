import { Redirect, router } from 'expo-router';
import { useIsFocused } from '@react-navigation/native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Linking, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { useState } from 'react';
import { Button, Card, EmptyState, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentChatMessage } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

const date = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' });

export function StudentChatScreen() {
  const session = useInviteSessionStore((state) => state.session); const client = useQueryClient(); const [content, setContent] = useState(''); const isFocused = useIsFocused();
  const chat = useQuery({ queryKey: ['student', session?.studentId, 'chat'], queryFn: () => inviteApi.chat(session!.accessToken), enabled: Boolean(session) && isFocused, refetchInterval: isFocused ? 20_000 : false });
  const send = useMutation({ mutationFn: () => inviteApi.sendChatMessage(session!.accessToken, content.trim()), onSuccess: () => { setContent(''); void client.invalidateQueries({ queryKey: ['student', session?.studentId, 'chat'] }); } });
  if (!session) return <Redirect href="/login" />;
  if (chat.isLoading) return <LoadingView message="Abrindo seu chat…" />;
  if (chat.isError) return <ErrorView message={chat.error.message} onRetry={() => chat.refetch()} />;
  const openWhatsApp = () => { const phone = chat.data!.trainerPhone?.replace(/\D/g, ''); if (phone) void Linking.openURL(`https://wa.me/${phone}`); };
  return <Screen style={styles.page}><TopBar eyebrow="CHAT" title="Seu personal" onBack={() => router.back()} />
    <Text style={styles.copy}>Converse diretamente com seu personal. As mensagens são atualizadas enquanto esta tela estiver aberta.</Text>
    {chat.data!.messages.length ? <View style={styles.messages}>{chat.data!.messages.map((message) => <MessageBubble key={message.id} message={message} />)}</View> : <EmptyState status="CONVERSA ABERTA" title="Envie a primeira mensagem quando precisar." message="Seu personal verá sua mensagem no acompanhamento e poderá responder por aqui." />}
    <Card style={styles.composer}><TextInput value={content} onChangeText={setContent} multiline maxLength={1000} placeholder="Escreva sua mensagem" placeholderTextColor={colors.textMuted} accessibilityLabel="Mensagem para o personal" style={styles.input} /><Button loading={send.isPending} disabled={!content.trim()} onPress={() => send.mutate()}>Enviar mensagem</Button>{chat.data!.trainerPhone ? <Pressable accessibilityRole="button" accessibilityLabel="Abrir WhatsApp do personal" onPress={openWhatsApp}><Text style={styles.whatsApp}>Falar pelo WhatsApp →</Text></Pressable> : null}</Card>
  </Screen>;
}

function MessageBubble({ message }: { message: StudentChatMessage }) { const mine = message.sender === 'Student'; return <View style={[styles.bubble, mine ? styles.myBubble : styles.trainerBubble]}><Text style={styles.sender}>{mine ? 'VOCÊ' : 'SEU PERSONAL'}</Text><Text style={styles.message}>{message.content}</Text><Text style={styles.date}>{date.format(new Date(message.createdAt))}</Text></View>; }

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, messages: { gap: spacing.sm }, bubble: { maxWidth: '88%', gap: spacing.xxs, padding: spacing.md, borderRadius: radius.md }, myBubble: { alignSelf: 'flex-end', backgroundColor: '#4D1520', borderWidth: 1, borderColor: colors.primary }, trainerBubble: { alignSelf: 'flex-start', backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border }, sender: { ...typography.caption, color: colors.primary, letterSpacing: .7 }, message: { ...typography.bodyMD, color: colors.textPrimary, lineHeight: 21 }, date: { ...typography.caption, color: colors.textMuted }, composer: { gap: spacing.sm }, input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 96, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, padding: spacing.md, textAlignVertical: 'top' }, whatsApp: { ...typography.bodyMD, color: colors.success, textAlign: 'center', fontFamily: 'MontserratSemiBold' } });
