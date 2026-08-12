import { router, useLocalSearchParams } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, Text, TextInput } from 'react-native';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { trainerClient } from '@/src/api/trainer-client';

export default function TrainerNutritionEditor() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const q = useQuery({ queryKey: ['trainer', 'nutrition', id], queryFn: () => trainerClient.nutrition(id!), enabled: Boolean(id) });
  const [name, setName] = useState(''); const [meal, setMeal] = useState(''); const [food, setFood] = useState(''); const [quantity, setQuantity] = useState('');
  const save = useMutation({ mutationFn: () => trainerClient.saveNutrition(id!, { name: name || q.data?.name || 'Plano alimentar', notes: '', meals: [{ name: meal.trim(), sequence: 1, notes: '', foods: [{ foodName: food.trim(), quantityGrams: Number(quantity.replace(',', '.')) }] }] }), onSuccess: () => router.back() });
  if (q.isLoading) return <LoadingView message="Abrindo alimentação…" />;
  if (q.isError) return <ErrorView message={q.error.message} onRetry={() => q.refetch()} />;
  return <Screen style={styles.page}><TopBar eyebrow="ALIMENTAÇÃO" title="Plano do aluno" onBack={() => router.back()} /><Text style={styles.copy}>Registre uma refeição e seus itens. A prescrição fica vinculada ao Trainer.</Text><Card style={styles.card}><Text style={styles.title}>Plano</Text><TextInput value={name || q.data?.name || ''} onChangeText={setName} placeholder="Nome do plano" placeholderTextColor={colors.textMuted} style={styles.input} /><TextInput value={meal} onChangeText={setMeal} placeholder="Nome da refeição" placeholderTextColor={colors.textMuted} style={styles.input} /><TextInput value={food} onChangeText={setFood} placeholder="Alimento" placeholderTextColor={colors.textMuted} style={styles.input} /><TextInput value={quantity} onChangeText={setQuantity} keyboardType="decimal-pad" placeholder="Quantidade em gramas" placeholderTextColor={colors.textMuted} style={styles.input} /><Button loading={save.isPending} disabled={!meal.trim() || !food.trim() || !quantity.trim()} onPress={() => save.mutate()}>Salvar plano</Button></Card></Screen>;
}
const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, card: { gap: spacing.md }, title: { ...typography.headingMD, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: spacing.md } });
