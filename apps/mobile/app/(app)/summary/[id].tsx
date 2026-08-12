import { router, useLocalSearchParams } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';
import { Button, Card, Tag } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainingStore } from '@/src/state/training-store';

export default function WorkoutSummaryScreen() {
  const { completedSets } = useLocalSearchParams<{ id: string; completedSets: string }>();
  const setActiveSession = useTrainingStore((state) => state.setActiveSession);
  const finish = () => { setActiveSession(undefined); router.replace('/(app)/home'); };
  return <Screen style={styles.screen}><View style={styles.hero}><Tag tone="success">TREINO CONCLUÍDO</Tag><Text style={styles.title}>Boa sessão.</Text><Text style={styles.copy}>Cada registro deixa a próxima decisão do método mais precisa.</Text></View><Card style={styles.metric}><Text style={styles.metricLabel}>SÉRIES REGISTRADAS HOJE</Text><Text style={styles.metricValue}>{completedSets}</Text><View style={styles.metricRule} /><Text style={styles.metricHint}>Seu plano foi atualizado com este desempenho.</Text></Card><View style={styles.bottom}><Text style={styles.note}>Consistência é o que transforma uma sessão em resultado.</Text><Button onPress={finish}>Voltar para Home</Button></View></Screen>;
}

const styles = StyleSheet.create({ screen: { justifyContent: 'space-between', paddingVertical: spacing.xxl }, hero: { gap: spacing.md }, title: { ...typography.displayXL, color: colors.textPrimary }, copy: { ...typography.bodyLG, color: colors.textSecondary, maxWidth: 300 }, metric: { alignItems: 'center', gap: spacing.sm, paddingVertical: spacing.xxxl, borderColor: '#245E42' }, metricValue: { ...typography.metricXL, color: colors.success, fontSize: 64, lineHeight: 70 }, metricLabel: { ...typography.caption, color: colors.textMuted, letterSpacing: 1 }, metricRule: { width: 52, height: 3, backgroundColor: colors.success, borderRadius: 4 }, metricHint: { ...typography.bodyMD, color: colors.textSecondary, textAlign: 'center' }, bottom: { gap: spacing.md }, note: { ...typography.bodyMD, color: colors.textMuted, textAlign: 'center' },
});
