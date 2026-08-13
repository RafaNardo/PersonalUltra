import { router, useLocalSearchParams } from 'expo-router';
import { useQuery } from '@tanstack/react-query';
import { Image, StyleSheet, Text, View } from 'react-native';
import { Button, Card, EmptyState, ErrorView, LoadingView, Tag } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { inviteApi, type StudentWorkoutPreview } from '@/src/features/student/invite/api';
import { useInviteSessionStore } from '@/src/features/student/invite/session-store';
import { exerciseMediaSource } from '@/src/shared/training/exercise-media';

export function StudentTrainingPreviewScreen() {
  const { id } = useLocalSearchParams<{ id: string }>();
  const session = useInviteSessionStore((state) => state.session);
  const preview = useQuery({ queryKey: ['student', session?.studentId, 'training-preview', id], queryFn: () => inviteApi.trainingPreview(session!.accessToken, id!), enabled: Boolean(session && id) });

  if (!session) { router.replace('/login'); return null; }
  if (preview.isLoading) return <LoadingView message="Carregando detalhes do treino…" />;
  if (preview.isError) return <ErrorView message={preview.error.message} onRetry={() => preview.refetch()} />;
  if (!preview.data) return <ErrorView message="Este treino não está disponível." onRetry={() => preview.refetch()} />;

  const workout = preview.data;
  const isContinuing = workout.state === 'InProgress' && Boolean(workout.activeSessionId);
  const state = workout.state === 'Recommended' ? { label: 'Recomendado', tone: 'primary' as const } : workout.state === 'InProgress' ? { label: 'Em andamento', tone: 'success' as const } : workout.state === 'Completed' ? { label: 'Concluído anteriormente', tone: 'success' as const } : { label: 'Disponível', tone: 'neutral' as const };

  return <Screen style={styles.page}>
    <TopBar eyebrow="PRÉVIA DO TREINO" title={workout.name} onBack={() => router.back()} />
    <View style={styles.headingMeta}><Tag tone={state.tone}>{state.label}</Tag><Text style={styles.meta}>{workout.exercises.length} exercícios · Dia {workout.recommendedDay}</Text></View>
    {workout.notes ? <Text style={styles.copy}>{workout.notes}</Text> : null}
    <Text style={styles.intro}>{isContinuing ? 'Você já tem uma sessão em andamento. Continue quando estiver pronto.' : 'Confira a sequência, a prescrição e as orientações antes de começar.'}</Text>
    {workout.exercises.length === 0 ? <EmptyState status="PRESCRIÇÃO INCOMPLETA" symbol="●" title="Este treino ainda não possui exercícios." message="Seu personal precisa adicionar a sequência antes que você possa iniciar a sessão." /> : workout.exercises.map((exercise) => <PreviewExercise key={exercise.id} exercise={exercise} />)}
    <Button disabled={workout.exercises.length === 0} onPress={() => router.replace({ pathname: '/student/training/[id]', params: { id: workout.id, start: '1' } })}>{isContinuing ? 'Continuar treino' : 'Iniciar treino'}</Button>
  </Screen>;
}

function PreviewExercise({ exercise }: { exercise: StudentWorkoutPreview['exercises'][number] }) {
  const source = exerciseMediaSource(exercise.imageRef);
  return <Card style={styles.exerciseCard}>
    <View style={styles.exerciseHeader}>
      {source ? <Image source={source} style={styles.exerciseImage} resizeMode="cover" accessibilityLabel={`Imagem do exercício ${exercise.name}`} /> : <View style={styles.imageFallback}><Text style={styles.sequence}>{exercise.sequence}</Text></View>}
      <View style={styles.identity}><Text style={styles.title}>{exercise.sequence}. {exercise.name}</Text>{(exercise.primaryMuscleGroup || exercise.equipment) ? <Text style={styles.context}>{[exercise.primaryMuscleGroup, exercise.equipment].filter(Boolean).join(' · ')}</Text> : null}</View>
    </View>
    <View style={styles.prescription}><Prescription label="Séries" value={String(exercise.sets)} /><Prescription label="Repetições" value={`${exercise.repetitionsMin}–${exercise.repetitionsMax}`} /><Prescription label="Descanso" value={`${exercise.restSeconds}s`} /></View>
    {exercise.instructions ? <Detail label="INSTRUÇÕES" value={exercise.instructions} /> : null}
    {exercise.notes ? <Detail label="OBSERVAÇÕES DO PERSONAL" value={exercise.notes} /> : null}
  </Card>;
}

function Prescription({ label, value }: { label: string; value: string }) { return <View style={styles.prescriptionItem}><Text style={styles.prescriptionLabel}>{label}</Text><Text style={styles.prescriptionValue}>{value}</Text></View>; }
function Detail({ label, value }: { label: string; value: string }) { return <View style={styles.detail}><Text style={styles.detailLabel}>{label}</Text><Text style={styles.copy}>{value}</Text></View>; }

const styles = StyleSheet.create({ page: { paddingVertical: spacing.xl, gap: spacing.lg }, headingMeta: { flexDirection: 'row', alignItems: 'center', gap: spacing.sm }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 21 }, intro: { ...typography.bodyLG, color: colors.titaniumLight, lineHeight: 24 }, meta: { ...typography.caption, color: colors.titanium }, exerciseCard: { gap: spacing.md }, exerciseHeader: { flexDirection: 'row', alignItems: 'center', gap: spacing.md }, exerciseImage: { width: 88, height: 88, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, imageFallback: { width: 88, height: 88, borderRadius: radius.sm, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.surfaceElevated }, sequence: { ...typography.headingMD, color: colors.primary }, identity: { flex: 1, gap: spacing.xxs }, title: { ...typography.headingMD, color: colors.textPrimary }, context: { ...typography.caption, color: colors.titanium }, prescription: { flexDirection: 'row', gap: spacing.xs }, prescriptionItem: { flex: 1, gap: spacing.xxs, padding: spacing.sm, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated }, prescriptionLabel: { ...typography.caption, color: colors.textMuted }, prescriptionValue: { ...typography.bodyLG, color: colors.textPrimary }, detail: { gap: spacing.xs, paddingTop: spacing.xs, borderTopWidth: 1, borderTopColor: colors.border }, detailLabel: { ...typography.caption, color: colors.primary, letterSpacing: .6 } });
