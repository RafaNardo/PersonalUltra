import { useQueries } from '@tanstack/react-query';
import { router } from 'expo-router';
import { useMemo, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Button, EmptyState, ErrorView, ListItem, LoadingView, SearchField, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { trainerClient } from '@/src/api/trainer-client';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudents } from '@/src/features/trainer/students/hooks';

export function TrainerNutritionOverviewScreen() {
  const students = useTrainerStudents();
  const [search, setSearch] = useState('');
  const plans = useQueries({ queries: (students.data ?? []).map((student) => ({ queryKey: ['trainer', 'students', student.studentId, 'nutrition'], queryFn: () => trainerClient.nutrition(student.studentId) })) });
  const filtered = useMemo(() => { const term = search.trim().toLocaleLowerCase('pt-BR'); return (students.data ?? []).map((student, index) => ({ student, plan: plans[index]?.data })).filter(({ student }) => !term || `${student.firstName} ${student.lastName} ${student.email ?? ''}`.toLocaleLowerCase('pt-BR').includes(term)); }, [plans, search, students.data]);
  if (students.isLoading) return <LoadingView message="Carregando alimentação dos alunos…" />;
  if (students.isError) return <ErrorView message={students.error.message} onRetry={() => students.refetch()} />;
  if (plans.some((plan) => plan.isLoading)) return <LoadingView message="Consultando os planos atuais…" />;
  const failedPlan = plans.find((plan) => plan.isError);
  if (failedPlan?.error) return <ErrorView message={failedPlan.error.message} onRetry={() => { plans.forEach((plan) => { if (plan.isError) void plan.refetch(); }); }} />;
  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="PRESCRIÇÃO" title="Alimentação dos alunos" />
    <Text style={styles.copy}>Consulte o plano atual de cada aluno ou comece uma nova alimentação.</Text>
    <Button variant="secondary" onPress={() => router.push('/trainer/nutrition/templates')}>Presets de refeição</Button>
    {students.data!.length ? <SearchField value={search} onChangeText={setSearch} placeholder="Buscar aluno…" accessibilityLabel="Buscar aluno para alimentação" /> : null}
    {!students.data!.length ? <EmptyState status="PRIMEIRO ALUNO" symbol="+" title="Comece pelo aluno." message="Convide um aluno para montar e acompanhar sua alimentação." actionLabel="Convidar aluno" onAction={() => router.push('/trainer/invite')} /> : !filtered.length ? <EmptyState variant="inline" status="BUSCA SEM RESULTADO" symbol="⌕" title="Não encontramos esse aluno." message="Tente outro nome ou e-mail." actionLabel="Limpar busca" onAction={() => setSearch('')} /> : <View style={styles.list}>{filtered.map(({ student, plan }) => <ListItem key={student.studentId} title={`${student.firstName} ${student.lastName}`} metadata={plan?.name ?? 'Nenhum plano alimentar'} badge={<Tag tone={plan ? 'success' : 'neutral'}>{plan ? 'COM PLANO' : 'SEM PLANO'}</Tag>} actionLabel="Abrir alimentação" onPress={() => router.push({ pathname: '/trainer/students/[id]', params: { id: student.studentId, section: 'nutrition' } })} />)}</View>}
  </Screen>;
}
const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, list: { gap: spacing.xs } });
