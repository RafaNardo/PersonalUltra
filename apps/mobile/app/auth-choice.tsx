import { useAuth, useUser } from "@clerk/expo";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { router } from "expo-router";
import { StyleSheet, Text, View } from "react-native";
import { useEffect } from "react";
import { Button, Card, ErrorView, LoadingView } from "@/src/components/ui";
import { Screen } from "@/src/components/layout";
import { colors, spacing, typography } from "@/src/design/tokens";
import { trainerClient } from "@/src/api/trainer-client";
import { inviteApi } from "@/src/features/student/invite/api";
import { useInviteSessionStore } from "@/src/features/student/invite/session-store";

export default function AuthChoiceScreen() {
  const { isSignedIn, signOut } = useAuth();
  const { user } = useUser();
  const queryClient = useQueryClient();
  const saveStudent = useInviteSessionStore((state) => state.save);
  const trainer = useQuery({
    queryKey: ["auth", "trainer-bootstrap", user?.id],
    queryFn: trainerClient.bootstrap,
    enabled: Boolean(isSignedIn && user?.id),
  });
  const student = useQuery({
    queryKey: ["auth", "student-bootstrap", user?.id],
    queryFn: inviteApi.bootstrap,
    enabled: Boolean(isSignedIn && user?.id),
  });
  useEffect(() => {
    if (trainer.data?.isOnboarded) router.replace("/trainer");
  }, [trainer.data?.isOnboarded]);
  useEffect(() => {
    if (!student.data?.isOnboarded || !student.data.studentId) return;
    saveStudent({
      accessToken: "",
      studentId: student.data.studentId,
      firstName: user?.firstName ?? "",
      lastName: user?.lastName ?? "",
      email: user?.primaryEmailAddress?.emailAddress ?? "",
      phone: "",
      trainerId: "",
    });
    router.replace("/student-access");
  }, [
    saveStudent,
    student.data?.isOnboarded,
    student.data?.studentId,
    user?.firstName,
    user?.lastName,
    user?.primaryEmailAddress?.emailAddress,
  ]);
  if (!isSignedIn) return null;
  if (trainer.isLoading || student.isLoading)
    return <LoadingView message="Preparando seu acesso…" />;
  if (trainer.isError || student.isError)
    return (
      <ErrorView
        message="Não foi possível validar sua conta."
        onRetry={() => {
          void trainer.refetch();
          void student.refetch();
        }}
      />
    );
  if (trainer.data?.isOnboarded)
    return <LoadingView message="Abrindo seu painel…" />;
  if (student.data?.isOnboarded)
    return <LoadingView message="Abrindo seu acompanhamento…" />;
  const handleSignOut = async () => {
    queryClient.clear();
    await signOut();
  };
  return (
    <Screen style={styles.page}>
      <View style={styles.hero}>
        <Text style={styles.eyebrow}>PRIMEIRO ACESSO</Text>
        <Text style={styles.title}>Como você vai usar o Personal Ultra?</Text>
        <Text style={styles.copy}>
          Uma conta fica vinculada a um único papel para proteger os dados do
          acompanhamento.
        </Text>
      </View>
      <Card style={styles.card}>
        <Button onPress={() => router.push("/student-onboarding")}>
          Sou aluno
        </Button>
        <Button
          variant="secondary"
          onPress={() => router.push("/trainer-onboarding")}
        >
          Sou personal
        </Button>
      </Card>
      <Button variant="ghost" onPress={() => void handleSignOut()}>
        Sair desta conta
      </Button>
    </Screen>
  );
}
const styles = StyleSheet.create({
  page: { justifyContent: "center", gap: spacing.xxl },
  hero: { gap: spacing.md },
  eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1.4 },
  title: { ...typography.displayLG, color: colors.textPrimary },
  copy: { ...typography.bodyMD, color: colors.textSecondary },
  card: { gap: spacing.md },
});
