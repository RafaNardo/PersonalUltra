import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useRef, useState } from 'react';
import { Alert, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import type { NutritionQuantityUnit, TrainerNutrition, TrainerNutritionInput } from '@/src/api/trainer-client';
import { useTrainerStudent } from '@/src/features/trainer/students/hooks';
import { useSaveTrainerNutrition, useTrainerNutrition } from './hooks';

const units: NutritionQuantityUnit[] = ['g', 'ml', 'unidade', 'fatia', 'colher', 'dose', 'porção'];
let nextLocalId = 0;
const localId = (prefix: string) => `${prefix}-${Date.now()}-${nextLocalId++}`;

type FoodDraft = { clientId: string; id?: string; foodName: string; quantity: string; unit: NutritionQuantityUnit };
type MealDraft = { clientId: string; id?: string; name: string; notes: string; foods: FoodDraft[] };
type NutritionDraft = { name: string; notes: string; meals: MealDraft[] };

function emptyFood(): FoodDraft {
  return { clientId: localId('food'), foodName: '', quantity: '', unit: 'g' };
}

function emptyMeal(): MealDraft {
  return { clientId: localId('meal'), name: '', notes: '', foods: [emptyFood()] };
}

function createDraft(nutrition: TrainerNutrition | null): NutritionDraft {
  if (!nutrition) return { name: '', notes: '', meals: [emptyMeal()] };
  return {
    name: nutrition.name,
    notes: nutrition.notes,
    meals: [...nutrition.meals].sort((left, right) => left.sequence - right.sequence).map((meal) => ({
      clientId: localId('meal'),
      id: meal.id,
      name: meal.name,
      notes: meal.notes,
      foods: [...meal.foods].sort((left, right) => left.sequence - right.sequence).map((food) => ({ clientId: localId('food'), id: food.id, foodName: food.foodName, quantity: String(food.quantity), unit: food.unit })),
    })),
  };
}

function parseQuantity(value: string) {
  return Number(value.replace(',', '.'));
}

function validate(draft: NutritionDraft) {
  const errors: string[] = [];
  if (!draft.name.trim()) errors.push('Informe o nome do plano.');
  if (draft.name.length > 200) errors.push('O nome do plano deve ter até 200 caracteres.');
  if (draft.notes.length > 2000) errors.push('As orientações gerais devem ter até 2000 caracteres.');
  if (draft.meals.length === 0) errors.push('Adicione ao menos uma refeição.');
  if (draft.meals.length > 20) errors.push('O plano pode ter no máximo 20 refeições.');
  draft.meals.forEach((meal, mealIndex) => {
    if (!meal.name.trim()) errors.push(`Informe o nome da refeição ${mealIndex + 1}.`);
    if (meal.name.length > 200) errors.push(`O nome da refeição ${mealIndex + 1} deve ter até 200 caracteres.`);
    if (meal.notes.length > 1000) errors.push(`As observações da refeição ${mealIndex + 1} devem ter até 1000 caracteres.`);
    if (meal.foods.length === 0) errors.push(`Adicione ao menos um item em ${meal.name.trim() || `refeição ${mealIndex + 1}`}.`);
    if (meal.foods.length > 30) errors.push(`${meal.name.trim() || `Refeição ${mealIndex + 1}`} pode ter no máximo 30 itens.`);
    meal.foods.forEach((food, foodIndex) => {
      const context = `${meal.name.trim() || `refeição ${mealIndex + 1}`}, item ${foodIndex + 1}`;
      if (!food.foodName.trim()) errors.push(`Informe o alimento de ${context}.`);
      if (food.foodName.length > 200) errors.push(`O nome do alimento em ${context} deve ter até 200 caracteres.`);
      if (!Number.isFinite(parseQuantity(food.quantity)) || parseQuantity(food.quantity) <= 0 || parseQuantity(food.quantity) > 10000) errors.push(`Informe uma quantidade maior que zero e até 10000 em ${context}.`);
    });
  });
  return errors;
}

function toInput(draft: NutritionDraft): TrainerNutritionInput {
  return {
    name: draft.name.trim(),
    notes: draft.notes.trim(),
    meals: draft.meals.map((meal, index) => ({
      name: meal.name.trim(),
      sequence: index + 1,
      notes: meal.notes.trim(),
      foods: meal.foods.map((food, foodIndex) => ({ foodName: food.foodName.trim(), quantity: parseQuantity(food.quantity), unit: food.unit, sequence: foodIndex + 1 })),
    })),
  };
}

export function TrainerNutritionScreen() {
  const { id = '' } = useLocalSearchParams<{ id: string }>();
  const nutrition = useTrainerNutrition(id);
  const student = useTrainerStudent(id);
  const save = useSaveTrainerNutrition(id);
  const hydrated = useRef(false);
  const initialSignature = useRef('');
  const [draft, setDraft] = useState<NutritionDraft>({ name: '', notes: '', meals: [] });
  const [validationErrors, setValidationErrors] = useState<string[]>([]);

  useEffect(() => {
    if (!hydrated.current && nutrition.isSuccess) {
      const initialDraft = createDraft(nutrition.data);
      setDraft(initialDraft);
      initialSignature.current = JSON.stringify(toInput(initialDraft));
      hydrated.current = true;
    }
  }, [nutrition.data, nutrition.isSuccess]);

  if (nutrition.isError) return <ErrorView message={nutrition.error.message} onRetry={() => nutrition.refetch()} />;
  if (student.isError) return <ErrorView message={student.error.message} onRetry={() => student.refetch()} />;
  if (nutrition.isLoading || student.isLoading || !hydrated.current) return <LoadingView message="Abrindo alimentação…" />;

  const studentName = `${student.data!.firstName} ${student.data!.lastName}`.trim();
  const isDirty = JSON.stringify(toInput(draft)) !== initialSignature.current;
  const goBack = () => {
    if (!isDirty) return router.back();
    Alert.alert('Descartar alterações?', 'O plano continuará como estava antes desta edição.', [
      { text: 'Continuar editando', style: 'cancel' },
      { text: 'Descartar', style: 'destructive', onPress: () => router.back() },
    ]);
  };
  const updateMeal = (mealIndex: number, update: (meal: MealDraft) => MealDraft) => setDraft((current) => ({ ...current, meals: current.meals.map((meal, index) => index === mealIndex ? update(meal) : meal) }));
  const moveMeal = (from: number, to: number) => setDraft((current) => {
    const meals = [...current.meals];
    const [moved] = meals.splice(from, 1);
    meals.splice(to, 0, moved);
    return { ...current, meals };
  });
  const moveFood = (mealIndex: number, from: number, to: number) => updateMeal(mealIndex, (meal) => {
    const foods = [...meal.foods];
    const [moved] = foods.splice(from, 1);
    foods.splice(to, 0, moved);
    return { ...meal, foods };
  });
  const requestSave = () => {
    const errors = validate(draft);
    setValidationErrors(errors);
    if (errors.length) return;
    const updating = Boolean(nutrition.data);
    Alert.alert(
      updating ? 'Atualizar plano?' : 'Salvar e disponibilizar?',
      `${student.data!.firstName} verá este plano no aplicativo assim que você salvar.`,
      [
        { text: 'Cancelar', style: 'cancel' },
        { text: updating ? 'Atualizar' : 'Salvar', onPress: () => save.mutate(toInput(draft), { onSuccess: () => Alert.alert('Plano salvo', `${student.data!.firstName} já pode consultar a alimentação atualizada.`, [{ text: 'Voltar ao aluno', onPress: () => router.back() }]) }) },
      ],
    );
  };

  return <Screen withinTabs style={styles.page}>
    <TopBar eyebrow="ALIMENTAÇÃO" title={studentName} onBack={goBack} />
    <Text style={styles.copy}>Monte o plano completo. Ao salvar, ele fica disponível para o aluno no aplicativo.</Text>

    <Card style={styles.card}>
      <Text style={styles.sectionTitle}>Plano alimentar</Text>
      <Field label="Nome do plano" value={draft.name} onChange={(name) => setDraft((current) => ({ ...current, name }))} placeholder="Ex.: Alimentação para rotina de treinos" maxLength={200} />
      <Field label="Orientações gerais (opcional)" value={draft.notes} onChange={(notes) => setDraft((current) => ({ ...current, notes }))} placeholder="Contexto e orientações para o aluno" multiline maxLength={2000} />
    </Card>

    {draft.meals.map((meal, mealIndex) => <Card key={meal.clientId} style={styles.card}>
      <View style={styles.cardHeader}>
        <View style={styles.headingGroup}><Text style={styles.eyebrow}>REFEIÇÃO {mealIndex + 1}</Text><Text style={styles.sectionTitle}>{meal.name.trim() || 'Nova refeição'}</Text></View>
        <View style={styles.actions}>
          <SmallAction label="↑" accessibilityLabel={`Mover refeição ${mealIndex + 1} para cima`} disabled={mealIndex === 0} onPress={() => moveMeal(mealIndex, mealIndex - 1)} />
          <SmallAction label="↓" accessibilityLabel={`Mover refeição ${mealIndex + 1} para baixo`} disabled={mealIndex === draft.meals.length - 1} onPress={() => moveMeal(mealIndex, mealIndex + 1)} />
          <SmallAction label="Remover" accessibilityLabel={`Remover refeição ${mealIndex + 1}`} tone="danger" onPress={() => setDraft((current) => ({ ...current, meals: current.meals.filter((_, index) => index !== mealIndex) }))} />
        </View>
      </View>
      <Field label="Nome da refeição" value={meal.name} onChange={(name) => updateMeal(mealIndex, (current) => ({ ...current, name }))} placeholder="Ex.: Café da manhã" maxLength={200} />
      <Field label="Observações (opcional)" value={meal.notes} onChange={(notes) => updateMeal(mealIndex, (current) => ({ ...current, notes }))} placeholder="Horário, preparo ou orientação" multiline maxLength={1000} />

      {meal.foods.map((food, foodIndex) => <View key={food.clientId} style={styles.foodCard}>
        <View style={styles.cardHeader}>
          <Text style={styles.itemTitle}>Item {foodIndex + 1}</Text>
          <View style={styles.actions}>
            <SmallAction label="↑" accessibilityLabel={`Mover item ${foodIndex + 1} para cima`} disabled={foodIndex === 0} onPress={() => moveFood(mealIndex, foodIndex, foodIndex - 1)} />
            <SmallAction label="↓" accessibilityLabel={`Mover item ${foodIndex + 1} para baixo`} disabled={foodIndex === meal.foods.length - 1} onPress={() => moveFood(mealIndex, foodIndex, foodIndex + 1)} />
            <SmallAction label="Remover" accessibilityLabel={`Remover item ${foodIndex + 1}`} tone="danger" onPress={() => updateMeal(mealIndex, (current) => ({ ...current, foods: current.foods.filter((_, index) => index !== foodIndex) }))} />
          </View>
        </View>
        <Field label="Alimento" value={food.foodName} onChange={(foodName) => updateMeal(mealIndex, (current) => ({ ...current, foods: current.foods.map((item, index) => index === foodIndex ? { ...item, foodName } : item) }))} placeholder="Ex.: Arroz integral" maxLength={200} />
        <Field label="Quantidade" value={food.quantity} onChange={(quantity) => updateMeal(mealIndex, (current) => ({ ...current, foods: current.foods.map((item, index) => index === foodIndex ? { ...item, quantity } : item) }))} placeholder="Ex.: 120" keyboardType="decimal-pad" />
        <Text style={styles.label}>Unidade</Text>
        <View style={styles.unitList}>{units.map((unit) => <Pressable key={unit} accessibilityRole="button" accessibilityState={{ selected: food.unit === unit }} onPress={() => updateMeal(mealIndex, (current) => ({ ...current, foods: current.foods.map((item, index) => index === foodIndex ? { ...item, unit } : item) }))} style={[styles.unit, food.unit === unit && styles.unitSelected]}><Text style={[styles.unitText, food.unit === unit && styles.unitTextSelected]}>{unit}</Text></Pressable>)}</View>
      </View>)}
      <Button variant="secondary" disabled={meal.foods.length >= 30} onPress={() => updateMeal(mealIndex, (current) => ({ ...current, foods: [...current.foods, emptyFood()] }))}>+ Adicionar item</Button>
    </Card>)}

    <Button variant="secondary" disabled={draft.meals.length >= 20} onPress={() => setDraft((current) => ({ ...current, meals: [...current.meals, emptyMeal()] }))}>+ Adicionar refeição</Button>

    {validationErrors.length ? <Card style={styles.errorCard}><Text accessibilityRole="alert" style={styles.errorTitle}>Revise antes de salvar</Text>{validationErrors.map((error) => <Text key={error} style={styles.errorText}>• {error}</Text>)}</Card> : null}
    {save.isError ? <Card style={styles.errorCard}><Text accessibilityRole="alert" style={styles.errorTitle}>Não foi possível salvar</Text><Text style={styles.errorText}>{save.error.message}</Text></Card> : null}
    <Button loading={save.isPending} onPress={requestSave}>{nutrition.data ? 'Atualizar plano' : 'Salvar e disponibilizar'}</Button>
    <Text style={styles.footer}>O aluno verá a versão salva mais recente. Você pode voltar e atualizar quando precisar.</Text>
  </Screen>;
}

function Field({ label, value, onChange, placeholder, multiline = false, keyboardType, maxLength }: { label: string; value: string; onChange: (value: string) => void; placeholder: string; multiline?: boolean; keyboardType?: 'decimal-pad'; maxLength?: number }) {
  return <View style={styles.field}><Text style={styles.label}>{label}</Text><TextInput value={value} onChangeText={onChange} multiline={multiline} keyboardType={keyboardType} maxLength={maxLength} placeholder={placeholder} placeholderTextColor={colors.textMuted} accessibilityLabel={label} style={[styles.input, multiline && styles.inputMultiline]} /></View>;
}

function SmallAction({ label, accessibilityLabel, onPress, disabled = false, tone = 'default' }: { label: string; accessibilityLabel: string; onPress: () => void; disabled?: boolean; tone?: 'default' | 'danger' }) {
  return <Pressable disabled={disabled} accessibilityRole="button" accessibilityLabel={accessibilityLabel} accessibilityState={{ disabled }} onPress={onPress} style={[styles.smallAction, disabled && styles.disabled]}><Text style={[styles.smallActionText, tone === 'danger' && styles.dangerText]}>{label}</Text></Pressable>;
}

const styles = StyleSheet.create({
  page: { paddingVertical: spacing.xl, gap: spacing.lg },
  card: { gap: spacing.md },
  cardHeader: { flexDirection: 'row', alignItems: 'flex-start', justifyContent: 'space-between', gap: spacing.sm },
  headingGroup: { flex: 1, gap: spacing.xxs },
  sectionTitle: { ...typography.headingMD, color: colors.textPrimary },
  itemTitle: { ...typography.bodyLG, color: colors.textPrimary, fontWeight: '700' },
  eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 },
  copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 },
  field: { gap: spacing.xs },
  label: { ...typography.caption, color: colors.textSecondary },
  input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 50, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, paddingHorizontal: spacing.md, paddingVertical: spacing.sm, backgroundColor: colors.background },
  inputMultiline: { minHeight: 88, textAlignVertical: 'top' },
  foodCard: { gap: spacing.sm, padding: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, backgroundColor: colors.surfaceElevated },
  actions: { flexDirection: 'row', alignItems: 'center', flexWrap: 'wrap', justifyContent: 'flex-end', gap: spacing.xs },
  smallAction: { minHeight: 36, minWidth: 36, alignItems: 'center', justifyContent: 'center', paddingHorizontal: spacing.sm, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated },
  smallActionText: { ...typography.caption, color: colors.textPrimary },
  dangerText: { color: colors.danger },
  disabled: { opacity: .3 },
  unitList: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs },
  unit: { minHeight: 40, justifyContent: 'center', paddingHorizontal: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.pill, backgroundColor: colors.background },
  unitSelected: { borderColor: colors.primary, backgroundColor: colors.surface },
  unitText: { ...typography.caption, color: colors.textSecondary },
  unitTextSelected: { color: colors.primary },
  errorCard: { gap: spacing.xs, borderColor: colors.danger },
  errorTitle: { ...typography.headingMD, color: colors.danger },
  errorText: { ...typography.bodyMD, color: colors.textSecondary },
  footer: { ...typography.caption, color: colors.textMuted, textAlign: 'center', lineHeight: 18 },
});
