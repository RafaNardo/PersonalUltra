import { Redirect, router } from 'expo-router';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { Button, Card, ErrorView, LoadingView } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { inviteApi } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';

export function StudentProfileScreen() {
  const session = useInviteSessionStore((state) => state.session); const clear = useInviteSessionStore((state) => state.clear); const client = useQueryClient();
  const [preferredName, setPreferredName] = useState('');
  const profile = useQuery({ queryKey: ['student', session?.studentId, 'profile'], queryFn: () => inviteApi.profile(session!.accessToken), enabled: Boolean(session) });
  useEffect(() => { if (profile.data) setPreferredName(profile.data.preferredName ?? ''); }, [profile.data]);
  const save = useMutation({ mutationFn: () => inviteApi.updateProfile(session!.accessToken, { preferredName: preferredName.trim() || undefined }), onSuccess: () => void client.invalidateQueries({ queryKey: ['student', session?.studentId, 'profile'] }) });
  if (!session) return <Redirect href="/login" />;
  if (profile.isLoading) return <LoadingView message="Carregando seu perfil…" />;
  if (profile.isError) return <ErrorView message={profile.error.message} onRetry={() => profile.refetch()} />;
  const data = profile.data!;
  return <Screen style={styles.page}><TopBar eyebrow="MEU PERFIL" title="Meu perfil" onBack={() => router.back()} />
    <Card style={styles.card}><Text style={styles.eyebrow}>COMO VOCÊ QUER SER CHAMADO(A)</Text><Text style={styles.title}>Nome no app</Text><Text style={styles.copy}>Usaremos este nome nas suas telas. Seu cadastro com o personal não é alterado.</Text><TextInput value={preferredName} onChangeText={setPreferredName} placeholder={data.firstName} placeholderTextColor={colors.textMuted} maxLength={100} style={styles.input} /><Button loading={save.isPending} onPress={() => save.mutate()}>Salvar nome</Button></Card>
    <Card style={styles.card}><Text style={styles.eyebrow}>DADOS CADASTRAIS</Text><ProfileField label="Nome cadastrado" value={`${data.firstName} ${data.lastName}`.trim()} /><ProfileField label="E-mail" value={data.email} /><ProfileField label="Telefone" value={data.phone} /></Card>
    <View style={styles.actions}><Button variant="secondary" onPress={() => { clear(); router.replace('/demo-role-switch'); }}>Trocar contexto demo</Button><Button variant="ghost" onPress={() => { clear(); router.replace('/login'); }}>Sair</Button></View>
  </Screen>;
}

function ProfileField({ label, value }: { label: string; value?: string }) { return <View style={styles.field}><Text style={styles.label}>{label}</Text><Text style={styles.fieldValue}>{value || 'Não informado'}</Text></View>; }

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, card: { gap: spacing.md }, eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1 }, title: { ...typography.headingMD, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, padding: spacing.md, minHeight: 52 }, field: { gap: spacing.xxs, paddingTop: spacing.sm, borderTopWidth: 1, borderTopColor: colors.border }, label: { ...typography.caption, color: colors.textMuted }, fieldValue: { ...typography.bodyLG, color: colors.textPrimary }, actions: { gap: spacing.sm } });
