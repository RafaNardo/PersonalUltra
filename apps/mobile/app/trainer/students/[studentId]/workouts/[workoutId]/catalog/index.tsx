import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { StyleSheet } from 'react-native';
import { ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { spacing } from '@/src/design/tokens';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { ExerciseCatalogBrowser, type ExerciseMuscleGroup } from '@/src/features/trainer/training/exercise-catalog-browser';
import { useTrainerExerciseCatalog, useTrainerStudentWorkout } from '@/src/features/trainer/training/hooks';

export default function TrainerExerciseCatalogScreen() {
  const { studentId, workoutId } = useLocalSearchParams<{ studentId: string; workoutId: string }>();
  const [search, setSearch] = useState('');
  const [muscleGroup, setMuscleGroup] = useState<ExerciseMuscleGroup>('Todos');
  const student = useTrainerStudent(studentId);
  const workout = useTrainerStudentWorkout(studentId, workoutId);
  const catalog = useTrainerExerciseCatalog(search, muscleGroup === 'Todos' ? undefined : muscleGroup);

  if (student.isLoading || workout.isLoading || (catalog.isLoading && !catalog.data)) return <LoadingView message="Abrindo o catálogo…" />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;
  if (workout.isError) return <ErrorView message={workout.error.message} onRetry={() => workout.refetch()} />;
  if (catalog.isError) return <ErrorView message={catalog.error.message} onRetry={() => catalog.refetch()} />;

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow={`${student.data!.firstName} ${student.data!.lastName} · ${workout.data!.name}`} title="Adicionar exercício" onBack={() => router.back()} />
    <ExerciseCatalogBrowser results={catalog.data ?? []} search={search} muscleGroup={muscleGroup} isFetching={catalog.isFetching} onSearchChange={setSearch} onMuscleGroupChange={setMuscleGroup} onSelect={(exercise) => router.push({ pathname: '/trainer/students/[studentId]/workouts/[workoutId]/catalog/[exerciseId]', params: { studentId: studentId!, workoutId: workoutId!, exerciseId: exercise.id } })} />
  </Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.md } });
