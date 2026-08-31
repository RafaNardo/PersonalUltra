import { StyleSheet, Text, View } from 'react-native';
import { colors, radius, spacing, typography } from '@/src/design/tokens';

export type ProgressChartPoint = { id: string; value: number; label: string; accessibilityLabel: string };

type ProgressBarChartProps = {
  points: ProgressChartPoint[];
  valueLabel: (value: number) => string;
  accent?: string;
};

/** A dependency-free mobile chart. It shows only real recorded values. */
export function ProgressBarChart({ points, valueLabel, accent = colors.primary }: ProgressBarChartProps) {
  const visible = points.slice(-7);
  const values = visible.map((item) => item.value);
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = max - min;

  return <View accessibilityRole="image" accessibilityLabel={`Gráfico com ${visible.length} registros`} style={styles.chart}>
    <View style={styles.bars}>
      {visible.map((point) => {
        const height = range === 0 ? 54 : 20 + ((point.value - min) / range) * 64;
        return <View key={point.id} accessible={false} style={styles.column}>
          <Text style={styles.value}>{valueLabel(point.value)}</Text>
          <View style={styles.track}><View style={[styles.bar, { height, backgroundColor: accent }]} /></View>
          <Text style={styles.label}>{point.label}</Text>
        </View>;
      })}
    </View>
    <Text style={styles.caption}>Exibe os últimos {visible.length} registros reais.</Text>
  </View>;
}

const styles = StyleSheet.create({
  chart: { gap: spacing.sm },
  bars: { minHeight: 116, flexDirection: 'row', alignItems: 'flex-end', gap: spacing.xs },
  column: { flex: 1, minWidth: 30, alignItems: 'center', justifyContent: 'flex-end', gap: spacing.xxs },
  value: { ...typography.caption, color: colors.titaniumLight, textAlign: 'center' },
  track: { height: 84, width: '100%', maxWidth: 34, justifyContent: 'flex-end', overflow: 'hidden', borderRadius: radius.sm, backgroundColor: colors.surfaceElevated },
  bar: { width: '100%', minHeight: 8, borderRadius: radius.sm },
  label: { ...typography.caption, color: colors.textMuted, textAlign: 'center' },
  caption: { ...typography.caption, color: colors.textMuted },
});
