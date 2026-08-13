import { router } from 'expo-router';
import { Image, StyleSheet, Text, View } from 'react-native';
import { Button, Card } from '@/src/components/ui';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useDemoRoleStore, type DemoRole } from '@/src/state/demo-role-store';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

export default function DemoRoleSwitchScreen() {
  const chooseRole = useDemoRoleStore((state) => state.chooseRole);
  const clearStudent = useInviteSessionStore((state) => state.clear);
  const choose = (role: DemoRole) => { clearStudent(); chooseRole(role); router.replace(role === 'trainer' ? '/trainer' : '/login'); };
  return <View style={styles.page}><View style={styles.hero}><Image source={require('../assets/brand/personal-ultra-logo-horizontal.png')} resizeMode="contain" style={styles.logo} /><Text style={styles.eyebrow}>DEMONSTRAÇÃO</Text><Text style={styles.title}>Escolha como continuar.</Text><Text style={styles.copy}>Entre no contexto que você quer explorar nesta demonstração.</Text></View><Card style={styles.card}><Button onPress={() => choose('trainer')}>Entrar como Trainer</Button><Button variant="secondary" onPress={() => choose('student')}>Entrar como Student</Button></Card></View>;
}
const styles = StyleSheet.create({ page: { flex: 1, justifyContent: 'center', padding: spacing.xl, backgroundColor: colors.background, gap: spacing.xxl }, hero: { gap: spacing.md }, logo: { width: 230, height: 86, alignSelf: 'center', marginBottom: spacing.xl }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1.4 }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, maxWidth: 320 }, card: { gap: spacing.md } });
