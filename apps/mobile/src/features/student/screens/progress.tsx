import { Redirect, router } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { ProgressBarChart } from '@/src/features/student/components/progress-bar-chart';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { HydrationProgressCard } from '@/src/features/student/components/hydration-progress-card';

const shortDate = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit' });
const number = (value: string) => Number(value.trim().replace(',', '.'));

function EntryEditor({ value, onChange, placeholder, submit, onSubmit, busy }: { value: string; onChange: (value: string) => void; placeholder: string; submit: string; onSubmit: () => void; busy: boolean }) {
  return <View style={styles.editor}><TextInput value={value} onChangeText={onChange} keyboardType="decimal-pad" placeholder={placeholder} placeholderTextColor={colors.textMuted} style={styles.input} /><Button loading={busy} disabled={!value.trim()} onPress={onSubmit}>{submit}</Button></View>;
}

export function StudentProgressScreen() {
  const session = useInviteSessionStore((state) => state.session);
  const queryClient = useQueryClient();
  const [weightValue, setWeightValue] = useState('');
  const weight = useQuery({ queryKey: ['student', session?.studentId, 'weight'], queryFn: () => inviteApi.weight(session!.accessToken), enabled: Boolean(session) });
  const addWeight = useMutation({ mutationFn: () => inviteApi.addWeight(session!.accessToken, number(weightValue)), onSuccess: () => { setWeightValue(''); void queryClient.invalidateQueries({ queryKey: ['student', session?.studentId, 'weight'] }); } });
  if (!session) return <Redirect href="/login" />;
  if (weight.isLoading) return <LoadingView message="Carregando seu progresso…" />;
  if (weight.isError) return <ErrorView message={weight.error.message} onRetry={() => weight.refetch()} />;
  const weights = weight.data!;
  return <Screen style={styles.page}><TopBar eyebrow="PROGRESSO" title="Progresso" onBack={() => router.back()} />
    <Card style={styles.card}><Text style={styles.eyebrow}>ACOMPANHAMENTO</Text><Text style={styles.title}>Evolução de peso</Text>{weights.length >= 2 ? <ProgressBarChart points={weights.map((entry) => ({ id: entry.id, value: entry.weightKg, label: shortDate.format(new Date(entry.recordedAt)), accessibilityLabel: `${entry.weightKg} kg` }))} valueLabel={(value) => `${value} kg`} /> : weights.length ? <Text style={styles.weightMetric}>{weights.at(-1)!.weightKg} kg</Text> : <Text style={styles.copy}>Registre sua primeira medida para começar a acompanhar.</Text>}<EntryEditor value={weightValue} onChange={setWeightValue} placeholder="Nova medida em kg" submit="Adicionar peso" busy={addWeight.isPending} onSubmit={() => addWeight.mutate()} /><Button variant="ghost" accessibilityHint="Abre seus registros para editar ou excluir" onPress={() => router.push('/student/progress/weight')}>Ver histórico e editar →</Button></Card>
    <HydrationProgressCard />
  </Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.xl }, card: { gap: spacing.md }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, title: { ...typography.headingLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, weightMetric: { ...typography.metricXL, color: colors.primary }, editor: { gap: spacing.sm }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, padding: spacing.md, minHeight: 52 } });
