import { router } from 'expo-router';
import { useState } from 'react';
import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { useQueryClient } from '@tanstack/react-query';
import { api, ApiError } from '@/src/features/student/api/client';
import { useAuthStore } from '@/src/features/student/state/auth-store';
import { Button, Card } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { feedback } from '@/src/platform/feedback';
import { telemetry } from '@/src/platform/telemetry';

const sides = ['Esquerdo', 'Direito', 'Bilateral'] as const;

export default function PainScreen() {
  const token = useAuthStore((state) => state.accessToken)!;
  const queryClient = useQueryClient();
  const [area, setArea] = useState('');
  const [side, setSide] = useState<(typeof sides)[number] | null>(null);
  const [intensity, setIntensity] = useState<number | null>(null);
  const [context, setContext] = useState('');
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  async function submit() {
    const normalizedArea = area.trim();
    const normalizedContext = context.trim();
    if (!normalizedArea || !side || intensity === null || !normalizedContext) {
      setError('Preencha região, lado, intensidade e contexto para registrar a dor.');
      return;
    }

    setError(null);
    setSuccess(null);
    setSending(true);
    try {
      const result = await api.reportPain(token, normalizedArea, side, intensity, normalizedContext);
      feedback.warning();
      telemetry.event('pain_reported');
      void queryClient.invalidateQueries({ queryKey: ['coach'] });
      setSuccess(result.message);
    } catch (requestError) {
      setError(requestError instanceof ApiError ? requestError.message : 'Não foi possível registrar a dor. Tente novamente.');
    } finally {
      setSending(false);
    }
  }

  return <Screen><TopBar eyebrow="Segurança primeiro" title="Registrar dor" onBack={() => router.replace('/student/coach')} />
    <Card style={styles.card}>
      <Text style={styles.copy}>Registre o que sentiu. Nenhuma alteração no treino será feita automaticamente.</Text>
      <Field label="Onde dói?" value={area} onChangeText={setArea} maxLength={100} placeholder="Ex.: joelho" />
      <View style={styles.field}><Text style={styles.label}>Qual lado?</Text><View style={styles.options}>{sides.map((option) => <Pressable key={option} accessibilityRole="radio" accessibilityLabel={option} accessibilityState={{ selected: side === option }} onPress={() => { setSide(option); setError(null); }} style={({ pressed }) => [styles.option, side === option && styles.optionSelected, pressed && styles.pressed]}><Text style={[styles.optionText, side === option && styles.optionTextSelected]}>{option}</Text></Pressable>)}</View></View>
      <View style={styles.field}><Text style={styles.label}>Intensidade de 0 a 10</Text><View style={styles.intensity}>{Array.from({ length: 11 }, (_, value) => <Pressable key={value} accessibilityRole="radio" accessibilityLabel={`Intensidade ${value} de 10`} accessibilityState={{ selected: intensity === value }} onPress={() => { setIntensity(value); setError(null); }} style={({ pressed }) => [styles.intensityOption, intensity === value && styles.intensitySelected, pressed && styles.pressed]}><Text style={[styles.intensityText, intensity === value && styles.intensityTextSelected]}>{value}</Text></Pressable>)}</View></View>
      <Field label="Em qual contexto?" value={context} onChangeText={setContext} maxLength={500} placeholder="Ex.: durante o agachamento" multiline />
      {error && <Text accessibilityRole="alert" style={styles.error}>{error}</Text>}
      {success && <View accessibilityLiveRegion="polite" style={styles.success}><Text style={styles.successTitle}>Registro enviado</Text><Text style={styles.successCopy}>{success}</Text></View>}
      <Button loading={sending} onPress={() => void submit()}>Registrar dor</Button>
    </Card>
  </Screen>;
}

function Field({ label, value, onChangeText, maxLength, placeholder, multiline = false }: { label: string; value: string; onChangeText: (value: string) => void; maxLength: number; placeholder: string; multiline?: boolean }) {
  return <View style={styles.field}><Text style={styles.label}>{label}</Text><TextInput value={value} onChangeText={onChangeText} maxLength={maxLength} multiline={multiline} accessibilityLabel={label} accessibilityHint={`Campo obrigatório, até ${maxLength} caracteres`} placeholder={placeholder} placeholderTextColor={colors.textMuted} style={[styles.input, multiline && styles.multiline]} /><Text style={styles.counter}>{value.length}/{maxLength}</Text></View>;
}

const styles = StyleSheet.create({
  card: { gap: spacing.md }, copy: { ...typography.bodyMD, color: colors.warning, lineHeight: 21 }, field: { gap: spacing.xs }, label: { ...typography.caption, color: colors.textSecondary }, input: { minHeight: 50, borderRadius: radius.sm, backgroundColor: colors.surfaceElevated, paddingHorizontal: spacing.md, color: colors.textPrimary, ...typography.bodyMD }, multiline: { minHeight: 92, paddingTop: spacing.sm, textAlignVertical: 'top' }, counter: { ...typography.caption, color: colors.textMuted, alignSelf: 'flex-end' }, options: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm }, option: { flexGrow: 1, minHeight: 44, alignItems: 'center', justifyContent: 'center', paddingHorizontal: spacing.sm, borderRadius: radius.sm, borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surfaceElevated }, optionSelected: { backgroundColor: '#4D1520', borderColor: colors.primary }, optionText: { ...typography.bodyMD, color: colors.textSecondary }, optionTextSelected: { color: colors.textPrimary, fontFamily: 'MontserratSemiBold' }, intensity: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.xs }, intensityOption: { width: 34, height: 34, borderRadius: 17, alignItems: 'center', justifyContent: 'center', borderWidth: 1, borderColor: colors.border, backgroundColor: colors.surfaceElevated }, intensitySelected: { borderColor: colors.primary, backgroundColor: colors.primary }, intensityText: { ...typography.caption, color: colors.textSecondary }, intensityTextSelected: { color: colors.textPrimary, fontFamily: 'MontserratBold' }, pressed: { opacity: .78 }, error: { ...typography.bodyMD, color: colors.danger }, success: { gap: spacing.xs, padding: spacing.md, borderRadius: radius.sm, backgroundColor: '#123D2B' }, successTitle: { ...typography.bodyLG, color: colors.success, fontFamily: 'MontserratSemiBold' }, successCopy: { ...typography.bodyMD, color: colors.textPrimary, lineHeight: 20 },
});
