import { router } from 'expo-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

export function StudentProgressScreen() {
  const session = useInviteSessionStore((state) => state.session);
  const client = useQueryClient();
  const [weight, setWeight] = useState('');
  const query = useQuery({ queryKey: ['student', session?.studentId, 'weight'], queryFn: () => inviteApi.weight(session!.accessToken), enabled: Boolean(session) });
  const add = useMutation({ mutationFn: () => inviteApi.addWeight(session!.accessToken, Number(weight.replace(',', '.'))), onSuccess: () => { setWeight(''); client.invalidateQueries({ queryKey: ['student', session?.studentId, 'weight'] }); } });
  if (!session) { router.replace('/login'); return null; }
  if (query.isLoading) return <LoadingView message="Carregando seu progresso…" />;
  if (query.isError) return <ErrorView message={query.error.message} onRetry={() => query.refetch()} />;
  return <Screen style={styles.page}><TopBar eyebrow="PROGRESSO" title="Seu peso" onBack={() => router.back()} /><Card style={styles.card}><Text style={styles.title}>Registrar nova medida</Text><View style={styles.row}><TextInput value={weight} onChangeText={setWeight} keyboardType="decimal-pad" placeholder="Peso em kg" placeholderTextColor={colors.textMuted} style={styles.input} /><Button loading={add.isPending} disabled={!weight.trim()} onPress={() => add.mutate()}>Salvar</Button></View></Card><View style={styles.list}>{query.data!.length === 0 ? <EmptyState status="PRIMEIRO REGISTRO" symbol="↗" title="Comece sua linha de progresso." message="Registre seu peso acima. A evolução aparecerá aqui em ordem cronológica." /> : query.data!.slice().reverse().map((entry) => <Card key={entry.id} style={styles.entry}><Text style={styles.value}>{entry.weightKg} kg</Text><Text style={styles.copy}>{new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium' }).format(new Date(entry.recordedAt))}</Text></Card>)}</View></Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.md }, card: { gap: spacing.md }, title: { ...typography.headingMD, color: colors.textPrimary }, row: { flexDirection: 'row', gap: spacing.sm, alignItems: 'center' }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: spacing.md, flex: 1 }, list: { gap: spacing.sm }, entry: { gap: spacing.xs }, value: { ...typography.metricXL, color: colors.signalGreen }, copy: { ...typography.bodyMD, color: colors.textSecondary } });
