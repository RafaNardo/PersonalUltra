import { useEffect, useMemo, useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card } from '@/src/components/ui';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import type { NutritionMealTemplate, NutritionQuantityUnit, TrainerNutritionInput, TrainerNutritionMeal } from '@/src/api/trainer-client';

const units: NutritionQuantityUnit[] = ['g', 'ml', 'unidade', 'fatia', 'colher', 'dose', 'porção'];
let counter = 0;
const localId = (prefix: string) => `${prefix}-${Date.now()}-${counter++}`;
type FoodDraft = { clientId: string; foodName: string; quantity: string; unit: NutritionQuantityUnit };
type MealDraft = { clientId: string; name: string; notes: string; foods: FoodDraft[] };
export type NutritionDraft = { name: string; notes: string; meals: MealDraft[] };

const emptyFood = (): FoodDraft => ({ clientId: localId('food'), foodName: '', quantity: '', unit: 'g' });
const emptyMeal = (): MealDraft => ({ clientId: localId('meal'), name: '', notes: '', foods: [emptyFood()] });
export function nutritionDraft(value?: { name: string; notes: string; meals?: TrainerNutritionMeal[] } | null): NutritionDraft {
  if (!value) return { name: '', notes: '', meals: [emptyMeal()] };
  return { name: value.name, notes: value.notes, meals: [...(value.meals ?? [])].sort((a, b) => a.sequence - b.sequence).map((meal) => ({ clientId: localId('meal'), name: meal.name, notes: meal.notes, foods: [...meal.foods].sort((a, b) => a.sequence - b.sequence).map((food) => ({ clientId: localId('food'), foodName: food.foodName, quantity: String(food.quantity), unit: food.unit })) })) };
}
export function nutritionMealTemplateDraft(value?: NutritionMealTemplate | null): NutritionDraft {
  if (!value) return { name: '', notes: '', meals: [emptyMeal()] };
  return { name: '', notes: '', meals: [{ clientId: localId('meal'), name: value.name, notes: value.notes, foods: [...(value.foods ?? [])].sort((a, b) => a.sequence - b.sequence).map((food) => ({ clientId: localId('food'), foodName: food.foodName, quantity: String(food.quantity), unit: food.unit })) }] };
}
const quantity = (value: string) => Number(value.replace(',', '.'));
export function nutritionInput(draft: NutritionDraft, mealTemplate = false): TrainerNutritionInput { const firstMeal = draft.meals[0]; return { name: (mealTemplate ? firstMeal?.name : draft.name)?.trim() ?? '', notes: (mealTemplate ? firstMeal?.notes : draft.notes)?.trim() ?? '', meals: draft.meals.map((meal, index) => ({ name: meal.name.trim(), notes: meal.notes.trim(), sequence: index + 1, foods: meal.foods.map((food, foodIndex) => ({ foodName: food.foodName.trim(), quantity: quantity(food.quantity), unit: food.unit, sequence: foodIndex + 1 })) })) }; }
function validate(draft: NutritionDraft, mealTemplate: boolean) {
  const errors: string[] = [];
  if (!mealTemplate && !draft.name.trim()) errors.push('Informe o nome.');
  if (!mealTemplate && draft.name.length > 200) errors.push('O nome deve ter até 200 caracteres.');
  if (!mealTemplate && draft.notes.length > 2000) errors.push('As orientações gerais devem ter até 2000 caracteres.');
  if (!draft.meals.length) errors.push('Adicione ao menos uma refeição.');
  if (draft.meals.length > 20) errors.push('Use no máximo 20 refeições.');
  draft.meals.forEach((meal, mealIndex) => {
    const mealName = meal.name.trim() || `refeição ${mealIndex + 1}`;
    if (!meal.name.trim()) errors.push(`Informe o nome da refeição ${mealIndex + 1}.`);
    if (meal.name.length > 200) errors.push(`O nome de ${mealName} deve ter até 200 caracteres.`);
    if (meal.notes.length > 1000) errors.push(`As observações de ${mealName} devem ter até 1000 caracteres.`);
    if (!meal.foods.length) errors.push(`Adicione ao menos um item em ${mealName}.`);
    meal.foods.forEach((food, foodIndex) => {
      if (!food.foodName.trim()) errors.push(`Informe o alimento de ${mealName}, item ${foodIndex + 1}.`);
      if (food.foodName.length > 200) errors.push(`O alimento de ${mealName}, item ${foodIndex + 1}, deve ter até 200 caracteres.`);
      if (!Number.isFinite(quantity(food.quantity)) || quantity(food.quantity) <= 0 || quantity(food.quantity) > 10000) errors.push(`Informe uma quantidade maior que zero e até 10000 em ${mealName}, item ${foodIndex + 1}.`);
    });
  });
  return errors;
}

