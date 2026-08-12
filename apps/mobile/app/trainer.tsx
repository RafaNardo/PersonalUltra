import { router } from 'expo-router';
import { StyleSheet, Text, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainerDashboard } from '@/src/features/trainer/dashboard/hooks';

export default function TrainerEntryScreen() {
  const dashboard = useTrainerDashboard();
  if (dashboard.isLoading) return <LoadingView message="Abrindo seu painel…" />;
  if (dashboard.isError) return <ErrorView message={dashboard.error.message} onRetry={() => dashboard.refetch()} />;

  const data = dashboard.data!;
  return <Screen style={styles.page}>
    <View style={styles.header}><Text style={styles.eyebrow}>PAINEL DO PERSONAL</Text><Text style={styles.title}>Olá, {data.trainerName.split(' ')[0]}.</Text><Text style={styles.copy}>Acompanhe o início da jornada dos seus alunos.</Text></View>
    <View style={styles.metrics}>
      <Card style={styles.metric}><Text style={styles.metricValue}>{data.activeStudents}</Text><Text style={styles.metricLabel}>Alunos ativos</Text></Card>
      <Card style={styles.metric}><Text style={styles.metricValue}>{data.pendingAnamneses}</Text><Text style={styles.metricLabel}>Anamneses pendentes</Text></Card>
    </View>
    <View style={styles.section}><View style={styles.sectionHeader}><Text style={styles.sectionTitle}>Alunos recentes</Text><Button variant="ghost" style={styles.viewAll} onPress={() => router.push('/trainer/students')}>Ver todos</Button></View>{data.recentStudents.length === 0 ? <Card><Text style={styles.copy}>Seus próximos alunos aparecerão aqui.</Text></Card> : data.recentStudents.map((student) => <Card key={student.studentId} style={styles.student}><View style={styles.studentHeader}><Text style={styles.studentName}>{student.firstName} {student.lastName}</Text><Tag tone={student.anamnesisStatus === 'Completed' ? 'success' : 'neutral'}>{anamnesisLabel(student.anamnesisStatus)}</Tag></View>{student.email && <Text style={styles.studentEmail}>{student.email}</Text>}</Card>)}</View>
    <View style={styles.section}><Text style={styles.sectionTitle}>Atividade recente</Text>{data.recentActivities.length === 0 ? <Card><Text style={styles.copy}>Quando um aluno concluir a anamnese, a atualização aparecerá aqui.</Text></Card> : data.recentActivities.map((activity) => <Card key={`${activity.type}-${activity.studentId}-${activity.occurredAt}`} style={styles.activity}><Text style={styles.activityTitle}>{activity.studentName} concluiu a anamnese</Text><Text style={styles.studentEmail}>{new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(activity.occurredAt))}</Text></Card>)}</View>
    <Button variant="ghost" onPress={() => router.replace('/demo-role-switch')}>Trocar contexto demo</Button>
  </Screen>;
}

function anamnesisLabel(status: 'NotStarted' | 'InProgress' | 'Completed') {
  return status === 'Completed' ? 'CONCLUÍDA' : status === 'InProgress' ? 'EM PREENCHIMENTO' : 'AGUARDANDO ANAMNESE';
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.xl }, header: { gap: spacing.xs }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
  metrics: { flexDirection: 'row', gap: spacing.sm }, metric: { flex: 1, gap: spacing.xxs, backgroundColor: colors.surfaceElevated }, metricValue: { ...typography.metricXL, color: colors.signalGreen }, metricLabel: { ...typography.bodyMD, color: colors.textSecondary },
  section: { gap: spacing.sm }, sectionHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }, sectionTitle: { ...typography.headingMD, color: colors.textPrimary }, viewAll: { minHeight: 36, paddingHorizontal: spacing.sm }, student: { gap: spacing.xs }, studentHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.sm }, studentName: { ...typography.headingMD, color: colors.textPrimary, flex: 1 }, studentEmail: { ...typography.bodyMD, color: colors.textSecondary }, activity: { gap: spacing.xs }, activityTitle: { ...typography.bodyLG, color: colors.textPrimary },
});
