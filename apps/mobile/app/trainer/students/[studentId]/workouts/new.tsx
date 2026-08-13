import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { Alert, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useCreateTrainerStudentWorkout } from '@/src/features/trainer/training/hooks';
import { feedback } from '@/src/platform/feedback';

const weekdays = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'];

export default function CreateStudentWorkoutScreen() {
  const { studentId } = useLocalSearchParams<{ studentId: string }>();
  const student = useTrainerStudent(studentId ?? '');
  const create = useCreateTrainerStudentWorkout(studentId ?? '');
  const [name, setName] = useState('');
  const [notes, setNotes] = useState('');
  const [recommendedDay, setRecommendedDay] = useState(1);
  const normalizedName = name.trim();
  const valid = normalizedName.length > 0 && normalizedName.length <= 200 && notes.length <= 2000;

  if (!studentId) return <ErrorView message="Não foi possível identificar o aluno deste treino." />;
  if (student.isLoading) return <LoadingView message="Preparando o novo treino…" />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;

  const createWorkout = async () => {
    try {
      const workout = await create.mutateAsync({ name: normalizedName, notes: notes.trim(), recommendedDay });
      feedback.success();
      router.replace({ pathname: '/trainer/students/[studentId]/workouts/[workoutId]', params: { studentId, workoutId: workout.id } });
    } catch (error) {
      feedback.warning();
      Alert.alert('Não foi possível criar o treino', error instanceof Error ? error.message : 'Tente novamente.');
    }
  };

  return <Screen style={styles.page}>
    <TopBar eyebrow="NOVO TREINO" title="Começar do zero" onBack={() => router.back()} />
    <Card style={styles.context}><Text style={styles.label}>CRIANDO PARA</Text><Text style={styles.studentName}>{student.data!.firstName} {student.data!.lastName}</Text><Text style={styles.copy}>Defina a identidade do treino. Na próxima tela, você adicionará os exercícios pelo catálogo e publicará a prescrição.</Text></Card>

    <Card style={styles.form}>
      <View style={styles.field}><Text style={styles.label}>Nome do treino</Text><TextInput value={name} onChangeText={setName} maxLength={200} placeholder="Ex.: Treino A · membros superiores" placeholderTextColor={colors.textMuted} accessibilityLabel="Nome do novo treino" style={styles.input} /></View>
      <View style={styles.field}><View style={styles.labelRow}><Text style={styles.label}>Observações</Text><Text style={styles.optional}>OPCIONAL</Text></View><TextInput value={notes} onChangeText={setNotes} maxLength={2000} multiline textAlignVertical="top" placeholder="Orientações gerais para o aluno" placeholderTextColor={colors.textMuted} accessibilityLabel="Observações do novo treino" style={[styles.input, styles.notes]} /></View>
      <View style={styles.field}><Text style={styles.label}>Dia de referência</Text><Text style={styles.copy}>Organiza a agenda semanal, mas o treino continua ativo até você alterá-lo.</Text><ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.days}>{weekdays.map((day, index) => <Pressable key={day} accessibilityRole="radio" accessibilityState={{ checked: recommendedDay === index + 1 }} onPress={() => setRecommendedDay(index + 1)} style={[styles.day, recommendedDay === index + 1 && styles.daySelected]}><Text style={[styles.dayText, recommendedDay === index + 1 && styles.dayTextSelected]}>{day}</Text></Pressable>)}</ScrollView></View>
    </Card>

    <Button loading={create.isPending} disabled={!valid || create.isPending} onPress={() => void createWorkout()}>Criar e adicionar exercícios</Button>
    <Text style={styles.footer}>O treino só ficará pronto para execução depois que você adicionar exercícios e publicar as alterações.</Text>
  </Screen>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, context: { gap: spacing.xs, borderColor: colors.primary, backgroundColor: colors.surfaceElevated }, label: { ...typography.caption, color: colors.primary, letterSpacing: .8 }, studentName: { ...typography.headingLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, form: { gap: spacing.lg }, field: { gap: spacing.xs }, labelRow: { flexDirection: 'row', justifyContent: 'space-between', gap: spacing.sm }, optional: { ...typography.caption, color: colors.textMuted }, input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 52, paddingHorizontal: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.background }, notes: { minHeight: 100, paddingTop: spacing.md }, days: { gap: spacing.xs }, day: { minWidth: 48, minHeight: 44, alignItems: 'center', justifyContent: 'center', borderRadius: radius.pill, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface }, daySelected: { borderColor: colors.primary, backgroundColor: '#3A1D0C' }, dayText: { ...typography.caption, color: colors.textSecondary }, dayTextSelected: { color: colors.primary }, footer: { ...typography.caption, color: colors.textMuted, lineHeight: 18, textAlign: 'center' },
});
