import { useState } from 'react';
import { Alert, Image, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { useAddWeight, useProgress, useResetCurrentMemberDemo, useWeights } from '@/src/api/hooks';
import type { WeightEntry } from '@/src/api/types';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { feedback } from '@/src/platform/feedback';
import { telemetry } from '@/src/platform/telemetry';
import { router } from 'expo-router';
import { useAuthStore } from '@/src/state/auth-store';

const chartHeight = 156;
const progressPhotos = [
  { label: 'Avaliação 01', source: require('../../assets/progress-model/day-01.png') },
  { label: 'Avaliação 02', source: require('../../assets/progress-model/day-08.png') },
  { label: 'Avaliação 03', source: require('../../assets/progress-model/day-15.png') },
  { label: 'Avaliação 04', source: require('../../assets/progress-model/day-22.png') },
  { label: 'Avaliação 05', source: require('../../assets/progress-model/day-28.png') },
];

export default function ProgressScreen() {
  const progress = useProgress();
  const weights = useWeights();
  const addWeight = useAddWeight();
  const resetCurrentMemberDemo = useResetCurrentMemberDemo();
  const signOut = useAuthStore((state) => state.signOut);
  const [value, setValue] = useState('');

  if (progress.isLoading || weights.isLoading) return <LoadingView />;
  if (progress.error || weights.error || !progress.data || !weights.data) return <ErrorView message="Não foi possível carregar seu progresso." onRetry={() => { void progress.refetch(); void weights.refetch(); }} />;

  const history = weights.data.slice().sort((left, right) => new Date(left.recordedAt).getTime() - new Date(right.recordedAt).getTime());
  const change = progress.data.weightChangeKg;
  const save = async () => {
    const parsed = Number(value.replace(',', '.'));
    if (!parsed) return Alert.alert('Informe seu peso em kg.');
    try {
      await addWeight.mutateAsync(parsed);
      setValue('');
      feedback.success();
      telemetry.event('weight_logged');
    } catch {
      Alert.alert('Não foi possível registrar o peso.');
    }
  };
  const restartDemo = async () => {
    try {
      await resetCurrentMemberDemo.mutateAsync();
      feedback.success();
      signOut();
      router.replace('/login');
    } catch {
      Alert.alert('Não foi possível recomeçar', 'Confira sua conexão e tente novamente.', [
        { text: 'Cancelar', style: 'cancel' },
        { text: 'Tentar novamente', onPress: () => void restartDemo() },
      ]);
    }
  };
  const confirmDemoReset = () => Alert.alert(
    'Recomeçar demonstração?',
    'Seu perfil, treino, refeições e registros desta demonstração serão apagados. Você voltará ao login para começar de novo.',
    [{ text: 'Cancelar', style: 'cancel' }, {
      text: 'Apagar e sair', style: 'destructive', onPress: () => void restartDemo(),
    }],
  );

  return <Screen>
    <TopBar eyebrow="Seu caminho" title="Progresso" />

    <View style={styles.sectionHeading}>
      <Text style={styles.sectionKicker}>VISÃO GERAL</Text>
      <Text style={styles.sectionCopy}>Evolução construída sessão após sessão.</Text>
    </View>

    <View style={styles.metrics}>
      <MetricCard value={`${change > 0 ? '+' : ''}${change.toFixed(1).replace('.', ',')} kg`} label="desde o início" accent={change <= 0 ? 'red' : 'neutral'} />
      <MetricCard value={`${progress.data.consistencyPercent}%`} label="consistência" accent="green" />
    </View>

    <Card style={styles.methodCard}><Text style={styles.daysValue}>{progress.data.daysOnMethod}</Text><View><Text style={styles.daysLabel}>dias no método</Text><Text style={styles.daysCopy}>Disciplina, consistência e resultado.</Text></View></Card>

    <Card style={styles.chartCard}>
      <View style={styles.chartHeader}>
        <View><Text style={styles.chartTitle}>Peso corporal</Text><Text style={styles.chartCopy}>{history.length} registros no período</Text></View>
        <View style={styles.period}><Text style={styles.periodText}>HISTÓRICO</Text></View>
      </View>
      <WeightChart history={history} />
      <View style={styles.chartFooter}>
        <Text style={styles.chartDate}>{formatDate(history.at(0)?.recordedAt)}</Text>
        <View style={styles.currentWeight}><Text style={styles.currentWeightValue}>{progress.data.currentWeightKg.toFixed(1).replace('.', ',')} kg</Text><Text style={styles.currentWeightLabel}>ATUAL</Text></View>
        <Text style={styles.chartDate}>{formatDate(history.at(-1)?.recordedAt)}</Text>
      </View>
    </Card>

    <Card style={styles.strengthCard}>
      <Text style={styles.cardKicker}>FORÇA E MÉTODO</Text>
      <View style={styles.strengthRow}><View><Text style={styles.workoutValue}>{progress.data.strength ? `${progress.data.strength.currentLoadKg.toFixed(1).replace('.', ',')} kg` : '—'}</Text><Text style={styles.workoutLabel}>{progress.data.strength?.exerciseName ?? 'sem séries registradas'}</Text></View><View style={styles.strengthMarker}><Text style={styles.markerText}>{progress.data.strength ? `${progress.data.strength.changePercent > 0 ? '+' : ''}${progress.data.strength.changePercent.toFixed(1).replace('.', ',')}%` : 'EM EVOLUÇÃO'}</Text></View></View>
      <View style={styles.strengthRule} /><Text style={styles.strengthInsight}>{progress.data.strengthInsight}</Text>
    </Card>

    <Card style={styles.photosCard}>
      <View><Text style={styles.chartTitle}>Evolução física</Text><Text style={styles.chartCopy}>Registros visuais da demonstração.</Text></View>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.photosList} decelerationRate="fast" snapToInterval={174}>
        {progressPhotos.map((photo) => <View key={photo.label} style={styles.photoItem}><Image source={photo.source} style={styles.photo} resizeMode="cover" /><View style={styles.photoLabel}><Text style={styles.photoLabelText}>{photo.label}</Text></View></View>)}
      </ScrollView>
      <View style={styles.photoDots}>{progressPhotos.map((photo, index) => <View key={photo.label} style={[styles.photoDot, index === 0 && styles.photoDotActive]} />)}</View>
    </Card>

    <Card style={styles.logCard}>
      <Text style={styles.chartTitle}>Registrar peso</Text>
      <Text style={styles.chartCopy}>Use o mesmo horário e as mesmas condições sempre que possível.</Text>
      <View style={styles.form}><TextInput value={value} onChangeText={setValue} keyboardType="decimal-pad" placeholder="Ex.: 81,5" placeholderTextColor={colors.textMuted} style={styles.input} /><Button loading={addWeight.isPending} onPress={save}>Salvar</Button></View>
    </Card>
    {__DEV__ && <View style={styles.demoReset}><Text style={styles.demoResetCopy}>Ambiente de demonstração</Text><Text style={styles.demoResetDescription}>Apaga somente a sua jornada de teste neste aparelho e volta ao login.</Text><Button variant="ghost" loading={resetCurrentMemberDemo.isPending} onPress={confirmDemoReset} accessibilityLabel="Recomeçar minha demonstração" accessibilityHint="Apaga seu perfil e dados de demonstração após confirmação, depois volta ao login.">Recomeçar demonstração</Button></View>}
  </Screen>;
}

