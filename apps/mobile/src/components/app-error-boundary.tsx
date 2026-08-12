import { Component, type ErrorInfo, type ReactNode } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { telemetry } from '@/src/platform/telemetry';
import { colors, radius, spacing, typography } from '@/src/design/tokens';

type Props = { children: ReactNode };
type State = { failed: boolean };

// Last-resort UI protection for render-time failures. It deliberately reports
// only a category and scope through the local telemetry facade.
export class AppErrorBoundary extends Component<Props, State> {
  state: State = { failed: false };

  static getDerivedStateFromError(): State { return { failed: true }; }

  componentDidCatch(error: Error, _info: ErrorInfo) {
    telemetry.error(error, { scope: 'render' });
  }

  private retry = () => this.setState({ failed: false });

  render() {
    if (this.state.failed) return <View accessibilityRole="alert" accessibilityLiveRegion="assertive" style={styles.container}>
      <Text style={styles.title}>Algo saiu do esperado</Text>
      <Text style={styles.message}>Não foi possível mostrar esta tela. Tente abrir novamente.</Text>
      <Pressable accessibilityRole="button" accessibilityLabel="Tentar abrir novamente" onPress={this.retry} style={({ pressed }) => [styles.button, pressed && styles.pressed]}><Text style={styles.buttonText}>TENTAR NOVAMENTE</Text></Pressable>
    </View>;
    return this.props.children;
  }
}

const styles = StyleSheet.create({
  container: { flex: 1, justifyContent: 'center', gap: spacing.md, padding: spacing.xl, backgroundColor: colors.background }, title: { ...typography.headingLG, color: colors.textPrimary }, message: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, button: { minHeight: 52, alignItems: 'center', justifyContent: 'center', borderRadius: radius.md, backgroundColor: colors.primary, marginTop: spacing.sm }, buttonText: { ...typography.caption, color: colors.textPrimary, letterSpacing: .6 }, pressed: { opacity: .78 },
});
