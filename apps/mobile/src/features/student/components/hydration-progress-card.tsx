import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { router } from 'expo-router';
import { Button, Card } from '@/src/components/ui';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { ProgressBarChart } from './progress-bar-chart';

const shortDate = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit' });
const dayKey = (value: string | Date) => { const date = new Date(value); return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`; };
const ml = (value: number) => value >= 1000 ? `${(value / 1000).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} L` : `${value} ml`;

export function HydrationProgressCard() {
  const session = useInviteSessionStore((state) => state.session); const client = useQueryClient(); const [customValue, setCustomValue] = useState('');
  const hydration = useQuery({ queryKey: ['student', session?.studentId, 'hydration'], queryFn: () => inviteApi.hydration(session!.accessToken), enabled: Boolean(session) });
  const add = useMutation({ mutationFn: (amount: number) => inviteApi.addHydration(session!.accessToken, amount), onSuccess: () => { setCustomValue(''); void client.invalidateQueries({ queryKey: ['student', session?.studentId, 'hydration'] }); } });
  const days = useMemo(() => { const totals = new Map<string, { id: string; amount: number; label: string }>(); (hydration.data ?? []).forEach((entry) => { const key = dayKey(entry.recordedAt); const previous = totals.get(key); totals.set(key, { id: key, amount: (previous?.amount ?? 0) + entry.amountMl, label: shortDate.format(new Date(entry.recordedAt)) }); }); return [...totals.values()]; }, [hydration.data]);
  if (hydration.isLoading) return <Card style={styles.card}><Text style={styles.copy}>Carregando hidratação…</Text></Card>;
  if (hydration.isError) return <Card style={styles.card}><Text style={styles.copy}>Não foi possível carregar a hidratação.</Text><Button variant="secondary" onPress={() => hydration.refetch()}>Tentar novamente</Button></Card>;
  const today = hydration.data!.filter((entry) => dayKey(entry.recordedAt) === dayKey(new Date())).reduce((total, entry) => total + entry.amountMl, 0);
  const customAmount = Math.round(Number(customValue.trim().replace(',', '.')));
  return <Card style={styles.card}><Text style={styles.eyebrow}>HIDRATAÇÃO DE HOJE</Text><Text style={styles.title}>Água</Text><Text style={styles.metric}>{ml(today)}</Text>{days.length >= 2 ? <ProgressBarChart accent={colors.signalGreen} points={days.map((entry) => ({ id: entry.id, value: entry.amount, label: entry.label, accessibilityLabel: `${entry.amount} ml em ${entry.label}` }))} valueLabel={ml} /> : null}<View style={styles.quickActions}>{[50, 250, 500, 1000].map((amount) => <Button key={amount} variant="secondary" style={styles.quickAction} disabled={add.isPending} onPress={() => add.mutate(amount)}>{amount === 1000 ? '+1 L' : `+${amount} ml`}</Button>)}</View><TextInput value={customValue} onChangeText={setCustomValue} keyboardType="decimal-pad" placeholder="Outro valor em ml" placeholderTextColor={colors.textMuted} style={styles.input} /><Button loading={add.isPending} disabled={!customValue.trim() || !Number.isFinite(customAmount) || customAmount < 1} onPress={() => add.mutate(customAmount)}>Adicionar água</Button><Button variant="ghost" accessibilityHint="Abre seus registros para editar ou excluir" onPress={() => router.push('/student/progress/hydration')}>Ver histórico e editar →</Button></Card>;
}

const styles = StyleSheet.create({ card: { gap: spacing.md }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, title: { ...typography.headingLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary }, metric: { ...typography.metricXL, color: colors.signalGreen }, quickActions: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm }, quickAction: { flexGrow: 1, flexBasis: 130, minHeight: 46, paddingHorizontal: spacing.md }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, padding: spacing.md, minHeight: 52 } });