function MetricCard({ value, label, accent }: { value: string; label: string; accent: 'red' | 'green' | 'neutral' }) {
  return <Card style={styles.metric}><Text style={[styles.metricValue, accent === 'red' && styles.metricRed]}>{value}</Text><Text style={styles.metricLabel}>{label}</Text><View style={[styles.metricLine, accent === 'green' && styles.metricGreen]} /></Card>;
}

function WeightChart({ history }: { history: WeightEntry[] }) {
  const [width, setWidth] = useState(0);
  if (history.length < 2) return <View style={styles.emptyChart}><Text style={styles.emptyChartText}>Registre mais um peso para visualizar sua tendência.</Text></View>;
  const values = history.map((entry) => entry.weightKg);
  const low = Math.min(...values);
  const high = Math.max(...values);
  const range = Math.max(high - low, 0.5);
  const points = values.map((weight, index) => ({
    x: (index / (values.length - 1)) * width,
    y: chartHeight - 18 - ((weight - low) / range) * (chartHeight - 42),
  }));

  return <View style={styles.chartArea} onLayout={(event) => setWidth(event.nativeEvent.layout.width)}>
    <Text style={styles.axisTop}>{high.toFixed(1).replace('.', ',')}</Text><Text style={styles.axisBottom}>{low.toFixed(1).replace('.', ',')}</Text>
    <View style={styles.gridTop} /><View style={styles.gridMiddle} /><View style={styles.gridBottom} />
    {points.slice(1).map((point, index) => <TrendLine key={`line-${index}`} from={points[index]} to={point} />)}
    {points.map((point, index) => {
      const current = index === points.length - 1;
      return <View key={`point-${index}`} style={[styles.point, { left: point.x - (current ? 6 : 4), top: point.y - (current ? 6 : 4) }, current && styles.pointCurrent]} />;
    })}
  </View>;
}

