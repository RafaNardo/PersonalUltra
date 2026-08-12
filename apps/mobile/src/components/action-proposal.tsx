import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { colors, radius, spacing, typography } from '@/src/design/tokens';

type SafetyLevel = 'Green' | 'Yellow' | 'Red';

type ActionProposalProps = {
  content: string;
  proposalType?: string;
  reasonCode?: string;
  safetyLevel?: SafetyLevel;
  requiresConfirmation?: boolean;
  time?: string;
  actionId?: string;
  isPending?: boolean;
  isResolved?: boolean;
  error?: string | null;
  onConfirm?: () => void;
  onReject?: () => void;
};

const safetyCopy: Record<SafetyLevel, string> = {
  Green: 'Dentro das regras aprovadas',
  Yellow: 'Revisão necessária',
  Red: 'Não automatizar',
};

function displayReason(reasonCode?: string) {
  if (!reasonCode) return 'Avaliação da metodologia SVR';
  if (reasonCode === 'EXERCISE_SUBSTITUTION_REQUIRES_CONFIRMATION') return 'Alternativa aprovada pela metodologia';
  if (reasonCode === 'FOOD_SUBSTITUTION_REQUIRES_CONFIRMATION') return 'Equivalência nutricional aprovada';
  return reasonCode.replaceAll('_', ' ').toLocaleLowerCase('pt-BR');
}

export function ActionProposal({ content, proposalType = 'Alteração proposta', reasonCode, safetyLevel = 'Yellow', requiresConfirmation = true, time, actionId, isPending = false, isResolved = false, error, onConfirm, onReject }: ActionProposalProps) {
  const safetyLabel = safetyCopy[safetyLevel];
  return <View accessible accessibilityRole="text" accessibilityLabel={`Proposta: ${proposalType}. Motivo: ${displayReason(reasonCode)}. Segurança: ${safetyLabel}.${requiresConfirmation ? ' A confirmação é necessária.' : ''}`} style={[styles.card, safetyLevel === 'Red' && styles.redCard]}>
    <Text style={styles.eyebrow}>PROPOSTA DO COACH</Text>
    <Text style={styles.type}>{proposalType}</Text>
    <Text style={styles.content}>{content}</Text>
    <View style={styles.divider} />
    <View style={styles.detail}><Text style={styles.detailLabel}>MOTIVO</Text><Text style={styles.detailValue}>{displayReason(reasonCode)}</Text></View>
    <View style={styles.detail}><Text style={styles.detailLabel}>SEGURANÇA</Text><Text style={[styles.safety, safetyLevel === 'Green' && styles.green, safetyLevel === 'Red' && styles.red]}>{safetyLabel}</Text></View>
    {requiresConfirmation && <View style={styles.confirmation}><Text style={styles.confirmationText}>{isResolved ? 'PROPOSTA JÁ RESOLVIDA' : 'CONFIRMAÇÃO NECESSÁRIA · NENHUMA ALTERAÇÃO FOI APLICADA'}</Text></View>}
    {actionId && !isResolved && <View style={styles.actions}>
      <Pressable accessibilityRole="button" accessibilityLabel="Confirmar proposta do Coach" accessibilityHint="Aplica a alteração proposta ao treino" accessibilityState={{ disabled: isPending }} disabled={isPending} onPress={onConfirm} style={({ pressed }) => [styles.confirmButton, (isPending || pressed) && styles.buttonPressed]}>{isPending ? <ActivityIndicator color={colors.textPrimary} /> : <Text style={styles.confirmText}>CONFIRMAR</Text>}</Pressable>
      <Pressable accessibilityRole="button" accessibilityLabel="Recusar proposta do Coach" accessibilityHint="Descarta a proposta sem alterar o treino" accessibilityState={{ disabled: isPending }} disabled={isPending} onPress={onReject} style={({ pressed }) => [styles.rejectButton, (isPending || pressed) && styles.buttonPressed]}><Text style={styles.rejectText}>RECUSAR</Text></Pressable>
    </View>}
    {error && <Text accessibilityRole="alert" style={styles.error}>{error}</Text>}
    {time && <Text style={styles.time}>{time}</Text>}
  </View>;
}

const styles = StyleSheet.create({
  card: { flex: 1, gap: spacing.sm, padding: spacing.md, borderRadius: radius.md, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.warning }, redCard: { borderColor: colors.danger }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: .9 }, type: { ...typography.headingMD, color: colors.textPrimary }, content: { ...typography.bodyMD, color: colors.textPrimary, lineHeight: 21 }, divider: { height: 1, backgroundColor: colors.border, marginVertical: spacing.xxs }, detail: { flexDirection: 'row', justifyContent: 'space-between', gap: spacing.md }, detailLabel: { ...typography.caption, color: colors.textMuted, letterSpacing: .5 }, detailValue: { ...typography.caption, color: colors.textSecondary, flexShrink: 1, textAlign: 'right', textTransform: 'capitalize' }, safety: { ...typography.caption, color: colors.warning, textAlign: 'right' }, green: { color: colors.success }, red: { color: colors.danger }, confirmation: { padding: spacing.sm, borderRadius: radius.sm, backgroundColor: '#4D1520' }, confirmationText: { ...typography.caption, color: colors.textPrimary, lineHeight: 16 }, actions: { flexDirection: 'row', gap: spacing.sm }, confirmButton: { flex: 1, minHeight: 42, alignItems: 'center', justifyContent: 'center', borderRadius: radius.sm, backgroundColor: colors.primary }, confirmText: { ...typography.caption, color: colors.textPrimary }, rejectButton: { flex: 1, minHeight: 42, alignItems: 'center', justifyContent: 'center', borderRadius: radius.sm, borderWidth: 1, borderColor: colors.border }, rejectText: { ...typography.caption, color: colors.textPrimary }, buttonPressed: { opacity: .55 }, error: { ...typography.caption, color: colors.danger }, time: { ...typography.caption, color: colors.textMuted, alignSelf: 'flex-end', fontSize: 10 },
});
