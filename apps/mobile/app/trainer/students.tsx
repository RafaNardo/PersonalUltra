import { router } from 'expo-router';
import { useMemo, useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { Card, EmptyState, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudents } from '@/src/features/trainer/students/hooks';

export default function TrainerStudentsScreen() {
  const students = useTrainerStudents();
  const [query, setQuery] = useState('');
  if (students.isLoading) return <LoadingView message="Carregando seus alunos…" />;
  if (students.isError) return <ErrorView message={students.error.message} onRetry={() => students.refetch()} />;

  const filteredStudents = useMemo(() => {
    const normalizedQuery = query.trim().toLocaleLowerCase('pt-BR');
    if (!normalizedQuery) return students.data!;
    return students.data!.filter((student) => `${student.firstName} ${student.lastName} ${student.email ?? ''}`.toLocaleLowerCase('pt-BR').includes(normalizedQuery));
  }, [query, students.data]);

  return <Screen style={styles.page}>
    <TopBar eyebrow="GESTÃO DE ALUNOS" title="Seus alunos" onBack={() => router.back()} />
    <Text style={styles.copy}>{students.data!.length} {students.data!.length === 1 ? 'aluno ativo' : 'alunos ativos'}</Text>
    <TextInput value={query} onChangeText={setQuery} autoCapitalize="none" autoCorrect={false} placeholder="Buscar por nome ou e-mail" placeholderTextColor={colors.textMuted} accessibilityLabel="Buscar alunos" style={styles.search} />
    {filteredStudents.length === 0 ? <EmptyState status={query.trim() ? 'BUSCA SEM RESULTADO' : 'COMECE SUA CARTEIRA'} symbol="+" title={query.trim() ? 'Nenhum aluno corresponde à busca.' : 'Seus alunos aparecerão aqui.'} message={query.trim() ? 'Revise o nome ou e-mail informado para tentar novamente.' : 'Convide o primeiro aluno para iniciar o acompanhamento.'} actionLabel={query.trim() ? 'Limpar busca' : 'Convidar aluno'} onAction={() => query.trim() ? setQuery('') : router.push('/trainer/invite')} /> : <View style={styles.list}>{filteredStudents.map((student) => <Pressable key={student.studentId} accessibilityRole="button" accessibilityLabel={`Abrir ${student.firstName} ${student.lastName}`} accessibilityHint="Abre o resumo e as ações do aluno" onPress={() => router.push({ pathname: '/trainer/students/[id]', params: { id: student.studentId } })} style={({ pressed }) => pressed && styles.pressed}><Card style={styles.student}><View style={styles.studentHeader}><Text style={styles.studentName}>{student.firstName} {student.lastName}</Text><Tag tone={student.anamnesisStatus === 'Completed' ? 'success' : 'neutral'}>{anamnesisLabel(student.anamnesisStatus)}</Tag></View>{student.email && <Text style={styles.email}>{student.email}</Text>}<View style={styles.action}><Text style={styles.actionText}>Ver detalhes</Text><Text style={styles.chevron}>›</Text></View></Card></Pressable>)}</View>}
  </Screen>;
}

function anamnesisLabel(status: 'NotStarted' | 'InProgress' | 'Completed') {
  return status === 'Completed' ? 'CONCLUÍDA' : status === 'InProgress' ? 'EM PREENCHIMENTO' : 'AGUARDANDO ANAMNESE';
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary }, search: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, paddingHorizontal: spacing.md, paddingVertical: spacing.sm, minHeight: 50, backgroundColor: colors.surface }, list: { gap: spacing.sm }, pressed: { opacity: .78 }, student: { gap: spacing.xs }, studentHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.sm }, studentName: { ...typography.headingMD, color: colors.textPrimary, flex: 1 }, email: { ...typography.bodyMD, color: colors.textSecondary }, action: { flexDirection: 'row', alignItems: 'center', gap: spacing.xs, marginTop: spacing.xs }, actionText: { ...typography.caption, color: colors.primary }, chevron: { fontSize: 22, lineHeight: 18, color: colors.primary },
});
