import { Redirect, router } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { Alert, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentHydration, type StudentWeight } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

const date = new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium', timeStyle: 'short' });
const number = (value: string) => Number(value.trim().replace(',', '.'));
const ml = (value: number) => value >= 1000 ? `${(value / 1000).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} L` : `${value} ml`;

export function StudentWeightHistoryScreen() { return <StudentProgressHistory kind="weight" />; }
export function StudentHydrationHistoryScreen() { return <StudentProgressHistory kind="hydration" />; }

function StudentProgressHistory({ kind }: { kind: 'weight' | 'hydration' }) {
  const session = useInviteSessionStore((state) => state.session); const client = useQueryClient();
  const [editing, setEditing] = useState<StudentWeight | StudentHydration>(); const [value, setValue] = useState('');
  const query = useQuery<Array<StudentWeight | StudentHydration>>({ queryKey: ['student', session?.studentId, kind], queryFn: async () => kind === 'weight' ? await inviteApi.weight(session!.accessToken) : await inviteApi.hydration(session!.accessToken), enabled: Boolean(session) });
  const save = useMutation<unknown, Error, void>({ mutationFn: async () => kind === 'weight' ? await inviteApi.updateWeight(session!.accessToken, editing!.id, number(value), editing!.recordedAt) : await inviteApi.updateHydration(session!.accessToken, editing!.id, Math.round(number(value)), editing!.recordedAt), onSuccess: () => { setEditing(undefined); setValue(''); void client.invalidateQueries({ queryKey: ['student', session?.studentId, kind] }); } });
  const remove = useMutation<unknown, Error, StudentWeight | StudentHydration>({ mutationFn: async (entry) => kind === 'weight' ? await inviteApi.deleteWeight(session!.accessToken, entry.id) : await inviteApi.deleteHydration(session!.accessToken, entry.id), onSuccess: () => void client.invalidateQueries({ queryKey: ['student', session?.studentId, kind] }) });
  if (!session) return <Redirect href="/login" />;
  if (query.isLoading) return <LoadingView message="Carregando seus registros…" />;
  if (query.isError) return <ErrorView message={query.error.message} onRetry={() => query.refetch()} />;
  const entries = query.data!;
  const unit = kind === 'weight' ? 'kg' : 'ml';
  const beginEdit = (entry: StudentWeight | StudentHydration) => { setEditing(entry); setValue(String('weightKg' in entry ? entry.weightKg : entry.amountMl).replace('.', ',')); };
  const confirmRemove = (entry: StudentWeight | StudentHydration) => Alert.alert('Excluir este registro?', 'Ele será removido do seu histórico.', [{ text: 'Cancelar', style: 'cancel' }, { text: 'Excluir', style: 'destructive', onPress: () => remove.mutate(entry) }]);
  return <Screen style={styles.page}><TopBar eyebrow="PROGRESSO" title={kind === 'weight' ? 'Histórico de peso' : 'Histórico de água'} onBack={() => router.back()} />
    {editing ? <Card style={styles.editor}><Text style={styles.title}>Corrigir registro</Text><TextInput value={value} onChangeText={setValue} keyboardType="decimal-pad" placeholder={`Valor em ${unit}`} placeholderTextColor={colors.textMuted} style={styles.input} /><View style={styles.actions}><Button variant="secondary" style={styles.action} onPress={() => { setEditing(undefined); setValue(''); }}>Cancelar</Button><Button style={styles.action} disabled={!value.trim()} loading={save.isPending} onPress={() => save.mutate()}>Salvar</Button></View></Card> : null}
    {entries.length ? <View style={styles.list}>{entries.slice().reverse().map((entry) => { const display = 'weightKg' in entry ? `${entry.weightKg} kg` : ml(entry.amountMl); return <Card key={entry.id} style={styles.entry}><View style={styles.entryCopy}><Text style={styles.value}>{display}</Text><Text style={styles.copy}>{date.format(new Date(entry.recordedAt))}</Text></View><View style={styles.entryActions}><Pressable accessibilityRole="button" accessibilityLabel={`Editar ${display}`} onPress={() => beginEdit(entry)}><Text style={styles.edit}>Editar</Text></Pressable><Pressable accessibilityRole="button" accessibilityLabel={`Excluir ${display}`} disabled={remove.isPending} onPress={() => confirmRemove(entry)}><Text style={styles.remove}>Excluir</Text></Pressable></View></Card>; })}</View> : <EmptyState status="SEM REGISTROS" title={kind === 'weight' ? 'Seu histórico de peso ainda está vazio.' : 'Seu histórico de água ainda está vazio.'} message="Use o card de Progresso para criar o primeiro registro." actionLabel="Voltar ao progresso" onAction={() => router.replace('/student/progress')} />}
  </Screen>;
}

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, editor: { gap: spacing.md }, title: { ...typography.headingMD, color: colors.textPrimary }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, padding: spacing.md, minHeight: 52 }, actions: { flexDirection: 'row', gap: spacing.sm }, action: { flex: 1 }, list: { gap: spacing.sm }, entry: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: spacing.md }, entryCopy: { flex: 1, minWidth: 130, gap: spacing.xxs }, value: { ...typography.headingMD, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary }, entryActions: { flexDirection: 'row', gap: spacing.md }, edit: { ...typography.caption, color: colors.primary }, remove: { ...typography.caption, color: colors.danger } });
