import { router } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';
import { Button, Card } from '@/src/components/ui';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useDemoRoleStore, type DemoRole } from '@/src/state/demo-role-store';

export default function DemoRoleSwitchScreen() {
  const chooseRole = useDemoRoleStore((state) => state.chooseRole);
  const choose = (role: DemoRole) => { chooseRole(role); router.replace(role === 'trainer' ? '/trainer' : '/student'); };
  return <View style={styles.page}><Text style={styles.title}>Personal Ultra</Text><Text style={styles.copy}>Escolha o contexto da demonstração.</Text><Card style={styles.card}><Button onPress={() => choose('trainer')}>Entrar como Trainer</Button><Button variant="secondary" onPress={() => choose('student')}>Entrar como Student</Button></Card></View>;
}
const styles = StyleSheet.create({ page: { flex: 1, justifyContent: 'center', padding: spacing.xl, backgroundColor: colors.background, gap: spacing.md }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary }, card: { gap: spacing.md } });
