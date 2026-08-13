import { router } from 'expo-router';
import { useMemo, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Button, EmptyState, ErrorView, ListItem, LoadingView, SearchField, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudents } from '@/src/features/trainer/students/hooks';

export default function TrainerTrainingScreen() {
  const students = useTrainerStudents();
  const [search, setSearch] = useState('');
  const filtered = useMemo(() => {
    const term = search.trim().toLocaleLowerCase('pt-BR');
    return term ? (students.data ?? []).filter((student) => `${student.firstName} ${student.lastName} ${student.email ?? ''}`.toLocaleLowerCase('pt-BR').includes(term)) : (students.data ?? []);
  }, [search, students.data]);
  if (students.isLoading) return <LoadingView message="Carregando alunos…" />;
  if (students.isError) return <ErrorView message={students.error.message} onRetry={() => students.refetch()} />;

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="PRESCRIÇÃO" title="Treinos dos alunos" />
    <Text style={styles.copy}>Escolha um aluno para consultar seus treinos e abrir uma prescrição.</Text>
    <Button variant="secondary" onPress={() => router.push('/trainer/training/templates')}>Biblioteca de modelos</Button>
    {students.data!.length > 0 ? <SearchField value={search} onChangeText={setSearch} placeholder="Buscar aluno…" accessibilityLabel="Buscar aluno para prescrição" /> : null}
    {students.data!.length === 0 ? <EmptyState status="PRIMEIRO ALUNO" symbol="+" title="Comece uma prescrição pelo aluno." message="Convide um aluno para montar e acompanhar seus treinos." actionLabel="Convidar aluno" onAction={() => router.push('/trainer/invite')} /> : filtered.length === 0 ? <EmptyState variant="inline" status="BUSCA SEM RESULTADO" symbol="⌕" title="Não encontramos esse aluno." message="Tente outro nome ou e-mail para localizar quem receberá a prescrição." actionLabel="Limpar busca" onAction={() => setSearch('')} /> : <View style={styles.list}>{filtered.map((student) => <ListItem key={student.studentId} title={`${student.firstName} ${student.lastName}`} metadata={student.email ?? 'E-mail não informado'} badge={<Tag tone={student.anamnesisStatus === 'Completed' ? 'success' : 'neutral'}>{student.anamnesisStatus === 'Completed' ? 'ATIVO' : 'EM ONBOARDING'}</Tag>} actionLabel="Abrir treinos" onPress={() => router.push({ pathname: '/trainer/students/[id]', params: { id: student.studentId, section: 'training' } })} accessibilityLabel={`Abrir treinos de ${student.firstName} ${student.lastName}`} />)}</View>}
  </Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, list: { gap: spacing.xs } });
