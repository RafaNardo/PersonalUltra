import { ScrollView, Pressable, StyleSheet, Text } from 'react-native';
import type { WorkoutTemplate } from '@/src/api/trainer-client';
import { colors, radius, spacing, typography } from '@/src/design/tokens';

export function templateGroups(templates: WorkoutTemplate[]) {
  return [...new Set(templates.flatMap((template) => template.muscleGroups ?? []))].sort((left, right) => left.localeCompare(right, 'pt-BR'));
}

export function filterTemplates(templates: WorkoutTemplate[], search: string, muscleGroup?: string) {
  const term = search.trim().toLocaleLowerCase('pt-BR');
  return templates.filter((template) => (!term || template.name.toLocaleLowerCase('pt-BR').includes(term)) && (!muscleGroup || template.muscleGroups?.includes(muscleGroup)));
}

export function TemplateMuscleFilters({ groups, selected, onSelect }: { groups: string[]; selected?: string; onSelect: (group?: string) => void }) {
  if (groups.length === 0) return null;
  return <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={styles.filters} accessibilityRole="tablist">
    <Filter label="Todos" selected={!selected} onPress={() => onSelect(undefined)} />
    {groups.map((group) => <Filter key={group} label={group} selected={selected === group} onPress={() => onSelect(group)} />)}
  </ScrollView>;
}

function Filter({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
  return <Pressable accessibilityRole="tab" accessibilityState={{ selected }} hitSlop={2} onPress={onPress} style={[styles.filter, selected && styles.filterSelected]}><Text style={[styles.filterText, selected && styles.filterTextSelected]}>{label}</Text></Pressable>;
}

const styles = StyleSheet.create({
  filters: { gap: spacing.xs, paddingRight: spacing.lg },
  filter: { minHeight: 40, justifyContent: 'center', paddingHorizontal: spacing.sm, borderRadius: radius.pill, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surface },
  filterSelected: { borderColor: colors.primary, backgroundColor: '#3A1D0C' },
  filterText: { ...typography.caption, color: colors.textSecondary },
  filterTextSelected: { color: colors.primary },
});
