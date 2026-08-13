import { router } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { EmptyState, ErrorView, ListItem, LoadingView, SearchField, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudents } from '@/src/features/trainer/students/hooks';

export default function TrainerStudentsScreen() {
  const students = useTrainerStudents();
  const [query, setQuery] = useState('');
  if (students.isLoading) return <LoadingView message="Carregando seus alunos…" />;
  if (students.isError) return <ErrorView message={students.error.message} onRetry={() => students.refetch()} />;

  const normalizedQuery = query.trim().toLocaleLowerCase('pt-BR');
  const filteredStudents = normalizedQuery
    ? students.data!.filter((student) => `${student.firstName} ${student.lastName} ${student.email ?? ''}`.toLocaleLowerCase('pt-BR').includes(normalizedQuery))
    : students.data!;

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="GESTÃO DE ALUNOS" title="Seus alunos" onBack={() => router.back()} />
    <Text style={styles.copy}>{students.data!.length} {students.data!.length === 1 ? 'aluno ativo' : 'alunos ativos'}</Text>
    <SearchField value={query} onChangeText={setQuery} placeholder="Buscar por nome ou e-mail" accessibilityLabel="Buscar alunos" />
    {filteredStudents.length === 0 ? <EmptyState status={query.trim() ? 'BUSCA SEM RESULTADO' : 'COMECE SUA CARTEIRA'} symbol="+" title={query.trim() ? 'Nenhum aluno corresponde à busca.' : 'Seus alunos aparecerão aqui.'} message={query.trim() ? 'Revise o nome ou e-mail informado para tentar novamente.' : 'Convide o primeiro aluno para iniciar o acompanhamento.'} actionLabel={query.trim() ? 'Limpar busca' : 'Convidar aluno'} onAction={() => query.trim() ? setQuery('') : router.push('/trainer/invite')} /> : <View style={styles.list}>{filteredStudents.map((student) => <ListItem key={student.studentId} title={`${student.firstName} ${student.lastName}`} metadata={student.email ?? 'E-mail não informado'} badge={<Tag tone={student.anamnesisStatus === 'Completed' ? 'success' : 'neutral'}>{anamnesisLabel(student.anamnesisStatus)}</Tag>} onPress={() => router.push({ pathname: '/trainer/students/[id]', params: { id: student.studentId } })} accessibilityLabel={`Abrir ${student.firstName} ${student.lastName}`} accessibilityHint="Abre o resumo e as ações do aluno" />)}</View>}
  </Screen>;
}

function anamnesisLabel(status: 'NotStarted' | 'InProgress' | 'Completed') {
  return status === 'Completed' ? 'CONCLUÍDA' : status === 'InProgress' ? 'EM PREENCHIMENTO' : 'AGUARDANDO ANAMNESE';
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary }, list: { gap: spacing.xs },
});
