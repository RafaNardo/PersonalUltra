import type { PropsWithChildren } from 'react';
import { KeyboardAvoidingView, Platform, ScrollView, StyleSheet, Text, View, type StyleProp, type ViewStyle } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Pressable } from 'react-native';
import { colors, spacing, typography } from '@/src/design/tokens';

export function Screen({ children, scroll = true, style }: PropsWithChildren<{ scroll?: boolean; style?: StyleProp<ViewStyle> }>) {
  const content = <View style={[styles.content, style]}>{children}</View>;
  return <SafeAreaView style={styles.safe}><KeyboardAvoidingView style={styles.keyboard} behavior={Platform.select({ ios: 'padding', android: 'height' })}>{scroll ? <ScrollView contentContainerStyle={styles.scroll} keyboardDismissMode="on-drag" keyboardShouldPersistTaps="handled">{content}</ScrollView> : content}</KeyboardAvoidingView></SafeAreaView>;
}

export function TopBar({ eyebrow, title, action, onBack }: { eyebrow?: string; title: string; action?: React.ReactNode; onBack?: () => void }) {
  return <View style={styles.topBar}>{onBack && <Pressable accessibilityRole="button" accessibilityLabel="Voltar" onPress={onBack} hitSlop={12} style={styles.back}><Text style={styles.backText}>‹</Text></Pressable>}<View style={styles.titleGroup}>{eyebrow && <Text style={styles.eyebrow}>{eyebrow}</Text>}<Text style={styles.title}>{title}</Text></View>{action}</View>;
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.background }, keyboard: { flex: 1 }, scroll: { flexGrow: 1 }, content: { flex: 1, padding: spacing.lg, gap: spacing.lg },
  topBar: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', gap: spacing.md }, back: { width: 34, height: 38, justifyContent: 'center', alignItems: 'center', marginLeft: -spacing.xs }, backText: { fontSize: 38, lineHeight: 38, color: colors.textPrimary }, titleGroup: { flex: 1, gap: spacing.xxs }, eyebrow: { ...typography.caption, color: colors.primary, textTransform: 'uppercase', letterSpacing: 1 }, title: { ...typography.displayLG, color: colors.textPrimary },
});
