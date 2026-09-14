import { useUser } from "@clerk/expo";
import { useMutation } from "@tanstack/react-query";
import { router } from "expo-router";
import { useState } from "react";
import { StyleSheet, Text, TextInput, View } from "react-native";
import { Button, Card } from "@/src/components/ui";
import { Screen } from "@/src/components/layout";
import { colors, spacing, typography } from "@/src/design/tokens";
import { inviteApi } from "@/src/features/student/invite/api";
import { useInviteSessionStore } from "@/src/features/student/invite/session-store";

export default function StudentOnboardingScreen() {
  const { user } = useUser();
  const save = useInviteSessionStore((state) => state.save);
  const [code, setCode] = useState("");
  const [firstName, setFirstName] = useState(user?.firstName ?? "");
  const [lastName, setLastName] = useState(user?.lastName ?? "");
  const preview = useMutation({
    mutationFn: () => inviteApi.previewInvite(code),
  });
  const claim = useMutation({
    mutationFn: () => inviteApi.claimInvite({ code, firstName, lastName }),
    onSuccess: (result) => {
      if (!result.studentId) return;
      save({
        accessToken: "",
        studentId: result.studentId,
        firstName,
        lastName,
        phone: "",
        email: user?.primaryEmailAddress?.emailAddress ?? "",
        trainerId: "",
      });
      router.replace("/student-access");
    },
  });

  const changeCode = () => {
    preview.reset();
    setCode("");
  };

  return (
    <Screen style={styles.page}>
      <View style={styles.hero}>
        <Text style={styles.eyebrow}>PRIMEIRO PASSO</Text>
        <Text style={styles.title}>
          Digite o código enviado pelo seu personal.
        </Text>
        <Text style={styles.copy}>
          Antes de concluir, vamos confirmar quem acompanhará você.
        </Text>
      </View>
      {!preview.data ? (
        <Card style={styles.card}>
          <Text style={styles.label}>Código de convite</Text>
          <TextInput
            value={code}
            onChangeText={(value) =>
              setCode(value.replace(/\D/g, "").slice(0, 6))
            }
            keyboardType="number-pad"
            placeholder="Ex.: 123456"
            placeholderTextColor={colors.textMuted}
            accessibilityLabel="Código de convite"
            style={styles.input}
          />
          {preview.error ? (
            <Text accessibilityRole="alert" style={styles.error}>
              {preview.error.message}
            </Text>
          ) : null}
          <Button
            loading={preview.isPending}
            disabled={code.length !== 6}
            onPress={() => preview.mutate()}
          >
            Continuar
          </Button>
          <Button variant="ghost" onPress={() => router.back()}>
            Voltar
          </Button>
        </Card>
      ) : (
        <Card style={styles.card}>
          <Text style={styles.confirmation}>
            Você está se cadastrando como aluno de {preview.data.trainerName},
            certo?
          </Text>
          <Text style={styles.copy}>
            Confira seus dados antes de confirmar o vínculo.
          </Text>
          <Text style={styles.label}>Seu nome</Text>
          <TextInput
            value={firstName}
            onChangeText={setFirstName}
            placeholder="Seu nome"
            placeholderTextColor={colors.textMuted}
            accessibilityLabel="Seu nome"
            style={styles.input}
          />
          <Text style={styles.label}>
            Sobrenome <Text style={styles.optional}>(opcional)</Text>
          </Text>
          <TextInput
            value={lastName}
            onChangeText={setLastName}
            placeholder="Seu sobrenome"
            placeholderTextColor={colors.textMuted}
            accessibilityLabel="Seu sobrenome"
            style={styles.input}
          />
          {claim.error ? (
            <Text accessibilityRole="alert" style={styles.error}>
              {claim.error.message}
            </Text>
          ) : null}
          <Button
            loading={claim.isPending}
            disabled={!firstName.trim()}
            onPress={() => void claim.mutateAsync()}
          >
            Sim, continuar
          </Button>
          <Button variant="ghost" onPress={changeCode}>
            Usar outro código
          </Button>
        </Card>
      )}
    </Screen>
  );
}
const styles = StyleSheet.create({
  page: { justifyContent: "center", gap: spacing.xxl },
  hero: { gap: spacing.md },
  eyebrow: { ...typography.caption, color: colors.primary, letterSpacing: 1.4 },
  title: { ...typography.displayLG, color: colors.textPrimary },
  copy: { ...typography.bodyMD, color: colors.textSecondary },
  confirmation: {
    ...typography.headingMD,
    color: colors.textPrimary,
    lineHeight: 26,
  },
  card: { gap: spacing.md },
  label: { ...typography.bodyLG, color: colors.textPrimary },
  optional: { color: colors.textMuted },
  input: {
    ...typography.bodyMD,
    color: colors.textPrimary,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
    paddingHorizontal: spacing.md,
    paddingVertical: spacing.md,
  },
  error: { ...typography.bodyMD, color: colors.danger },
});
