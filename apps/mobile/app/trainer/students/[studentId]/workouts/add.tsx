import Ionicons from '@expo/vector-icons/Ionicons';
import { router, useLocalSearchParams } from 'expo-router';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';

export default function AddStudentWorkoutScreen() {
  const { studentId } = useLocalSearchParams<{ studentId: string }>();
  const student = useTrainerStudent(studentId ?? '');
  if (!studentId) return <ErrorView message="Não foi possível identificar o aluno." />;
  if (student.isLoading) return <LoadingView message="Preparando as opções…" />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;

  const firstName = student.data!.firstName;
  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow={`NOVO TREINO · ${firstName}`} title="Como você quer começar?" onBack={() => router.back()} />
    <Text style={styles.intro}>Escolha o ponto de partida. Nas duas opções, você poderá revisar a prescrição antes que ela fique disponível para {firstName}.</Text>
    <View style={styles.options}>
      <Option icon="create-outline" eyebrow="PRESCRIÇÃO EXCLUSIVA" title="Criar do zero" description="Dê um nome ao treino e monte cada exercício pelo catálogo, com séries, repetições e descanso." action="Começar treino novo" onPress={() => router.push({ pathname: '/trainer/students/[studentId]/workouts/new', params: { studentId } })} />
      <Option icon="copy-outline" eyebrow="PONTO DE PARTIDA RÁPIDO" title="Usar um preset" description="Escolha uma estrutura da sua biblioteca, confira os exercícios e organize o dia para este aluno." action="Escolher um preset" onPress={() => router.push({ pathname: '/trainer/students/[studentId]/workouts/from-template', params: { studentId } })} />
    </View>
    <Text style={styles.note}>Presets são apenas um ponto de partida: o treino aplicado ao aluno continua independente e editável.</Text>
  </Screen>;
}

function Option({ icon, eyebrow, title, description, action, onPress }: { icon: keyof typeof Ionicons.glyphMap; eyebrow: string; title: string; description: string; action: string; onPress: () => void }) {
  return <Pressable accessibilityRole="button" accessibilityLabel={title} accessibilityHint={description} onPress={onPress} style={({ pressed }) => [styles.option, pressed && styles.pressed]}>
    <View style={styles.icon}><Ionicons name={icon} size={28} color={colors.primary} /></View>
    <View style={styles.identity}><Text style={styles.eyebrow}>{eyebrow}</Text><Text style={styles.title}>{title}</Text><Text style={styles.description}>{description}</Text><View style={styles.action}><Text style={styles.actionText}>{action}</Text><Ionicons name="arrow-forward" size={18} color={colors.primary} /></View></View>
  </Pressable>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.lg }, intro: { ...typography.bodyLG, color: colors.textSecondary, lineHeight: 25 }, options: { gap: spacing.md },
  option: { flexDirection: 'row', alignItems: 'flex-start', gap: spacing.md, padding: spacing.lg, borderRadius: radius.lg, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface }, pressed: { opacity: .76, transform: [{ scale: .99 }] },
  icon: { width: 56, height: 56, borderRadius: radius.md, alignItems: 'center', justifyContent: 'center', backgroundColor: '#3A1D0C' }, identity: { flex: 1, gap: spacing.xs }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: .8 }, title: { ...typography.headingLG, color: colors.textPrimary }, description: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, action: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs, marginTop: spacing.xs }, actionText: { ...typography.caption, color: colors.primary }, note: { ...typography.caption, color: colors.textMuted, lineHeight: 18, textAlign: 'center' },
});
