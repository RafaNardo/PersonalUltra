import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import type { TrainerExerciseCatalogItem } from '@/src/api/trainer-client';
import { Card, EmptyState } from '@/src/components/ui';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { ExerciseImage } from '@/src/shared/training/exercise-image';

export const exerciseMuscleGroups = ['Todos', 'Bíceps', 'Cardio', 'Core', 'Corpo inteiro', 'Costas', 'Glúteos', 'Ombros', 'Panturrilhas', 'Peito', 'Posteriores da coxa', 'Quadríceps', 'Tríceps'] as const;
export type ExerciseMuscleGroup = (typeof exerciseMuscleGroups)[number];

export function ExerciseCatalogBrowser({ results, search, muscleGroup, isFetching, onSearchChange, onMuscleGroupChange, onSelect }: {
  results: TrainerExerciseCatalogItem[];
  search: string;
  muscleGroup: ExerciseMuscleGroup;
  isFetching: boolean;
  onSearchChange: (value: string) => void;
  onMuscleGroupChange: (value: ExerciseMuscleGroup) => void;
  onSelect: (exercise: TrainerExerciseCatalogItem) => void;
}) {
  const clearFilters = () => { onSearchChange(''); onMuscleGroupChange('Todos'); };
  return <>
    <View style={styles.searchShell}>
      <Text style={styles.searchIcon}>⌕</Text>
      <TextInput value={search} onChangeText={onSearchChange} maxLength={100} autoCapitalize="none" autoCorrect={false} returnKeyType="search" placeholder="Buscar exercício…" placeholderTextColor={colors.textMuted} accessibilityLabel="Buscar exercício no catálogo" accessibilityHint="A busca é feita pelo nome do exercício" style={styles.search} />
      {isFetching ? <ActivityIndicator accessibilityLabel="Atualizando resultados" color={colors.primary} size="small" /> : null}
    </View>
    <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.scroller} contentContainerStyle={styles.filters} accessibilityRole="tablist">
      {exerciseMuscleGroups.map((group) => <Pressable key={group} accessibilityRole="tab" accessibilityState={{ selected: muscleGroup === group }} accessibilityLabel={`Filtrar por ${group}`} onPress={() => onMuscleGroupChange(group)} style={[styles.filter, muscleGroup === group && styles.filterSelected]}><Text style={[styles.filterText, muscleGroup === group && styles.filterTextSelected]}>{group}</Text></Pressable>)}
    </ScrollView>
    <View style={styles.resultHeader}><Text accessibilityLiveRegion="polite" style={styles.resultCount}>{results.length} {results.length === 1 ? 'exercício encontrado' : 'exercícios encontrados'}</Text><Text style={styles.resultHint}>Toque para configurar</Text></View>
    {results.length === 0 ? <EmptyState status="BUSCA SEM RESULTADO" symbol="⌕" title="Nenhum exercício corresponde aos filtros." message="Tente outro termo ou remova o filtro de grupo muscular." actionLabel="Limpar filtros" onAction={clearFilters} /> : <View style={styles.grid}>
      {results.map((exercise) => {
        return <Pressable key={exercise.id} accessibilityRole="button" accessibilityLabel={`${exercise.name}, ${exercise.primaryMuscleGroup}${exercise.equipment ? `, ${exercise.equipment}` : ''}`} accessibilityHint="Abre a configuração de séries, repetições, descanso e notas" onPress={() => onSelect(exercise)} style={({ pressed }) => [styles.tile, pressed && styles.pressed]}>
          <Card style={styles.exerciseCard}>
            <ExerciseImage imageRef={exercise.imageRef} imageUrl={exercise.imageUrl} contentFit="contain" accessible={false} style={styles.thumbnail} />
            <View style={styles.exerciseCopy}><Text numberOfLines={2} style={styles.exerciseName}>{exercise.name}</Text><Text numberOfLines={1} style={styles.exerciseMeta}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text></View>
          </Card>
        </Pressable>;
      })}
    </View>}
    <Card style={styles.info}><Text style={styles.infoTitle}>Catálogo curado</Text><Text style={styles.infoCopy}>Os exercícios são gerenciados pelo Personal Ultra. A criação por texto livre não faz parte deste fluxo.</Text></Card>
  </>;
}

const styles = StyleSheet.create({
  searchShell: { minHeight: 52, flexDirection: 'row', alignItems: 'center', gap: spacing.sm, paddingHorizontal: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, backgroundColor: colors.surface }, searchIcon: { fontSize: 24, color: colors.textMuted }, search: { ...typography.bodyMD, color: colors.textPrimary, flex: 1, minHeight: 50 }, scroller: { flexGrow: 0 }, filters: { gap: spacing.xs, paddingRight: spacing.lg }, filter: { minWidth: 76, height: 40, paddingHorizontal: spacing.md, alignItems: 'center', justifyContent: 'center', borderRadius: radius.pill, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface }, filterSelected: { borderColor: colors.primary, backgroundColor: '#3A1D0C' }, filterText: { ...typography.caption, color: colors.textSecondary, textAlign: 'center' }, filterTextSelected: { color: colors.primary }, resultHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', gap: spacing.sm }, resultCount: { ...typography.caption, color: colors.titanium }, resultHint: { ...typography.caption, color: colors.textMuted }, grid: { flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'space-between', gap: spacing.sm }, tile: { width: '48%' }, pressed: { opacity: .76, transform: [{ scale: .985 }] }, exerciseCard: { padding: 0, overflow: 'hidden', gap: 0 }, thumbnail: { width: '100%', height: 112, backgroundColor: colors.surfaceElevated }, exerciseCopy: { minHeight: 82, padding: spacing.sm, gap: spacing.xxs }, exerciseName: { ...typography.bodyMD, color: colors.textPrimary, fontFamily: 'MontserratSemiBold' }, exerciseMeta: { ...typography.caption, color: colors.textMuted }, info: { gap: spacing.xs, backgroundColor: colors.surfaceElevated }, infoTitle: { ...typography.caption, color: colors.titaniumLight }, infoCopy: { ...typography.bodyMD, color: colors.textSecondary },
});