export function NutritionEditor({ initialValue, submitLabel, pending, error, onSubmit, onDirtyChange, mealTemplate = false }: { initialValue: NutritionDraft; submitLabel: string; pending: boolean; error?: string; onSubmit: (input: TrainerNutritionInput) => void; onDirtyChange?: (dirty: boolean) => void; mealTemplate?: boolean }) {
  const [draft, setDraft] = useState(initialValue);
  const [errors, setErrors] = useState<string[]>([]);
  const initialSignature = useMemo(() => JSON.stringify(nutritionInput(initialValue, mealTemplate)), [initialValue, mealTemplate]);
  useEffect(() => onDirtyChange?.(JSON.stringify(nutritionInput(draft, mealTemplate)) !== initialSignature), [draft, initialSignature, mealTemplate, onDirtyChange]);
  const updateMeal = (mealIndex: number, update: (meal: MealDraft) => MealDraft) => setDraft((current) => ({ ...current, meals: current.meals.map((meal, index) => index === mealIndex ? update(meal) : meal) }));
  const move = <T,>(items: T[], from: number, to: number) => { const result = [...items]; const [item] = result.splice(from, 1); result.splice(to, 0, item); return result; };
  const submit = () => { const next = validate(draft, mealTemplate); setErrors(next); if (!next.length) onSubmit(nutritionInput(draft, mealTemplate)); };
  return <>
    {!mealTemplate ? <Card style={styles.card}><Text style={styles.title}>Identificação</Text><Field label="Nome" value={draft.name} onChange={(name) => setDraft((current) => ({ ...current, name }))} placeholder="Ex.: Alimentação para rotina de treinos" /><Field label="Orientações gerais (opcional)" value={draft.notes} onChange={(notes) => setDraft((current) => ({ ...current, notes }))} placeholder="Contexto e orientações" multiline /></Card> : null}
    {draft.meals.map((meal, mealIndex) => <Card key={meal.clientId} style={styles.card}>
      <View style={styles.header}><View style={styles.identity}><Text style={styles.eyebrow}>{mealTemplate ? 'PRESET DE REFEIÇÃO' : `REFEIÇÃO ${mealIndex + 1}`}</Text><Text style={styles.title}>{meal.name.trim() || 'Nova refeição'}</Text></View>{!mealTemplate ? <View style={styles.actions}><Action label="↑" disabled={!mealIndex} onPress={() => setDraft((c) => ({ ...c, meals: move(c.meals, mealIndex, mealIndex - 1) }))} /><Action label="↓" disabled={mealIndex === draft.meals.length - 1} onPress={() => setDraft((c) => ({ ...c, meals: move(c.meals, mealIndex, mealIndex + 1) }))} /><Action label="Remover" danger onPress={() => setDraft((c) => ({ ...c, meals: c.meals.filter((_, i) => i !== mealIndex) }))} /></View> : null}</View>
      <Field label="Nome da refeição" value={meal.name} onChange={(name) => updateMeal(mealIndex, (m) => ({ ...m, name }))} placeholder="Ex.: Café da manhã" /><Field label="Observações (opcional)" value={meal.notes} onChange={(notes) => updateMeal(mealIndex, (m) => ({ ...m, notes }))} placeholder="Horário, preparo ou orientação" multiline />
      {meal.foods.map((food, foodIndex) => <View key={food.clientId} style={styles.food}>
        <View style={styles.header}><Text style={styles.itemTitle}>Item {foodIndex + 1}</Text><View style={styles.actions}><Action label="↑" disabled={!foodIndex} onPress={() => updateMeal(mealIndex, (m) => ({ ...m, foods: move(m.foods, foodIndex, foodIndex - 1) }))} /><Action label="↓" disabled={foodIndex === meal.foods.length - 1} onPress={() => updateMeal(mealIndex, (m) => ({ ...m, foods: move(m.foods, foodIndex, foodIndex + 1) }))} /><Action label="Remover" danger onPress={() => updateMeal(mealIndex, (m) => ({ ...m, foods: m.foods.filter((_, i) => i !== foodIndex) }))} /></View></View>
        <Field label="Alimento" value={food.foodName} onChange={(foodName) => updateMeal(mealIndex, (m) => ({ ...m, foods: m.foods.map((f, i) => i === foodIndex ? { ...f, foodName } : f) }))} placeholder="Ex.: Arroz integral" /><Field label="Quantidade" value={food.quantity} onChange={(value) => updateMeal(mealIndex, (m) => ({ ...m, foods: m.foods.map((f, i) => i === foodIndex ? { ...f, quantity: value } : f) }))} placeholder="Ex.: 120" keyboardType="decimal-pad" />
        <Text style={styles.label}>Unidade</Text><View style={styles.units}>{units.map((unit) => <Pressable key={unit} onPress={() => updateMeal(mealIndex, (m) => ({ ...m, foods: m.foods.map((f, i) => i === foodIndex ? { ...f, unit } : f) }))} style={[styles.unit, food.unit === unit && styles.unitSelected]}><Text style={[styles.unitText, food.unit === unit && styles.unitTextSelected]}>{unit}</Text></Pressable>)}</View>
      </View>)}
      <Button variant="secondary" disabled={meal.foods.length >= 30} onPress={() => updateMeal(mealIndex, (m) => ({ ...m, foods: [...m.foods, emptyFood()] }))}>+ Adicionar item</Button>
    </Card>)}
    {!mealTemplate ? <Button variant="secondary" disabled={draft.meals.length >= 20} onPress={() => setDraft((c) => ({ ...c, meals: [...c.meals, emptyMeal()] }))}>+ Adicionar refeição</Button> : null}
    {errors.length || error ? <Card style={styles.error}><Text accessibilityRole="alert" style={styles.errorTitle}>Revise antes de salvar</Text>{errors.map((item) => <Text key={item} style={styles.errorText}>• {item}</Text>)}{error ? <Text style={styles.errorText}>{error}</Text> : null}</Card> : null}
    <Button loading={pending} onPress={submit}>{submitLabel}</Button>
  </>;
}

