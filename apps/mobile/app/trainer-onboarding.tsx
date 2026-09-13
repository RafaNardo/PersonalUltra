import { useUser } from '@clerk/expo';
import { useMutation } from '@tanstack/react-query';
import { router } from 'expo-router';
import { useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { trainerClient } from '@/src/api/trainer-client';

export default function TrainerOnboardingScreen() {
  const { user } = useUser(); const [name, setName] = useState([user?.firstName, user?.lastName].filter(Boolean).join(' ')); const [displayName, setDisplayName] = useState(user?.firstName ?? '');
  const complete = useMutation({ mutationFn: () => trainerClient.completeOnboarding({ name, displayName }), onSuccess: () => router.replace('/trainer') });
  return <Screen style={styles.page}><View style={styles.hero}><Text style={styles.eyebrow}>PRIMEIRO ACESSO</Text><Text style={styles.title}>Configure seu espaço profissional.</Text><Text style={styles.copy}>Estes dados serão exibidos aos seus alunos.</Text></View><Card style={styles.card}><TextInput value={name} onChangeText={setName} placeholder="Seu nome profissional" placeholderTextColor={colors.textMuted} style={styles.input} /><TextInput value={displayName} onChangeText={setDisplayName} placeholder="Nome de exibição" placeholderTextColor={colors.textMuted} style={styles.input} />{complete.error ? <Text style={styles.error}>{complete.error.message}</Text> : null}<Button loading={complete.isPending} disabled={!name.trim() || !displayName.trim()} onPress={() => void complete.mutateAsync()}>Concluir cadastro</Button><Button variant="ghost" onPress={() => router.back()}>Voltar</Button></Card></Screen>;
}
const styles = StyleSheet.create({ page: { justifyContent: 'center', gap: spacing.xxl }, hero: { gap: spacing.md }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1.4 }, title: { ...typography.displayLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary }, card: { gap: spacing.md }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: 12, paddingHorizontal: spacing.md, paddingVertical: spacing.md }, error: { ...typography.bodyMD, color: colors.danger } });
