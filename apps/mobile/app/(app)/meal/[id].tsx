import { router, useLocalSearchParams } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, Pressable, StyleSheet, Text, View } from 'react-native';
import { useState } from 'react';
import type { FoodAlternative, MealFood } from '@/src/api/types';
import { api } from '@/src/api/client';
import { useAuthStore } from '@/src/state/auth-store';
import { Button, Card, EmptyState, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { feedback } from '@/src/platform/feedback';
import { telemetry } from '@/src/platform/telemetry';

export default function MealScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const token = useAuthStore((state) => state.accessToken)!;
  const client = useQueryClient();
  const [selectedFood, setSelectedFood] = useState<MealFood | null>(null);
  const [alternatives, setAlternatives] = useState<FoodAlternative[]>([]);
  const [isLoadingAlternatives, setIsLoadingAlternatives] = useState(false);
  const [alternativesError, setAlternativesError] = useState<string | null>(null);
  const [substitutionError, setSubstitutionError] = useState<string | null>(null);

  const meal = useQuery({ queryKey: ['meal', id], queryFn: () => api.meal(token, id), enabled: Boolean(token && id) });
  const complete = useMutation({
    mutationFn: () => api.completeMeal(token, id),
    onSuccess: () => {
      feedback.success();
      telemetry.event('meal_completed');
      void client.invalidateQueries({ queryKey: ['nutrition', 'today'] });
      void client.invalidateQueries({ queryKey: ['meal', id] });
    },
    onError: () => Alert.alert('Não foi possível concluir esta refeição.', 'Tente novamente em instantes.'),
  });
  const substitute = useMutation({
    mutationFn: ({ foodId, replacementId }: { foodId: string; replacementId: string }) => api.substituteFood(token, id, foodId, replacementId),
    onSuccess: () => {
      feedback.success();
      telemetry.event('food_substitution_completed');
      closeAlternatives();
      void client.invalidateQueries({ queryKey: ['meal', id] });
      void client.invalidateQueries({ queryKey: ['nutrition', 'today'] });
    },
    onError: () => setSubstitutionError('Não foi possível aplicar a troca. Sua refeição não foi alterada.'),
  });

  function closeAlternatives() {
    setSelectedFood(null);
    setAlternatives([]);
    setAlternativesError(null);
    setSubstitutionError(null);
  }

  async function loadAlternatives(food: MealFood) {
    setSelectedFood(food);
    setAlternatives([]);
    setAlternativesError(null);
    setSubstitutionError(null);
    setIsLoadingAlternatives(true);
    try {
      const options = await api.foodAlternatives(token, id, food.foodId);
      setAlternatives(options);
      if (!options.length) setAlternativesError('Não há alternativas equivalentes aprovadas para este alimento.');
    } catch {
      setAlternativesError('Não foi possível buscar alternativas agora.');
    } finally {
      setIsLoadingAlternatives(false);
    }
  }

  if (meal.isLoading) return <LoadingView />;
  if (meal.error || !meal.data) {
    return <Screen><TopBar eyebrow="Nutrição" title="Refeição" onBack={() => router.replace('/(app)/nutrition')} /><ErrorView message="Não foi possível carregar esta refeição." onRetry={() => void meal.refetch()} /></Screen>;
  }

  const calories = meal.data.foods.reduce((total, food) => total + food.calories, 0);
  const protein = meal.data.foods.reduce((total, food) => total + food.protein, 0);
  const carbs = meal.data.foods.reduce((total, food) => total + food.carbs, 0);
  const fat = meal.data.foods.reduce((total, food) => total + food.fat, 0);

  return <Screen><TopBar eyebrow="Nutrição" title={meal.data.name} onBack={() => router.replace('/(app)/nutrition')} />
    <Card style={styles.summary}><View><Text style={styles.summaryLabel}>ESTA REFEIÇÃO</Text><Text style={styles.summaryValue}>{calories} kcal</Text></View><View style={styles.summaryMacros}><Text style={styles.summaryMacro}>P {protein.toFixed(0)}g</Text><Text style={styles.summaryMacro}>C {carbs.toFixed(0)}g</Text><Text style={styles.summaryMacro}>G {fat.toFixed(0)}g</Text></View></Card>
    <Text style={styles.help}>Escolha um alimento para ver substituições equivalentes.</Text>
    {meal.data.foods.length > 0 ? meal.data.foods.map((food, index) => <View key={food.id} style={styles.foodGroup}>
      <Card style={styles.food}><View style={styles.foodIndex}><Text style={styles.foodIndexText}>{index + 1}</Text></View><View style={styles.foodBody}><Text style={styles.name}>{food.name}</Text><Text style={styles.copy}>{food.quantityGrams}g · {food.calories} kcal · P {food.protein}g</Text></View><Pressable accessibilityRole="button" accessibilityLabel={`Trocar ${food.name}`} accessibilityHint="Abre as alternativas equivalentes" accessibilityState={{ disabled: substitute.isPending }} disabled={substitute.isPending} hitSlop={12} onPress={() => void loadAlternatives(food)}><Text style={[styles.link, substitute.isPending && styles.linkDisabled]}>Trocar</Text></Pressable></Card>
      {selectedFood?.id === food.id && <AlternativesPanel
        food={selectedFood}
        alternatives={alternatives}
        loading={isLoadingAlternatives}
        error={alternativesError ?? substitutionError}
        applying={substitute.isPending}
        onCancel={closeAlternatives}
        onRetry={() => void loadAlternatives(selectedFood)}
        onSelect={(replacementId) => { setSubstitutionError(null); substitute.mutate({ foodId: selectedFood.foodId, replacementId }); }}
      />}
    </View>) : <EmptyState title="Nenhum alimento nesta refeição" message="Seu Coach ainda não definiu os itens desta refeição." actionLabel="Atualizar" onAction={() => void meal.refetch()} />}
    <Button loading={complete.isPending} disabled={meal.data.completed || meal.data.foods.length === 0} onPress={() => void complete.mutate()}>{meal.data.completed ? 'Refeição concluída' : 'Concluir refeição'}</Button>
  </Screen>;
}