function Field({ label, value, onChange, placeholder, multiline, keyboardType }: { label: string; value: string; onChange: (value: string) => void; placeholder: string; multiline?: boolean; keyboardType?: 'decimal-pad' }) { return <View style={styles.field}><Text style={styles.label}>{label}</Text><TextInput value={value} onChangeText={onChange} multiline={multiline} keyboardType={keyboardType} maxLength={multiline ? 2000 : 200} placeholder={placeholder} placeholderTextColor={colors.textMuted} style={[styles.input, multiline && styles.multiline]} /></View>; }
function Action({ label, onPress, disabled, danger }: { label: string; onPress: () => void; disabled?: boolean; danger?: boolean }) { return <Pressable disabled={disabled} onPress={onPress} style={[styles.action, disabled && styles.disabled]}><Text style={[styles.actionText, danger && styles.danger]}>{label}</Text></Pressable>; }
const styles = StyleSheet.create({ card: { gap: spacing.md }, title: { ...typography.headingMD, color: colors.textPrimary }, itemTitle: { ...typography.bodyLG, color: colors.textPrimary, fontWeight: '700' }, eyebrow: { ...typography.caption, color: colors.primary }, header: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'flex-start', gap: spacing.sm }, identity: { flex: 1, gap: spacing.xxs }, actions: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs, justifyContent: 'flex-end' }, action: { minHeight: 36, minWidth: 36, justifyContent: 'center', alignItems: 'center', paddingHorizontal: spacing.sm, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm }, actionText: { ...typography.caption, color: colors.textPrimary }, danger: { color: colors.danger }, disabled: { opacity: .3 }, field: { gap: spacing.xs }, label: { ...typography.caption, color: colors.textSecondary }, input: { ...typography.bodyMD, color: colors.textPrimary, minHeight: 50, borderWidth: 1, borderColor: colors.border, borderRadius: radius.sm, paddingHorizontal: spacing.md, backgroundColor: colors.background }, multiline: { minHeight: 88, paddingTop: spacing.md, textAlignVertical: 'top' }, food: { gap: spacing.sm, padding: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, backgroundColor: colors.surfaceElevated }, units: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs }, unit: { minHeight: 40, justifyContent: 'center', paddingHorizontal: spacing.md, borderWidth: 1, borderColor: colors.border, borderRadius: radius.pill }, unitSelected: { borderColor: colors.primary, backgroundColor: colors.surface }, unitText: { ...typography.caption, color: colors.textSecondary }, unitTextSelected: { color: colors.primary }, error: { gap: spacing.xs, borderColor: colors.danger }, errorTitle: { ...typography.headingMD, color: colors.danger }, errorText: { ...typography.bodyMD, color: colors.textSecondary } });
