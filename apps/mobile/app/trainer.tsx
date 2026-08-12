import { router } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';
import { Button, Card } from '@/src/components/ui';
import { colors, spacing, typography } from '@/src/design/tokens';

export default function TrainerEntryScreen() {
  return <View style={styles.page}><Text style={styles.title}>Trainer</Text><Card><Text style={styles.copy}>A área do Trainer será construída nos próximos milestones.</Text><Button variant="secondary" onPress={() => router.replace('/demo-role-switch')}>Trocar contexto demo</Button></Card></View>;
}
const styles = StyleSheet.create({ page: { flex: 1, justifyContent: 'center', padding: spacing.xl, backgroundColor: colors.background, gap: spacing.lg }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, marginBottom: spacing.lg } });
