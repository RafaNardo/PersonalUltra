import Ionicons from '@expo/vector-icons/Ionicons';
import type { PropsWithChildren, ReactNode } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, TextInput, View, type AccessibilityRole, type StyleProp, type ViewStyle } from 'react-native';
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

type ListItemProps = {
  title: string;
  metadata?: string;
  description?: string;
  leading?: ReactNode;
  badge?: ReactNode;
  actionLabel?: string;
  onPress?: () => void;
  disabled?: boolean;
  accessibilityLabel?: string;
  accessibilityHint?: string;
};

export function ListItem({ title, metadata, description, leading, badge, actionLabel = 'Ver detalhes', onPress, disabled = false, accessibilityLabel, accessibilityHint }: ListItemProps) {
  const content = <>
    {leading}
    <View style={styles.listItemIdentity}>
      <Text numberOfLines={2} style={styles.listItemTitle}>{title}</Text>
      {metadata ? <Text numberOfLines={1} style={styles.listItemMetadata}>{metadata}</Text> : null}
      {description ? <Text numberOfLines={2} style={styles.listItemDescription}>{description}</Text> : null}
      {badge || onPress ? <View style={styles.listItemFooter}>{badge}{onPress ? <View style={styles.listItemAction}><Text style={styles.listItemActionText}>{actionLabel}</Text><Ionicons name="chevron-forward" size={18} color={colors.primary} /></View> : null}</View> : null}
    </View>
  </>;

  if (!onPress) return <View style={styles.listItem}>{content}</View>;
  return <Pressable disabled={disabled} accessibilityRole="button" accessibilityState={{ disabled }} accessibilityLabel={accessibilityLabel ?? title} accessibilityHint={accessibilityHint} onPress={onPress} style={({ pressed }) => [styles.listItem, disabled && styles.disabled, pressed && !disabled && styles.listItemPressed]}>{content}</Pressable>;
}

export function SearchField({ value, onChangeText, placeholder, accessibilityLabel }: { value: string; onChangeText: (value: string) => void; placeholder: string; accessibilityLabel: string }) {
  return <View style={styles.searchField}><Ionicons name="search-outline" size={21} color={colors.textMuted} /><TextInput value={value} onChangeText={onChangeText} autoCapitalize="none" autoCorrect={false} placeholder={placeholder} placeholderTextColor={colors.textMuted} accessibilityLabel={accessibilityLabel} style={styles.searchInput} /></View>;
}

