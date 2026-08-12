import type { PropsWithChildren } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View, type AccessibilityRole, type StyleProp, type ViewStyle } from 'react-native';
import { colors, radius, spacing, typography } from '@/src/design/tokens';

type ButtonProps = PropsWithChildren<{ onPress?: () => void; disabled?: boolean; loading?: boolean; variant?: 'primary' | 'secondary' | 'ghost'; style?: StyleProp<ViewStyle>; accessibilityLabel?: string; accessibilityHint?: string; accessibilityRole?: AccessibilityRole }>;

export function Button({ children, onPress, disabled, loading, variant = 'primary', style, accessibilityLabel, accessibilityHint, accessibilityRole }: ButtonProps) {
  return <Pressable disabled={disabled || loading} onPress={onPress} accessibilityRole={accessibilityRole ?? 'button'} accessibilityLabel={accessibilityLabel} accessibilityHint={accessibilityHint} style={({ pressed }) => [styles.button, styles[variant], (disabled || loading) && styles.disabled, pressed && !disabled && styles.pressed, style]}>
    {loading ? <ActivityIndicator color={variant === 'primary' ? colors.textPrimary : colors.primary} /> : <Text style={[styles.buttonText, variant !== 'primary' && styles.buttonTextSecondary]}>{children}</Text>}
  </Pressable>;
}

export function Card({ children, style }: PropsWithChildren<{ style?: StyleProp<ViewStyle> }>) {
  return <View style={[styles.card, style]}>{children}</View>;
}

export function Tag({ children, tone = 'neutral' }: PropsWithChildren<{ tone?: 'neutral' | 'success' | 'primary' }>) {
  return <View style={[styles.tag, tone === 'success' && styles.tagSuccess, tone === 'primary' && styles.tagPrimary]}><Text style={[styles.tagText, tone === 'success' && styles.tagSuccessText]}>{children}</Text></View>;
}

export function ProgressBar({ value }: { value: number }) {
  return <View style={styles.progressTrack}><View style={[styles.progressValue, { width: `${Math.max(0, Math.min(value, 1)) * 100}%` }]} /></View>;
}

export function LoadingView({ message = 'Carregando…' }: { message?: string }) {
  return <View accessibilityRole="progressbar" accessibilityLabel={message} accessibilityLiveRegion="polite" style={styles.loading}><ActivityIndicator color={colors.primary} size="large" /><Text style={styles.loadingText}>{message}</Text></View>;
}

export function ErrorView({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return <View accessibilityRole="alert" accessibilityLiveRegion="assertive" style={styles.error}><Text style={styles.errorTitle}>Não foi possível carregar</Text><Text style={styles.errorMessage}>{message}</Text>{onRetry && <Button variant="secondary" onPress={onRetry}>Tentar novamente</Button>}</View>;
}

export function EmptyState({ title, message, actionLabel, onAction }: { title: string; message: string; actionLabel?: string; onAction?: () => void }) {
  return <View accessibilityRole="text" accessibilityLabel={`${title}. ${message}`} style={styles.empty}><Text style={styles.emptyTitle}>{title}</Text><Text style={styles.emptyMessage}>{message}</Text>{actionLabel && onAction && <Button variant="secondary" onPress={onAction}>{actionLabel}</Button>}</View>;
}

const styles = StyleSheet.create({
  button: { minHeight: 54, alignItems: 'center', justifyContent: 'center', borderRadius: radius.md, paddingHorizontal: spacing.xl },
  primary: { backgroundColor: colors.primary }, secondary: { backgroundColor: colors.surfaceElevated, borderWidth: 1, borderColor: colors.border }, ghost: { backgroundColor: 'transparent' },
  buttonText: { ...typography.bodyLG, color: colors.textPrimary, fontWeight: '700' }, buttonTextSecondary: { color: colors.textPrimary },
  disabled: { opacity: 0.45 }, pressed: { transform: [{ scale: 0.98 }] },
  card: { backgroundColor: colors.surface, borderRadius: radius.lg, padding: spacing.lg, borderWidth: 1, borderColor: colors.border },
  tag: { alignSelf: 'flex-start', paddingHorizontal: spacing.sm, paddingVertical: spacing.xs, borderRadius: radius.pill, backgroundColor: colors.surfaceElevated },
  tagSuccess: { backgroundColor: '#123D2B' }, tagPrimary: { backgroundColor: '#4D1520' }, tagText: { ...typography.caption, color: colors.textSecondary }, tagSuccessText: { color: colors.success },
  progressTrack: { height: 6, overflow: 'hidden', borderRadius: radius.pill, backgroundColor: colors.surfaceElevated }, progressValue: { height: '100%', borderRadius: radius.pill, backgroundColor: colors.primary },
  loading: { flex: 1, justifyContent: 'center', alignItems: 'center', gap: spacing.md, padding: spacing.xl, backgroundColor: colors.background }, loadingText: { ...typography.bodyMD, color: colors.textSecondary },
  error: { flex: 1, justifyContent: 'center', gap: spacing.md, padding: spacing.xl, backgroundColor: colors.background }, errorTitle: { ...typography.headingLG, color: colors.textPrimary }, errorMessage: { ...typography.bodyMD, color: colors.textSecondary },
  empty: { gap: spacing.sm, padding: spacing.lg, borderRadius: radius.lg, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border }, emptyTitle: { ...typography.headingMD, color: colors.textPrimary }, emptyMessage: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
});
