import { router } from 'expo-router';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudents } from '@/src/features/trainer/students/hooks';

export default function TrainerTrainingScreen() {
  const students = useTrainerStudents();
  if (students.isLoading) return <LoadingView message="Carregando alunos…" />;
  if (students.isError) return <ErrorView message={students.error.message} onRetry={() => students.refetch()} />;

  return <Screen style={styles.page}>
    <TopBar eyebrow="PRESCRIÇÃO" title="Treinos dos alunos" />
    <Text style={styles.copy}>Escolha um aluno para consultar seus treinos e abrir uma prescrição.</Text>
    <Button variant="secondary" onPress={() => router.push('/trainer/training/templates')}>Biblioteca de modelos</Button>
    {students.data!.length === 0 ? <EmptyState status="PRIMEIRO ALUNO" symbol="+" title="Comece uma prescrição pelo aluno." message="Convide um aluno para montar e acompanhar seus treinos." actionLabel="Convidar aluno" onAction={() => router.push('/trainer/invite')} /> : <View style={styles.list}>{students.data!.map((student) => <Pressable key={student.studentId} accessibilityRole="button" accessibilityLabel={`Abrir treinos de ${student.firstName} ${student.lastName}`} onPress={() => router.push({ pathname: '/trainer/students/[id]', params: { id: student.studentId, section: 'training' } })} style={({ pressed }) => pressed && styles.pressed}>
      <Card style={styles.student}>
        <View style={styles.header}><Text style={styles.name}>{student.firstName} {student.lastName}</Text><Tag tone={student.anamnesisStatus === 'Completed' ? 'success' : 'neutral'}>{student.anamnesisStatus === 'Completed' ? 'ATIVO' : 'EM ONBOARDING'}</Tag></View>
        {student.email ? <Text style={styles.copy}>{student.email}</Text> : null}
        <Text style={styles.openLink}>Ver treinos ›</Text>
      </Card>
    </Pressable>)}</View>}
  </Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, list: { gap: spacing.sm }, student: { gap: spacing.xs }, header: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.sm }, name: { ...typography.headingMD, color: colors.textPrimary, flex: 1 }, openLink: { ...typography.caption, color: colors.primary, marginTop: spacing.xs }, pressed: { opacity: .78 } });
