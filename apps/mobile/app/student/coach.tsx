import { useState } from 'react';
import { router } from 'expo-router';
import { ActivityIndicator, Image, KeyboardAvoidingView, Platform, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import type { CoachMessage } from '@/src/api/types';
import { useCoachConversation, useSendCoachMessage } from '@/src/api/hooks';
import { Card, ErrorView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { feedback } from '@/src/platform/feedback';
import { telemetry } from '@/src/platform/telemetry';

const avatar = require('../../assets/avatar.png');
const maxMessageLength = 2000;
const actions = [
  { icon: '◔', label: 'Estou\ncansado', message: 'Estou cansado hoje' },
  { icon: '↺', label: 'Ver meu\ntreino', message: 'Qual treino está recomendado para hoje?' },
  { icon: '✈', label: 'Vou\nviajar', message: 'Vou viajar' },
  { icon: '⌁', label: 'Ver minha\nalimentação', message: 'Quais são as refeições do meu plano?' },
];

type CoachMetadata = { reasonCode?: string; messageType?: CoachMessage['kind']; requiresUserInput?: boolean; requiresConfirmation?: boolean; proposalType?: string; safetyLevel?: 'Green' | 'Yellow' | 'Red'; actionId?: string };

function metadataFor(message: CoachMessage): CoachMetadata | null {
  if (!message.metadataJson) return null;
  try { return JSON.parse(message.metadataJson) as CoachMetadata; } catch { return null; }
}

function AssistantMessage({ message }: { message: CoachMessage }) {
  const metadata = metadataFor(message);
  const label = message.kind === 'Choice' ? 'Pergunta do Coach' : message.kind === 'ProgressInsight' ? 'Insight de progresso' : 'Mensagem do Coach';
  const typeLabel = message.kind === 'Choice' ? 'SUA RESPOSTA' : message.kind === 'ProgressInsight' ? 'INSIGHT DE PROGRESSO' : null;
  const typeCopy = message.kind === 'Choice' && metadata?.requiresUserInput ? 'Responda para o Coach continuar o acompanhamento.' : null;

  return <View accessible accessibilityRole="text" accessibilityLabel={`${label}: ${message.content}`} style={styles.messageRow}>
    <Image source={avatar} style={styles.messageAvatar} />
    <View style={[styles.message, styles.assistant, message.kind === 'Choice' && styles.choiceMessage, message.kind === 'ProgressInsight' && styles.insightMessage]}>
      {typeLabel && <Text style={styles.typeLabel}>{typeLabel}</Text>}
      <Text style={styles.messageText}>{message.content}</Text>
      {typeCopy && <Text style={styles.note}>{typeCopy}</Text>}
      <MessageTime createdAt={message.createdAt} />
    </View>
  </View>;
}

function UserMessage({ message }: { message: CoachMessage }) {
  return <View accessible accessibilityRole="text" accessibilityLabel={`Sua mensagem: ${message.content}`} style={[styles.messageRow, styles.userRow]}>
    <View style={[styles.message, styles.user]}><Text style={styles.messageText}>{message.content}</Text><MessageTime createdAt={message.createdAt} /></View>
  </View>;
}

function MessageTime({ createdAt }: { createdAt: string }) {
  return <Text style={styles.time}>{formatTime(createdAt)}</Text>;
}

function formatTime(createdAt: string) {
  const time = new Date(createdAt);
  return Number.isNaN(time.getTime()) ? '' : time.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}

export default function CoachScreen() {
  const conversation = useCoachConversation();
  const send = useSendCoachMessage();
  const [content, setContent] = useState('');
  const [sendError, setSendError] = useState<string | null>(null);

  async function submit(message = content, restoreOnFailure = message === content) {
    const text = message.trim();
    if (!text || send.isPending) return;
    setSendError(null);
    if (restoreOnFailure) setContent('');
    try {
      await send.mutateAsync(text);
      feedback.selection();
      telemetry.event('coach_message_sent');
    } catch (error) {
      if (restoreOnFailure) setContent(text);
      setSendError(error instanceof Error ? error.message : 'Não foi possível enviar sua mensagem. Tente novamente.');
    }
  }

  if (conversation.isLoading) return <Screen><TopBar eyebrow="Hoje · Personal Ultra" title="Coach" action={<Image source={avatar} style={styles.topAvatar} />} /><View accessibilityLiveRegion="polite" style={styles.loading}><ActivityIndicator color={colors.primary} size="large" /><Text style={styles.loadingText}>Preparando seu acompanhamento…</Text></View></Screen>;
  if (conversation.error || !conversation.data) return <Screen><TopBar eyebrow="Hoje · Personal Ultra" title="Coach" action={<Image source={avatar} style={styles.topAvatar} />} /><ErrorView message="Não foi possível abrir o Coach." onRetry={() => void conversation.refetch()} /></Screen>;

  const messages = conversation.data.messages;
  return <KeyboardAvoidingView style={styles.flex} behavior={Platform.OS === 'ios' ? 'padding' : undefined}><Screen scroll><TopBar eyebrow="Hoje · Personal Ultra" title="Coach" action={<Image source={avatar} style={styles.topAvatar} />} />
    <View style={styles.coachIntro}><Image source={avatar} style={styles.introAvatar} /><View><Text style={styles.introName}>SEU COACH</Text><Text style={styles.introCopy}>Orientações baseadas no que já foi prescrito para você.</Text></View></View>
    {messages.length === 0 ? <Card style={styles.empty}><Text style={styles.emptyLabel}>COACH</Text><Text style={styles.emptyTitle}>Como posso apoiar seu dia?</Text><Text style={styles.emptyCopy}>Envie uma mensagem ou escolha um atalho. O Coach explica seu plano, mas não faz alterações.</Text></Card> : <View accessibilityLiveRegion="polite" style={styles.messages}>{messages.map((message) => message.role === 'User' ? <UserMessage key={message.id} message={message} /> : <AssistantMessage key={message.id} message={message} />)}</View>}
    <View style={styles.quickHeader}><Text style={styles.quickTitle}>COMO POSSO AJUDAR?</Text><Pressable accessibilityRole="button" accessibilityLabel="Registrar dor" accessibilityHint="Abre o registro de dor" onPress={() => router.push('/student/pain')}><Text style={styles.painLink}>Registrar dor</Text></Pressable></View>
    <View style={styles.quick}>{actions.map((action) => <Pressable key={action.label} accessibilityRole="button" accessibilityLabel={action.label.replace('\n', ' ')} accessibilityHint="Envia esta mensagem ao Coach" accessibilityState={{ disabled: send.isPending }} disabled={send.isPending} onPress={() => void submit(action.message, false)} style={({ pressed }) => [styles.quickAction, pressed && !send.isPending && styles.pressed, send.isPending && styles.disabled]}><Text style={styles.quickIcon}>{action.icon}</Text><Text style={styles.quickText}>{action.label}</Text></Pressable>)}</View>
    {sendError && <View accessibilityLiveRegion="assertive" style={styles.sendError}><Text accessibilityRole="alert" style={styles.sendErrorText}>{sendError}</Text></View>}
    <View style={styles.composer}><TextInput value={content} onChangeText={setContent} onSubmitEditing={() => void submit()} editable={!send.isPending} maxLength={maxMessageLength} accessibilityLabel="Mensagem para o Coach" accessibilityHint={`Digite até ${maxMessageLength} caracteres e envie`} placeholder="Digite sua mensagem..." placeholderTextColor={colors.textMuted} returnKeyType="send" style={styles.input} /><Pressable accessibilityRole="button" accessibilityLabel="Enviar mensagem" accessibilityState={{ disabled: send.isPending || !content.trim() }} disabled={send.isPending || !content.trim()} onPress={() => void submit()} style={({ pressed }) => [styles.send, (send.isPending || !content.trim()) && styles.sendDisabled, pressed && !send.isPending && content.trim() && styles.pressed]}>{send.isPending ? <ActivityIndicator color={colors.textPrimary} /> : <Text style={styles.sendText}>↑</Text>}</Pressable></View>
    <Text style={styles.counter} accessibilityLabel={`${content.length} de ${maxMessageLength} caracteres`}>{content.length}/{maxMessageLength}</Text>
  </Screen></KeyboardAvoidingView>;
}

const styles = StyleSheet.create({
  flex: { flex: 1 }, topAvatar: { width: 34, height: 34, borderRadius: 17, marginTop: spacing.xs }, loading: { flex: 1, justifyContent: 'center', alignItems: 'center', gap: spacing.md, minHeight: 320 }, loadingText: { ...typography.bodyMD, color: colors.textSecondary }, coachIntro: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, padding: spacing.md, borderRadius: radius.md, backgroundColor: colors.surface }, introAvatar: { width: 50, height: 50, borderRadius: 25 }, introName: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, introCopy: { ...typography.bodyMD, color: colors.textSecondary, marginTop: spacing.xxs, maxWidth: 240 },
  empty: { gap: spacing.sm, borderColor: colors.primary }, emptyLabel: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, emptyTitle: { ...typography.headingLG, color: colors.textPrimary }, emptyCopy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, messages: { gap: spacing.md }, messageRow: { flexDirection: 'row', alignItems: 'flex-end', gap: spacing.xs }, userRow: { justifyContent: 'flex-end' }, messageAvatar: { width: 28, height: 28, borderRadius: 14 }, message: { maxWidth: '82%', padding: spacing.md, borderRadius: radius.md, gap: spacing.xs }, user: { backgroundColor: colors.primary, borderBottomRightRadius: spacing.xxs }, assistant: { backgroundColor: colors.surface, borderBottomLeftRadius: spacing.xxs }, choiceMessage: { borderWidth: 1, borderColor: colors.warning }, insightMessage: { borderWidth: 1, borderColor: colors.success }, typeLabel: { ...typography.caption, color: colors.warning, letterSpacing: .8 }, messageText: { ...typography.bodyMD, color: colors.textPrimary, lineHeight: 21 }, note: { ...typography.caption, color: colors.textSecondary, marginTop: spacing.xs }, time: { ...typography.caption, color: colors.textMuted, alignSelf: 'flex-end', fontSize: 10 },
  quickHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginTop: spacing.xs }, quickTitle: { ...typography.caption, color: colors.textMuted, letterSpacing: 1 }, painLink: { ...typography.caption, color: colors.primary }, quick: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm }, quickAction: { width: '48%', minHeight: 88, justifyContent: 'space-between', borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, padding: spacing.sm, backgroundColor: colors.surface }, quickIcon: { color: colors.primary, fontSize: 22 }, quickText: { ...typography.caption, color: colors.textPrimary, lineHeight: 17 }, disabled: { opacity: .5 }, pressed: { opacity: .8 }, sendError: { borderRadius: radius.sm, borderWidth: 1, borderColor: colors.danger, backgroundColor: '#311114', padding: spacing.sm }, sendErrorText: { ...typography.bodyMD, color: colors.danger },
  composer: { flexDirection: 'row', gap: spacing.sm, alignItems: 'center', marginTop: spacing.xs, padding: spacing.xs, backgroundColor: colors.surface, borderRadius: radius.md, borderWidth: 1, borderColor: colors.border }, input: { flex: 1, minHeight: 42, color: colors.textPrimary, paddingHorizontal: spacing.sm, ...typography.bodyMD }, send: { width: 42, height: 42, borderRadius: 21, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.primary }, sendDisabled: { opacity: .45 }, sendText: { color: colors.textPrimary, fontSize: 24, lineHeight: 28 }, counter: { ...typography.caption, color: colors.textMuted, textAlign: 'right', marginTop: -spacing.md },
});
