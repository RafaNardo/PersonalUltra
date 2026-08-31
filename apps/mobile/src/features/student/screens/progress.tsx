import { Redirect, router } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { ProgressBarChart } from '@/src/features/student/components/progress-bar-chart';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

const shortDate = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit' });
const number = (value: string) => Number(value.trim().replace(',', '.'));
const dayKey = (value: string | Date) => { const date = new Date(value); return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`; };
const ml = (value: number) => value >= 1000 ? `${(value / 1000).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} L` : `${value} ml`;

function EntryEditor({ value, onChange, placeholder, submit, onSubmit, busy }: { value: string; onChange: (value: string) => void; placeholder: string; submit: string; onSubmit: () => void; busy: boolean }) {
  return <View style={styles.editor}><TextInput value={value} onChangeText={onChange} keyboardType="decimal-pad" placeholder={placeholder} placeholderTextColor={colors.textMuted} style={styles.input} /><Button loading={busy} disabled={!value.trim()} onPress={onSubmit}>{submit}</Button></View>;
}

export function StudentProgressScreen() {
  const session = useInviteSessionStore((state) => state.session);
  const queryClient = useQueryClient();
  const [weightValue, setWeightValue] = useState(''); const [hydrationValue, setHydrationValue] = useState('');
  const weight = useQuery({ queryKey: ['student', session?.studentId, 'weight'], queryFn: () => inviteApi.weight(session!.accessToken), enabled: Boolean(session) });
  const hydration = useQuery({ queryKey: ['student', session?.studentId, 'hydration'], queryFn: () => inviteApi.hydration(session!.accessToken), enabled: Boolean(session) });
  const addWeight = useMutation({ mutationFn: () => inviteApi.addWeight(session!.accessToken, number(weightValue)), onSuccess: () => { setWeightValue(''); void queryClient.invalidateQueries({ queryKey: ['student', session?.studentId, 'weight'] }); } });
  const addHydration = useMutation({ mutationFn: (amount: number) => inviteApi.addHydration(session!.accessToken, amount), onSuccess: () => { setHydrationValue(''); void queryClient.invalidateQueries({ queryKey: ['student', session?.studentId, 'hydration'] }); } });
  const hydrationDays = useMemo(() => { const totals = new Map<string, { id: string; amount: number; label: string }>(); (hydration.data ?? []).forEach((entry) => { const key = dayKey(entry.recordedAt); const previous = totals.get(key); totals.set(key, { id: key, amount: (previous?.amount ?? 0) + entry.amountMl, label: shortDate.format(new Date(entry.recordedAt)) }); }); return [...totals.values()]; }, [hydration.data]);
  if (!session) return <Redirect href="/login" />;
  if (weight.isLoading || hydration.isLoading) return <LoadingView message="Carregando seu progresso…" />;
  if (weight.isError || hydration.isError) return <ErrorView message={weight.error?.message ?? hydration.error?.message ?? 'Não foi possível carregar seu progresso.'} onRetry={() => { void weight.refetch(); void hydration.refetch(); }} />;
  const weights = weight.data!; const water = hydration.data!;
  const today = water.filter((entry) => dayKey(entry.recordedAt) === dayKey(new Date())).reduce((total, entry) => total + entry.amountMl, 0);
  return <Screen style={styles.page}><TopBar eyebrow="PROGRESSO" title="Progresso" onBack={() => router.back()} />
    <Card style={styles.card}><Text style={styles.eyebrow}>ACOMPANHAMENTO</Text><Text style={styles.title}>Evolução de peso</Text>{weights.length >= 2 ? <ProgressBarChart points={weights.map((entry) => ({ id: entry.id, value: entry.weightKg, label: shortDate.format(new Date(entry.recordedAt)), accessibilityLabel: `${entry.weightKg} kg` }))} valueLabel={(value) => `${value} kg`} /> : weights.length ? <Text style={styles.weightMetric}>{weights.at(-1)!.weightKg} kg</Text> : <Text style={styles.copy}>Registre sua primeira medida para começar a acompanhar.</Text>}<EntryEditor value={weightValue} onChange={setWeightValue} placeholder="Nova medida em kg" submit="Adicionar peso" busy={addWeight.isPending} onSubmit={() => addWeight.mutate()} /><Button variant="ghost" accessibilityHint="Abre seus registros para editar ou excluir" onPress={() => router.push('/student/progress/weight')}>Ver histórico e editar →</Button></Card>
    <Card style={styles.card}><Text style={styles.eyebrow}>HIDRATAÇÃO DE HOJE</Text><Text style={styles.title}>Água</Text><Text style={styles.hydrationMetric}>{ml(today)}</Text>{hydrationDays.length >= 2 ? <ProgressBarChart accent={colors.signalGreen} points={hydrationDays.map((entry) => ({ id: entry.id, value: entry.amount, label: entry.label, accessibilityLabel: `${entry.amount} ml em ${entry.label}` }))} valueLabel={ml} /> : null}<View style={styles.quickActions}><QuickAction label="+50 ml" onPress={() => addHydration.mutate(50)} busy={addHydration.isPending} /><QuickAction label="+250 ml" onPress={() => addHydration.mutate(250)} busy={addHydration.isPending} /><QuickAction label="+500 ml" onPress={() => addHydration.mutate(500)} busy={addHydration.isPending} /><QuickAction label="+1 L" onPress={() => addHydration.mutate(1000)} busy={addHydration.isPending} /></View><EntryEditor value={hydrationValue} onChange={setHydrationValue} placeholder="Outro valor em ml" submit="Adicionar água" busy={addHydration.isPending} onSubmit={() => addHydration.mutate(Math.round(number(hydrationValue)))} /><Button variant="ghost" accessibilityHint="Abre seus registros para editar ou excluir" onPress={() => router.push('/student/progress/hydration')}>Ver histórico e editar →</Button></Card>
  </Screen>;
}

function QuickAction({ label, onPress, busy }: { label: string; onPress: () => void; busy: boolean }) { return <Button variant="secondary" style={styles.quickAction} disabled={busy} onPress={onPress}>{label}</Button>; }

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.xl }, card: { gap: spacing.md }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, title: { ...typography.headingLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, weightMetric: { ...typography.metricXL, color: colors.primary }, hydrationMetric: { ...typography.metricXL, color: colors.signalGreen }, editor: { gap: spacing.sm }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, padding: spacing.md, minHeight: 52 }, quickActions: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm }, quickAction: { flexGrow: 1, flexBasis: 130, minHeight: 46, paddingHorizontal: spacing.md } });