export function Tag({ children, tone = 'neutral', style }: PropsWithChildren<{ tone?: 'neutral' | 'success' | 'primary'; style?: StyleProp<ViewStyle> }>) {
  return <View style={[styles.tag, tone === 'success' && styles.tagSuccess, tone === 'primary' && styles.tagPrimary, style]}><Text style={[styles.tagText, tone === 'success' && styles.tagSuccessText]}>{children}</Text></View>;
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

type EmptyStateProps = {
  title: string;
  message: string;
  status?: string;
  symbol?: string;
  items?: string[];
  footer?: string;
  actionLabel?: string;
  onAction?: () => void;
  variant?: 'page' | 'section' | 'inline';
};

export function EmptyState({ title, message, status = 'PRÓXIMO PASSO', symbol = '✦', items, footer, actionLabel, onAction, variant = 'section' }: EmptyStateProps) {
  const compact = variant === 'inline';
  return <View accessibilityLiveRegion="polite" style={[styles.empty, variant !== 'inline' && styles.emptyCentered, variant === 'page' && styles.emptyPage, compact && styles.emptyInline]}>
    {!compact ? <View style={[styles.emptyMark, variant === 'page' && styles.emptyMarkPage]}><Text style={[styles.emptyMarkText, variant === 'page' && styles.emptyMarkTextPage]}>{symbol}</Text></View> : null}
    <Text style={styles.emptyStatus}>{status}</Text>
    <Text style={[styles.emptyTitle, variant === 'page' && styles.emptyTitlePage]}>{title}</Text>
    <Text style={[styles.emptyMessage, variant === 'page' && styles.emptyMessagePage]}>{message}</Text>
    {items?.length ? <View style={styles.emptyItems}>{items.map((item, index) => <View key={`${index}-${item}`} style={styles.emptyItem}><View style={styles.emptyItemNumber}><Text style={styles.emptyItemNumberText}>{String(index + 1).padStart(2, '0')}</Text></View><Text style={styles.emptyItemText}>{item}</Text></View>)}</View> : null}
    {footer ? <Text style={styles.emptyFooter}>{footer}</Text> : null}
    {actionLabel && onAction ? <Button variant="secondary" style={styles.emptyAction} onPress={onAction}>{actionLabel}</Button> : null}
  </View>;
}

const styles = StyleSheet.create({
  button: { minHeight: 54, alignItems: 'center', justifyContent: 'center', borderRadius: radius.md, paddingHorizontal: spacing.xl },
  primary: { backgroundColor: colors.primary }, secondary: { backgroundColor: colors.surfaceElevated, borderWidth: 1, borderColor: colors.border }, ghost: { backgroundColor: 'transparent' },
  buttonText: { ...typography.bodyLG, color: colors.textPrimary, fontWeight: '700' }, buttonTextSecondary: { color: colors.textPrimary },
  disabled: { opacity: 0.45 }, pressed: { transform: [{ scale: 0.98 }] },
  card: { backgroundColor: colors.surface, borderRadius: radius.lg, padding: spacing.lg, borderWidth: 1, borderColor: colors.border },
  listItem: { minHeight: 76, flexDirection: 'row', alignItems: 'center', gap: spacing.sm, padding: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, backgroundColor: colors.surface },
  listItemPressed: { opacity: .76, transform: [{ scale: .99 }] },
  listItemIdentity: { flex: 1, gap: spacing.xxs },
  listItemTitle: { ...typography.bodyLG, color: colors.textPrimary, fontFamily: 'MontserratSemiBold' },
  listItemMetadata: { ...typography.caption, color: colors.titanium },
  listItemDescription: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
  listItemFooter: { flexDirection: 'row', alignItems: 'center', flexWrap: 'wrap', columnGap: spacing.sm, rowGap: spacing.xs, marginTop: spacing.xs },
  listItemAction: { flexDirection: 'row', alignItems: 'center', gap: spacing.xxs, marginLeft: 'auto' },
  listItemActionText: { ...typography.caption, color: colors.primary },
  searchField: { minHeight: 50, flexDirection: 'row', alignItems: 'center', gap: spacing.sm, paddingHorizontal: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, backgroundColor: colors.surface },
  searchInput: { ...typography.bodyMD, color: colors.textPrimary, flex: 1, minHeight: 48 },
  tag: { alignSelf: 'flex-start', paddingHorizontal: spacing.sm, paddingVertical: spacing.xs, borderRadius: radius.pill, backgroundColor: colors.surfaceElevated },
  tagSuccess: { backgroundColor: '#123D2B' }, tagPrimary: { backgroundColor: '#4D1520' }, tagText: { ...typography.caption, color: colors.textSecondary }, tagSuccessText: { color: colors.success },
  progressTrack: { height: 6, overflow: 'hidden', borderRadius: radius.pill, backgroundColor: colors.surfaceElevated }, progressValue: { height: '100%', borderRadius: radius.pill, backgroundColor: colors.primary },
  loading: { flex: 1, justifyContent: 'center', alignItems: 'center', gap: spacing.md, padding: spacing.xl, backgroundColor: colors.background }, loadingText: { ...typography.bodyMD, color: colors.textSecondary },
  error: { flex: 1, justifyContent: 'center', gap: spacing.md, padding: spacing.xl, backgroundColor: colors.background }, errorTitle: { ...typography.headingLG, color: colors.textPrimary }, errorMessage: { ...typography.bodyMD, color: colors.textSecondary },
  empty: { gap: spacing.sm, padding: spacing.lg, borderRadius: radius.lg, backgroundColor: colors.surface, borderWidth: 1, borderColor: colors.border },
  emptyCentered: { alignItems: 'center' },
  emptyPage: { gap: spacing.md, paddingHorizontal: spacing.xl, paddingVertical: spacing.xxl },
  emptyInline: { gap: spacing.xs, padding: spacing.md, backgroundColor: colors.surfaceElevated },
  emptyMark: { width: 58, height: 58, borderRadius: 29, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.primary },
  emptyMarkPage: { width: 92, height: 92, borderRadius: 46 },
  emptyMarkText: { ...typography.headingLG, color: colors.background },
  emptyMarkTextPage: { ...typography.displayLG },
  emptyStatus: { ...typography.caption, color: colors.primary, letterSpacing: 1, textAlign: 'center' },
  emptyTitle: { ...typography.headingMD, color: colors.textPrimary, textAlign: 'center' },
  emptyTitlePage: { ...typography.displayLG },
  emptyMessage: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21, textAlign: 'center' },
  emptyMessagePage: { ...typography.bodyLG, lineHeight: 25 },
  emptyItems: { alignSelf: 'stretch', gap: spacing.xs, marginTop: spacing.sm },
  emptyItem: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, paddingVertical: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border },
  emptyItemNumber: { width: 38, height: 38, borderRadius: 19, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.surfaceElevated },
  emptyItemNumberText: { ...typography.caption, color: colors.primary },
  emptyItemText: { ...typography.bodyMD, color: colors.titaniumLight, lineHeight: 21, flex: 1 },
  emptyFooter: { ...typography.bodyMD, color: colors.textMuted, lineHeight: 21, textAlign: 'center', marginTop: spacing.sm },
  emptyAction: { alignSelf: 'stretch', width: '100%', marginTop: spacing.sm },
});
