import { router } from 'expo-router';
import { useEffect, useRef, useState } from 'react';
import { ActivityIndicator, Animated, StyleSheet, Text, View } from 'react-native';
import { usePrepareInitialPlan } from '@/src/api/hooks';
import { Button, Card, ErrorView, ProgressBar, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';

const stages = [
  { label: 'DADOS REGISTRADOS', title: 'Seu ponto de partida está mapeado.', copy: 'Organizando sua rotina, disponibilidade e estrutura de treino.' },
  { label: 'CICLO DE TREINO', title: 'Definindo a estrutura da sua semana.', copy: 'Preparando sessões, exercícios e prescrições iniciais do método SVR.' },
  { label: 'ACOMPANHAMENTO', title: 'Incluindo sua linha de evolução.', copy: 'Registrando o histórico demonstrativo de peso, consistência e força.' },
  { label: 'NUTRIÇÃO', title: 'Conectando treino e alimentação.', copy: 'Adicionando refeições e metas para acompanhar sua rotina.' },
  { label: 'PLANO PRONTO', title: 'Seu próximo ciclo está tomando forma.', copy: 'Conferindo tudo para apresentar sua experiência inicial.' },
];
const minimumPresentationMs = 10_000;

function formatDate(value?: string | null) {
  if (!value) return null;
  const [year, month, day] = value.slice(0, 10).split('-').map(Number);
  if (!year || !month || !day) return null;
  return new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: 'long' }).format(new Date(year, month - 1, day));
}

