import { router, useLocalSearchParams } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { StyleSheet, Text, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { trainerClient } from '@/src/api/trainer-client';

export default function TrainerTrainingDetailScreen() {
  const { id } = useLocalSearchParams<{ id: string }>(); const item = useQuery({ queryKey: ['trainer', 'training', id], queryFn: () => trainerClient.template(id!), enabled: Boolean(id) });
  if (item.isLoading) return <LoadingView message="Abrindo treino…" />; if (item.isError) return <ErrorView message={item.error.message} onRetry={() => item.refetch()} />;
  return <Screen style={styles.page}><TopBar eyebrow="MODELO DE TREINO" title={item.data!.name} onBack={() => router.back()} /><Text style={styles.copy}>{item.data!.notes}</Text><View style={styles.list}>{item.data!.exercises!.map((exercise) => <Card key={`${exercise.sequence}-${exercise.name}`} style={styles.exercise}><Text style={styles.name}>{exercise.sequence}. {exercise.name}</Text><Text style={styles.meta}>{exercise.sets} séries · {exercise.repetitions} reps · {exercise.restSeconds}s descanso</Text>{exercise.notes && <Text style={styles.copy}>{exercise.notes}</Text>}</Card>)}</View><Button onPress={() => router.push('/trainer/students')}>Aplicar a um aluno</Button></Screen>;
}
const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary }, list: { gap: spacing.sm }, exercise: { gap: spacing.xs }, name: { ...typography.headingMD, color: colors.textPrimary }, meta: { ...typography.bodyMD, color: colors.primary } });
