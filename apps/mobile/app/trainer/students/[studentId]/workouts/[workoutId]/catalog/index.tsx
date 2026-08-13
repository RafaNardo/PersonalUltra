import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Image, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { Card, EmptyState, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useTrainerExerciseCatalog, useTrainerStudentWorkout } from '@/src/features/trainer/training/hooks';
import { exerciseMediaSource } from '@/src/shared/training/exercise-media';

const muscleGroups = ['Todos', 'Peito', 'Costas', 'Ombros', 'Braços', 'Pernas', 'Glúteos'] as const;

export default function TrainerExerciseCatalogScreen() {
  const { studentId, workoutId } = useLocalSearchParams<{ studentId: string; workoutId: string }>();
  const [search, setSearch] = useState('');
  const [muscleGroup, setMuscleGroup] = useState<(typeof muscleGroups)[number]>('Todos');
  const student = useTrainerStudent(studentId);
  const workout = useTrainerStudentWorkout(studentId, workoutId);
  const catalog = useTrainerExerciseCatalog(search, muscleGroup === 'Todos' ? undefined : muscleGroup);

  if (student.isLoading || workout.isLoading || (catalog.isLoading && !catalog.data)) return <LoadingView message="Abrindo o catálogo…" />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;
  if (workout.isError) return <ErrorView message={workout.error.message} onRetry={() => workout.refetch()} />;
  if (catalog.isError) return <ErrorView message={catalog.error.message} onRetry={() => catalog.refetch()} />;

  const results = catalog.data ?? [];
  const clearFilters = () => { setSearch(''); setMuscleGroup('Todos'); };

  return <Screen style={styles.page}>
    <TopBar eyebrow={`${student.data!.firstName} ${student.data!.lastName} · ${workout.data!.name}`} title="Adicionar exercício" onBack={() => router.back()} />
    <View style={styles.searchShell}>
      <Text style={styles.searchIcon}>⌕</Text>
      <TextInput value={search} onChangeText={setSearch} maxLength={100} autoCapitalize="none" autoCorrect={false} returnKeyType="search" placeholder="Buscar exercício…" placeholderTextColor={colors.textMuted} accessibilityLabel="Buscar exercício no catálogo" accessibilityHint="A busca é feita pelo nome do exercício" style={styles.search} />
      {catalog.isFetching ? <ActivityIndicator accessibilityLabel="Atualizando resultados" color={colors.primary} size="small" /> : null}
    </View>
    <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.filters} accessibilityRole="tablist">
      {muscleGroups.map((group) => <Pressable key={group} accessibilityRole="tab" accessibilityState={{ selected: muscleGroup === group }} accessibilityLabel={`Filtrar por ${group}`} onPress={() => setMuscleGroup(group)} style={[styles.filter, muscleGroup === group && styles.filterSelected]}><Text style={[styles.filterText, muscleGroup === group && styles.filterTextSelected]}>{group}</Text></Pressable>)}
    </ScrollView>
    <View style={styles.resultHeader}><Text accessibilityLiveRegion="polite" style={styles.resultCount}>{results.length} {results.length === 1 ? 'exercício encontrado' : 'exercícios encontrados'}</Text><Text style={styles.resultHint}>Toque para configurar</Text></View>
    {results.length === 0 ? <EmptyState title="Nenhum exercício encontrado" message="Tente outro termo ou remova o filtro de grupo muscular." actionLabel="Limpar filtros" onAction={clearFilters} /> : <View style={styles.grid}>
      {results.map((exercise) => {
        const source = exerciseMediaSource(exercise.imageRef);
        return <Pressable key={exercise.id} accessibilityRole="button" accessibilityLabel={`${exercise.name}, ${exercise.primaryMuscleGroup}${exercise.equipment ? `, ${exercise.equipment}` : ''}`} accessibilityHint="Abre a configuração de séries, repetições, descanso e notas" onPress={() => router.push({ pathname: '/trainer/students/[studentId]/workouts/[workoutId]/catalog/[exerciseId]', params: { studentId: studentId!, workoutId: workoutId!, exerciseId: exercise.id } })} style={({ pressed }) => [styles.tile, pressed && styles.pressed]}>
          <Card style={styles.exerciseCard}>
            {source ? <Image source={source} accessible={false} resizeMode="cover" style={styles.thumbnail} /> : <View style={styles.thumbnailFallback}><Text style={styles.thumbnailFallbackText}>PU</Text></View>}
            <View style={styles.exerciseCopy}><Text numberOfLines={2} style={styles.exerciseName}>{exercise.name}</Text><Text numberOfLines={1} style={styles.exerciseMeta}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text></View>
          </Card>
        </Pressable>;
      })}
    </View>}
    <Card style={styles.info}><Text style={styles.infoTitle}>Catálogo curado</Text><Text style={styles.infoCopy}>Os exercícios são gerenciados pelo Personal Ultra. A criação por texto livre não faz parte deste fluxo.</Text></Card>
  </Screen>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.md }, searchShell: { minHeight: 52, flexDirection: 'row', alignItems: 'center', gap: spacing.sm, paddingHorizontal: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, backgroundColor: colors.surface }, searchIcon: { fontSize: 24, color: colors.textMuted }, search: { ...typography.bodyMD, color: colors.textPrimary, flex: 1, minHeight: 50 }, filters: { gap: spacing.xs, paddingRight: spacing.lg }, filter: { minHeight: 44, justifyContent: 'center', paddingHorizontal: spacing.md, borderRadius: radius.pill, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface }, filterSelected: { borderColor: colors.primary, backgroundColor: '#3A1D0C' }, filterText: { ...typography.caption, color: colors.textSecondary }, filterTextSelected: { color: colors.primary }, resultHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: spacing.sm }, resultCount: { ...typography.caption, color: colors.titanium }, resultHint: { ...typography.caption, color: colors.textMuted }, grid: { flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'space-between', gap: spacing.sm }, tile: { width: '48%' }, pressed: { opacity: .76, transform: [{ scale: .985 }] }, exerciseCard: { padding: 0, overflow: 'hidden', gap: 0 }, thumbnail: { width: '100%', aspectRatio: 1.22, backgroundColor: colors.surfaceElevated }, thumbnailFallback: { width: '100%', aspectRatio: 1.22, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.surfaceElevated }, thumbnailFallbackText: { ...typography.headingMD, color: colors.textMuted }, exerciseCopy: { minHeight: 88, padding: spacing.sm, gap: spacing.xxs }, exerciseName: { ...typography.bodyMD, color: colors.textPrimary, fontFamily: 'MontserratSemiBold' }, exerciseMeta: { ...typography.caption, color: colors.textMuted }, info: { gap: spacing.xs, backgroundColor: colors.surfaceElevated }, infoTitle: { ...typography.caption, color: colors.titaniumLight }, infoCopy: { ...typography.bodyMD, color: colors.textSecondary },
});