export default function PreparePlanScreen() {
  const plan = usePrepareInitialPlan();
  const [stage, setStage] = useState(0);
  const [minimumElapsed, setMinimumElapsed] = useState(false);
  const [presentationVisible, setPresentationVisible] = useState(false);
  const pulse = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    const timers = [
      setTimeout(() => setStage(1), 2_000),
      setTimeout(() => setStage(2), 4_000),
      setTimeout(() => setStage(3), 6_000),
      setTimeout(() => setStage(4), 8_000),
      setTimeout(() => setMinimumElapsed(true), minimumPresentationMs),
    ];
    return () => timers.forEach(clearTimeout);
  }, []);
  useEffect(() => {
    const animation = Animated.loop(Animated.sequence([
      Animated.timing(pulse, { toValue: 1, duration: 900, useNativeDriver: true }),
      Animated.timing(pulse, { toValue: 0, duration: 900, useNativeDriver: true }),
    ]));
    animation.start();
    return () => animation.stop();
  }, [pulse]);
  if (plan.isError) return <ErrorView message={plan.error.message} onRetry={() => { void plan.refetch(); }} />;

  if (plan.isLoading || !minimumElapsed) {
    const current = stages[stage];
    return <Screen scroll={false} style={styles.preparing}>
      <View accessibilityRole="progressbar" accessibilityLiveRegion="polite" accessibilityLabel={current.title} style={styles.preparingContent}>
        <View style={styles.progressVisual}><Animated.View style={[styles.progressGlow, { opacity: pulse.interpolate({ inputRange: [0, 1], outputRange: [.25, .8] }), transform: [{ scale: pulse.interpolate({ inputRange: [0, 1], outputRange: [1, 1.14] }) }] }]} /><View style={styles.progressIcon}><ActivityIndicator size="large" color={colors.primary} /><Text style={styles.progressNumber}>{String(stage + 1).padStart(2, '0')}</Text></View></View>
        <View style={styles.preparingHeadline}><Text style={styles.eyebrow}>{current.label}</Text><Text style={styles.preparingTitle}>{current.title}</Text><Text style={styles.preparingCopy}>{current.copy}</Text></View>
        <View style={styles.stageTrack}>{stages.map((item, index) => <View key={item.label} style={[styles.stageDot, index <= stage && styles.stageDotActive]} />)}</View>
        <ProgressBar value={(stage + 1) / stages.length} />
        <Text style={styles.honesty}>Criando uma estrutura inicial padrão do método SVR. A geração individualizada por IA não é usada nesta demonstração.</Text>
      </View>
    </Screen>;
  }

  if (!plan.data?.isProvisioned || !plan.data.name || !plan.data.sessionsPerWeek) {
    return <ErrorView message="Não encontramos seu plano inicial." onRetry={() => { void plan.refetch(); }} />;
  }

  if (!presentationVisible) {
    return <Screen scroll={false} style={styles.readyScreen}>
      <View accessibilityRole="alert" accessibilityLiveRegion="assertive" style={styles.readyContent}>
        <View style={styles.readyMark}><Text style={styles.readyMarkText}>SVR</Text></View>
        <Text style={styles.eyebrow}>PLANO LIBERADO</Text>
        <Text style={styles.readyTitle}>Seu plano ficou pronto.</Text>
        <Text style={styles.readyCopy}>Pronto para transformar disciplina em resultado?</Text>
        <View style={styles.readyRule} />
        <Text style={styles.readyDetail}>Treino, nutrição e seu histórico demonstrativo já estão conectados.</Text>
        <Button onPress={() => setPresentationVisible(true)} style={styles.readyButton}>PRONTO</Button>
      </View>
    </Screen>;
  }

  const startsOn = formatDate(plan.data.startsOn);
  return <Screen>
    <TopBar eyebrow="PLANO INICIAL" title="Tudo pronto." />
    <Card style={styles.hero}>
      <Tag tone="success">ESTRUTURA SVR</Tag>
      <Text style={styles.planName}>{plan.data.name}</Text>
      <Text style={styles.heroCopy}>Seu plano de demonstração está preparado com treino e alimentação para você explorar o método.</Text>
      <View style={styles.metrics}>
        <View><Text style={styles.metric}>{plan.data.sessionsPerWeek}x</Text><Text style={styles.metricLabel}>por semana</Text></View>
        <View style={styles.metricDivider} />
        <View><Text style={styles.metric}>{plan.data.workouts.length}</Text><Text style={styles.metricLabel}>sessões</Text></View>
        {startsOn && <><View style={styles.metricDivider} /><View><Text style={styles.metricSmall}>{startsOn}</Text><Text style={styles.metricLabel}>início</Text></View></>}
      </View>
    </Card>
    <View style={styles.sectionHeader}><Text style={styles.sectionTitle}>SEU CICLO DE TREINO</Text><Text style={styles.sectionCopy}>Cada sessão já vem com a prescrição inicial.</Text></View>
    <View style={styles.workouts}>{plan.data.workouts.map((workout) => <Card key={workout.id} style={styles.workout}><View style={styles.sequence}><Text style={styles.sequenceText}>{workout.sequence}</Text></View><View style={styles.workoutBody}><Text style={styles.workoutName}>{workout.name}</Text><Text style={styles.workoutCopy}>{workout.exerciseCount} exercícios prescritos</Text></View></Card>)}</View>
    {plan.data.nutrition && <Card style={styles.nutrition}><View><Text style={styles.sectionTitle}>SUA NUTRIÇÃO</Text><Text style={styles.nutritionCalories}>{plan.data.nutrition.caloriesTarget.toLocaleString('pt-BR')} kcal</Text><Text style={styles.sectionCopy}>meta diária inicial</Text></View><View style={styles.macros}><Text style={styles.macro}>P {plan.data.nutrition.proteinGramsTarget}g</Text><Text style={styles.macro}>C {plan.data.nutrition.carbsGramsTarget}g</Text><Text style={styles.macro}>G {plan.data.nutrition.fatGramsTarget}g</Text></View><View style={styles.meals}>{plan.data.nutrition.meals.map((meal) => <View key={meal} style={styles.meal}><Text style={styles.mealText}>{meal}</Text></View>)}</View></Card>}
    <Card style={styles.note}><Text style={styles.noteTitle}>Seu ponto de partida está salvo</Text><Text style={styles.noteCopy}>A estrutura mostrada é o plano padrão da demonstração. Ajustes individualizados e geração por IA não fazem parte desta etapa.</Text></Card>
    <Button onPress={() => router.replace('/(app)/home')}>Conhecer minha Home</Button>
  </Screen>;
}