function TrendLine({ from, to }: { from: { x: number; y: number }; to: { x: number; y: number } }) {
  const horizontal = to.x - from.x;
  const vertical = to.y - from.y;
  const length = Math.sqrt(horizontal ** 2 + vertical ** 2);
  const angle = `${Math.atan2(vertical, horizontal)}rad`;
  return <View style={[styles.trendLine, { left: from.x, top: from.y, width: length, transform: [{ rotate: angle }] }]} />;
}

function formatDate(value?: string) {
  return value ? new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit' }).format(new Date(value)) : '—';
}

const styles = StyleSheet.create({
  sectionHeading: { gap: spacing.xs, marginTop: -spacing.xs }, sectionKicker: { ...typography.caption, color: colors.primary, letterSpacing: 1.1 }, sectionCopy: { ...typography.bodyMD, color: colors.textSecondary },
  metrics: { flexDirection: 'row', gap: spacing.md }, metric: { flex: 1, gap: spacing.xs, minHeight: 126, justifyContent: 'space-between' }, metricValue: { ...typography.headingLG, color: colors.textPrimary }, metricRed: { color: colors.primary }, metricLabel: { ...typography.caption, color: colors.textSecondary, textTransform: 'uppercase' }, metricLine: { height: 3, width: '68%', backgroundColor: colors.primary, borderRadius: radius.pill }, metricGreen: { backgroundColor: colors.success },
  methodCard: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, paddingVertical: spacing.md }, daysValue: { ...typography.metricXL, color: colors.primary }, daysLabel: { ...typography.headingMD, color: colors.textPrimary }, daysCopy: { ...typography.bodyMD, color: colors.textSecondary, marginTop: spacing.xxs },
  chartCard: { gap: spacing.md, padding: spacing.lg }, chartHeader: { flexDirection: 'row', justifyContent: 'space-between', gap: spacing.sm }, chartTitle: { ...typography.headingMD, color: colors.textPrimary }, chartCopy: { ...typography.bodyMD, color: colors.textSecondary, marginTop: spacing.xs }, period: { alignSelf: 'flex-start', backgroundColor: colors.surfaceElevated, paddingHorizontal: spacing.sm, paddingVertical: spacing.xs, borderRadius: radius.sm }, periodText: { ...typography.caption, color: colors.textMuted, letterSpacing: .6 },
  chartArea: { height: chartHeight, marginTop: spacing.sm, marginLeft: 24, position: 'relative' }, gridTop: { position: 'absolute', top: 16, left: 0, right: 0, height: 1, backgroundColor: colors.border }, gridMiddle: { position: 'absolute', top: chartHeight / 2, left: 0, right: 0, height: 1, backgroundColor: colors.border }, gridBottom: { position: 'absolute', bottom: 16, left: 0, right: 0, height: 1, backgroundColor: colors.border }, axisTop: { ...typography.caption, color: colors.textMuted, position: 'absolute', left: -29, top: 8 }, axisBottom: { ...typography.caption, color: colors.textMuted, position: 'absolute', left: -29, bottom: 8 }, trendLine: { position: 'absolute', height: 2, backgroundColor: colors.primary, borderRadius: radius.pill, transformOrigin: 'left center' }, point: { position: 'absolute', width: 8, height: 8, borderRadius: 4, backgroundColor: colors.primary }, pointCurrent: { width: 12, height: 12, borderRadius: 6, backgroundColor: colors.textPrimary, borderWidth: 3, borderColor: colors.primary }, emptyChart: { height: chartHeight, alignItems: 'center', justifyContent: 'center', paddingHorizontal: spacing.xl }, emptyChartText: { ...typography.bodyMD, color: colors.textMuted, textAlign: 'center' },
  chartFooter: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }, chartDate: { ...typography.caption, color: colors.textMuted }, currentWeight: { alignItems: 'center', borderWidth: 1, borderColor: colors.primary, borderRadius: radius.sm, paddingHorizontal: spacing.sm, paddingVertical: spacing.xs, marginTop: -spacing.lg }, currentWeightValue: { ...typography.bodyMD, color: colors.textPrimary }, currentWeightLabel: { ...typography.caption, color: colors.primary, fontSize: 9 },
  strengthCard: { gap: spacing.md }, cardKicker: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, strengthRow: { flexDirection: 'row', alignItems: 'flex-end', justifyContent: 'space-between', gap: spacing.md }, workoutValue: { ...typography.metricXL, color: colors.textPrimary }, workoutLabel: { ...typography.caption, color: colors.textSecondary, textTransform: 'uppercase' }, strengthMarker: { borderWidth: 1, borderColor: '#245E42', backgroundColor: '#10291D', borderRadius: radius.sm, paddingHorizontal: spacing.sm, paddingVertical: spacing.xs }, markerText: { ...typography.caption, color: colors.success, fontSize: 10 }, strengthRule: { height: 1, backgroundColor: colors.border }, strengthInsight: { ...typography.bodyMD, color: colors.textSecondary },
  photosCard: { gap: spacing.md, paddingRight: 0 }, photosList: { gap: spacing.sm, paddingRight: spacing.lg }, photoItem: { width: 166, height: 236, borderRadius: radius.md, overflow: 'hidden', backgroundColor: colors.surfaceElevated }, photo: { width: '100%', height: '100%' }, photoLabel: { position: 'absolute', left: spacing.sm, right: spacing.sm, bottom: spacing.sm, backgroundColor: 'rgba(0, 0, 0, 0.72)', borderRadius: radius.sm, paddingVertical: spacing.xs, alignItems: 'center' }, photoLabelText: { ...typography.caption, color: colors.textPrimary, textTransform: 'uppercase', letterSpacing: .6 }, photoDots: { flexDirection: 'row', gap: spacing.xs, justifyContent: 'center', paddingRight: spacing.lg }, photoDot: { width: 5, height: 5, borderRadius: radius.pill, backgroundColor: colors.textMuted }, photoDotActive: { width: 18, backgroundColor: colors.primary },
  logCard: { gap: spacing.md }, form: { flexDirection: 'row', gap: spacing.sm }, input: { flex: 1, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated, paddingHorizontal: spacing.md, color: colors.textPrimary, ...typography.bodyLG },
  demoReset: { alignItems: 'center', gap: spacing.xs, paddingVertical: spacing.lg }, demoResetCopy: { ...typography.caption, color: colors.textMuted, textTransform: 'uppercase', letterSpacing: .8 }, demoResetDescription: { ...typography.bodyMD, color: colors.textSecondary, textAlign: 'center', maxWidth: 300 },
});