function AlternativesPanel({ food, alternatives, loading, error, applying, onCancel, onRetry, onSelect }: { food: MealFood; alternatives: FoodAlternative[]; loading: boolean; error: string | null; applying: boolean; onCancel: () => void; onRetry: () => void; onSelect: (replacementId: string) => void }) {
  return <View accessibilityLiveRegion="polite"><Card style={styles.alternatives}>
    <View><Text style={styles.alternativesLabel}>TROCAR ALIMENTO</Text><Text style={styles.alternativesTitle}>Alternativas para {food.name}</Text><Text style={styles.alternativesCopy}>As quantidades sugeridas preservam a equivalência calórica.</Text></View>
    {loading && <Text style={styles.status}>Buscando alternativas aprovadas…</Text>}
    {!loading && error && <View style={styles.errorGroup}><Text accessibilityRole="alert" style={styles.error}>{error}</Text><Button variant="secondary" onPress={onRetry}>Tentar novamente</Button></View>}
    {!loading && !error && alternatives.map((alternative) => <Pressable key={alternative.foodId} accessibilityRole="button" accessibilityLabel={`Trocar por ${alternative.name}, ${alternative.suggestedQuantityGrams} gramas`} accessibilityState={{ disabled: applying }} disabled={applying} onPress={() => onSelect(alternative.foodId)} style={({ pressed }) => [styles.alternativeOption, pressed && !applying && styles.alternativeOptionPressed, applying && styles.optionDisabled]}><View><Text style={styles.alternativeName}>{alternative.name}</Text><Text style={styles.alternativeDetails}>{alternative.suggestedQuantityGrams}g · equivalente à sua porção atual</Text></View><Text style={styles.optionAction}>{applying ? 'Aplicando…' : 'Escolher'}</Text></Pressable>)}
    <Pressable accessibilityRole="button" accessibilityLabel="Cancelar troca de alimento" disabled={applying} onPress={onCancel} style={({ pressed }) => [styles.cancel, pressed && !applying && styles.cancelPressed]}><Text style={styles.cancelText}>Cancelar</Text></Pressable>
  </Card></View>;
}

const styles = StyleSheet.create({
  summary: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }, summaryLabel: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, summaryValue: { ...typography.headingLG, color: colors.textPrimary, marginTop: spacing.xs }, summaryMacros: { alignItems: 'flex-end', gap: spacing.xxs }, summaryMacro: { ...typography.bodyMD, color: colors.textSecondary }, help: { ...typography.bodyMD, color: colors.textSecondary, marginTop: -spacing.xs }, foodGroup: { gap: spacing.sm }, food: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm }, foodIndex: { width: 28, height: 28, borderRadius: 14, backgroundColor: colors.surfaceElevated, alignItems: 'center', justifyContent: 'center' }, foodIndexText: { ...typography.caption, color: colors.primary }, foodBody: { flex: 1 }, name: { ...typography.headingMD, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, marginTop: spacing.xs }, link: { ...typography.bodyMD, color: colors.primary, fontFamily: 'MontserratBold' }, linkDisabled: { color: colors.textMuted }, alternatives: { gap: spacing.md, borderColor: colors.primary, borderWidth: 1 }, alternativesLabel: { ...typography.caption, color: colors.primary, letterSpacing: .8 }, alternativesTitle: { ...typography.headingMD, color: colors.textPrimary, marginTop: spacing.xxs }, alternativesCopy: { ...typography.bodyMD, color: colors.textSecondary, marginTop: spacing.xs }, status: { ...typography.bodyMD, color: colors.textSecondary }, alternativeOption: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: spacing.sm, padding: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, alternativeOptionPressed: { opacity: .8, borderColor: colors.primary }, optionDisabled: { opacity: .55 }, alternativeName: { ...typography.bodyLG, color: colors.textPrimary, fontFamily: 'MontserratSemiBold' }, alternativeDetails: { ...typography.caption, color: colors.textSecondary, marginTop: spacing.xxs }, optionAction: { ...typography.caption, color: colors.primary, fontFamily: 'MontserratBold' }, errorGroup: { gap: spacing.sm }, error: { ...typography.bodyMD, color: colors.danger }, cancel: { alignItems: 'center', paddingVertical: spacing.sm, borderRadius: radius.sm, borderWidth: 1, borderColor: colors.border }, cancelPressed: { opacity: .7 }, cancelText: { ...typography.bodyMD, color: colors.textSecondary, fontFamily: 'MontserratSemiBold' }, empty: { gap: spacing.sm }, emptyTitle: { ...typography.headingMD, color: colors.textPrimary }, emptyCopy: { ...typography.bodyMD, color: colors.textSecondary },
});