const styles = StyleSheet.create({
  preparing: { justifyContent: 'center' }, preparingContent: { gap: spacing.xl, alignItems: 'center' }, progressVisual: { width: 168, height: 168, alignItems: 'center', justifyContent: 'center' }, progressGlow: { position: 'absolute', width: 168, height: 168, borderRadius: 84, backgroundColor: '#4D1520' }, progressIcon: { width: 116, height: 116, borderRadius: 58, justifyContent: 'center', alignItems: 'center', borderWidth: 1, borderColor: colors.primary, backgroundColor: colors.surface }, progressNumber: { ...typography.headingMD, color: colors.primary, marginTop: spacing.xs }, preparingHeadline: { gap: spacing.sm, alignItems: 'center' }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1.2 }, preparingTitle: { ...typography.displayLG, color: colors.textPrimary, textAlign: 'center' }, preparingCopy: { ...typography.bodyLG, color: colors.textSecondary, textAlign: 'center', lineHeight: 24 }, stageTrack: { flexDirection: 'row', gap: spacing.xs }, stageDot: { width: 8, height: 8, borderRadius: radius.pill, backgroundColor: colors.surfaceElevated }, stageDotActive: { width: 24, backgroundColor: colors.primary }, honesty: { ...typography.bodyMD, color: colors.textMuted, textAlign: 'center', lineHeight: 21 },
  readyScreen: { justifyContent: 'center' }, readyContent: { gap: spacing.lg, alignItems: 'center' }, readyMark: { width: 92, height: 92, borderRadius: 46, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.primary, shadowColor: colors.primary, shadowOpacity: .45, shadowRadius: 20, elevation: 8 }, readyMarkText: { ...typography.headingLG, color: colors.textPrimary }, readyTitle: { ...typography.displayLG, color: colors.textPrimary, textAlign: 'center' }, readyCopy: { ...typography.headingMD, color: colors.textSecondary, textAlign: 'center' }, readyRule: { width: 52, height: 3, borderRadius: radius.pill, backgroundColor: colors.primary }, readyDetail: { ...typography.bodyMD, color: colors.textMuted, textAlign: 'center', lineHeight: 21 }, readyButton: { alignSelf: 'stretch', marginTop: spacing.sm },
  hero: { gap: spacing.md, padding: spacing.xl }, planName: { ...typography.headingLG, color: colors.textPrimary }, heroCopy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, metrics: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, marginTop: spacing.xs }, metric: { ...typography.metricXL, color: colors.primary }, metricSmall: { ...typography.headingMD, color: colors.textPrimary }, metricLabel: { ...typography.caption, color: colors.textMuted }, metricDivider: { height: 42, width: 1, backgroundColor: colors.border },
  sectionHeader: { gap: spacing.xxs }, sectionTitle: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, sectionCopy: { ...typography.bodyMD, color: colors.textSecondary }, workouts: { gap: spacing.sm }, workout: { flexDirection: 'row', alignItems: 'center', gap: spacing.md, padding: spacing.md }, sequence: { width: 36, height: 36, borderRadius: radius.pill, alignItems: 'center', justifyContent: 'center', backgroundColor: '#42121A' }, sequenceText: { ...typography.headingMD, color: colors.primary }, workoutBody: { flex: 1, gap: spacing.xxs }, workoutName: { ...typography.bodyLG, color: colors.textPrimary, fontFamily: 'MontserratBold' }, workoutCopy: { ...typography.caption, color: colors.textSecondary },
  nutrition: { gap: spacing.md, borderColor: '#4D1520' }, nutritionCalories: { ...typography.metricXL, color: colors.textPrimary, marginTop: spacing.xs }, macros: { flexDirection: 'row', gap: spacing.sm, flexWrap: 'wrap' }, macro: { ...typography.caption, color: colors.success, borderRadius: radius.pill, paddingHorizontal: spacing.sm, paddingVertical: spacing.xs, backgroundColor: '#123D2B' }, meals: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs }, meal: { borderRadius: radius.pill, paddingHorizontal: spacing.sm, paddingVertical: spacing.xs, backgroundColor: colors.surfaceElevated }, mealText: { ...typography.caption, color: colors.textSecondary },
  note: { gap: spacing.xs, backgroundColor: colors.surfaceElevated }, noteTitle: { ...typography.headingMD, color: colors.textPrimary }, noteCopy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
});
