import { router } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useCreateTrainerTemplate, useDuplicateTrainerTemplate, useTrainerTemplates } from '@/src/features/trainer/training/hooks';

export default function TrainerTrainingScreen() {
  const templates = useTrainerTemplates(); const create = useCreateTrainerTemplate(); const duplicate = useDuplicateTrainerTemplate(); const [name, setName] = useState('');
  if (templates.isLoading) return <LoadingView message="Carregando seus treinos…" />;
  if (templates.isError) return <ErrorView message={templates.error.message} onRetry={() => templates.refetch()} />;
  const createTemplate = () => { if (!name.trim()) return; create.mutate({ name: name.trim(), notes: 'Treino criado pelo personal.', exercises: [{ name: 'Exercício 1', sequence: 1, sets: 3, repetitions: 10, restSeconds: 60, notes: '' }] }); setName(''); };
  return <Screen style={styles.page}><TopBar eyebrow="PRESCRIÇÃO" title="Meus treinos" onBack={() => router.back()} /><Text style={styles.copy}>Crie modelos reutilizáveis e aplique uma cópia ao aluno.</Text><Card style={styles.create}><Text style={styles.cardTitle}>Novo modelo</Text><TextInput value={name} onChangeText={setName} placeholder="Nome do treino" placeholderTextColor={colors.textMuted} style={styles.input} /><Button loading={create.isPending} disabled={!name.trim()} onPress={createTemplate}>Criar treino</Button></Card><View style={styles.list}>{templates.data!.map((item) => <Card key={item.id} style={styles.item}><Text style={styles.itemTitle}>{item.name}</Text><Text style={styles.copy}>{item.exerciseCount ?? 0} exercícios</Text><View style={styles.actions}><Button variant="secondary" onPress={() => duplicate.mutate(item.id)}>Duplicar</Button><Button variant="ghost" onPress={() => router.push({ pathname: '/trainer/training/[id]', params: { id: item.id } })}>Editar</Button></View></Card>)}</View></Screen>;
}
const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, copy: { ...typography.bodyMD, color: colors.textSecondary }, create: { gap: spacing.md }, cardTitle: { ...typography.headingMD, color: colors.textPrimary }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, padding: spacing.md }, list: { gap: spacing.sm }, item: { gap: spacing.xs }, itemTitle: { ...typography.headingMD, color: colors.textPrimary }, actions: { flexDirection: 'row', gap: spacing.sm } });
