import { router } from 'expo-router';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Card, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudents } from '@/src/features/trainer/students/hooks';

export default function TrainerStudentsScreen() {
  const students = useTrainerStudents();
  if (students.isLoading) return <LoadingView message="Carregando seus alunos…" />;
  if (students.isError) return <ErrorView message={students.error.message} onRetry={() => students.refetch()} />;

  return <Screen style={styles.page}>
    <TopBar eyebrow="GESTÃO DE ALUNOS" title="Seus alunos" onBack={() => router.back()} />
    <Text style={styles.copy}>{students.data!.length} {students.data!.length === 1 ? 'aluno ativo' : 'alunos ativos'}</Text>
    <View style={styles.list}>{students.data!.map((student) => <Pressable key={student.studentId} accessibilityRole="button" accessibilityLabel={`Abrir ${student.firstName} ${student.lastName}`} onPress={() => router.push({ pathname: '/trainer/students/[id]', params: { id: student.studentId } })} style={({ pressed }) => pressed && styles.pressed}><Card style={styles.student}><View style={styles.studentHeader}><Text style={styles.studentName}>{student.firstName} {student.lastName}</Text><Tag tone={student.anamnesisStatus === 'Completed' ? 'success' : 'neutral'}>{anamnesisLabel(student.anamnesisStatus)}</Tag></View>{student.email && <Text style={styles.email}>{student.email}</Text>}</Card></Pressable>)}</View>
  </Screen>;
}

function anamnesisLabel(status: 'NotStarted' | 'InProgress' | 'Completed') {
  return status === 'Completed' ? 'CONCLUÍDA' : status === 'InProgress' ? 'EM PREENCHIMENTO' : 'AGUARDANDO ANAMNESE';
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary }, list: { gap: spacing.sm }, pressed: { opacity: .78 }, student: { gap: spacing.xs }, studentHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.sm }, studentName: { ...typography.headingMD, color: colors.textPrimary, flex: 1 }, email: { ...typography.bodyMD, color: colors.textSecondary },
});
