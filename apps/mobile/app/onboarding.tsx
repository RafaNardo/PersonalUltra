import { router } from 'expo-router';
import { useEffect, useState } from 'react';
import { Alert, Keyboard, Pressable, StyleSheet, Text, TextInput, View } from 'react-native';
import { useCompleteOnboarding, useOnboardingProfile, useSaveOnboardingProfile } from '@/src/features/student/api/hooks';
import type { OnboardingProfile, SaveOnboardingProfile } from '@/src/features/student/api/types';
import { Button, Card, ErrorView, LoadingView, ProgressBar } from '@/src/components/ui';
import { Screen, TopBar } from '@/src/components/layout';
import { colors, radius, spacing, typography } from '@/src/design/tokens';
import { useAuthStore } from '@/src/features/student/state/auth-store';

const stepCount = 8;
const goals = ['Ganhar massa muscular', 'Reduzir gordura', 'Fortalecer glúteos e pernas', 'Voltar à rotina', 'Melhorar condicionamento'];
const experienceOptions = ['Estou começando agora', 'Menos de 2 anos', 'Entre 2 e 5 anos', '6 anos ou mais'];
const noHealth = 'Nenhuma informada';
const noPain = 'Sem dor atual';

const empty: OnboardingProfile = { firstName: '', lastName: '', goal: '', experienceLevel: '', trainingDaysPerWeek: 0, sessionDurationMinutes: 0, trainingLocation: '', equipmentNotes: '', heightCm: 0, weightKg: 0, healthConditions: '', movementRestrictions: '', currentPainDescription: '', nutritionPreferences: '', nutritionRestrictions: '', currentStep: 1, isCompleted: false };

function Choice({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
  return <Pressable accessibilityRole="button" accessibilityState={{ selected }} accessibilityLabel={label} onPress={onPress} style={({ pressed }) => [styles.choice, selected && styles.choiceSelected, pressed && styles.choicePressed]}><Text style={[styles.choiceText, selected && styles.choiceTextSelected]}>{label}</Text></Pressable>;
}

function YesNo({ question, selected, onYes, onNo }: { question: string; selected: boolean | undefined; onYes: () => void; onNo: () => void }) {
  return <View style={styles.question}><Text style={styles.label}>{question}</Text><View style={styles.choiceRow}><Choice label="Não" selected={selected === false} onPress={onNo} /><Choice label="Sim" selected={selected === true} onPress={onYes} /></View></View>;
}

export default function OnboardingScreen() {
  const profile = useOnboardingProfile();
  const save = useSaveOnboardingProfile();
  const complete = useCompleteOnboarding();
  const signIn = useAuthStore((state) => state.signIn);
  const [form, setForm] = useState<OnboardingProfile>(empty);
  const [step, setStep] = useState(1);
  const [details, setDetails] = useState({ health: undefined as boolean | undefined, movement: undefined as boolean | undefined, pain: undefined as boolean | undefined, nutrition: undefined as boolean | undefined });

  useEffect(() => {
    if (!profile.data) return;
    setForm(profile.data);
    setStep(Math.max(1, Math.min(stepCount, profile.data.currentStep)));
    setDetails({
      health: profile.data.healthConditions ? profile.data.healthConditions !== noHealth : undefined,
      movement: profile.data.movementRestrictions ? profile.data.movementRestrictions !== noHealth : undefined,
      pain: profile.data.currentPainDescription ? profile.data.currentPainDescription !== noPain : undefined,
      nutrition: profile.data.nutritionRestrictions ? profile.data.nutritionRestrictions !== noHealth : undefined,
    });
  }, [profile.data]);

  const update = <K extends keyof OnboardingProfile>(field: K, value: OnboardingProfile[K]) => setForm((previous) => ({ ...previous, [field]: value }));
  const payload = (currentStep: number): SaveOnboardingProfile => ({
    ...form,
    trainingDaysPerWeek: form.trainingDaysPerWeek || undefined,
    sessionDurationMinutes: form.sessionDurationMinutes || undefined,
    heightCm: form.heightCm || undefined,
    weightKg: form.weightKg || undefined,
    currentStep,
  });
  const selectedGoals = form.goal ? form.goal.split('; ') : [];
  const toggleGoal = (goal: string) => update('goal', selectedGoals.includes(goal) ? selectedGoals.filter((item) => item !== goal).join('; ') : [...selectedGoals, goal].join('; '));
  const setAnswer = (kind: keyof typeof details, hasDetails: boolean, field: 'healthConditions' | 'movementRestrictions' | 'currentPainDescription' | 'nutritionRestrictions') => {
    setDetails((previous) => ({ ...previous, [kind]: hasDetails }));
    update(field, hasDetails ? '' : field === 'currentPainDescription' ? noPain : noHealth);
  };
  const requireSelection = (valid: boolean, message: string) => {
    if (valid) return true;
    Alert.alert('Falta uma escolha', message);
    return false;
  };
  const canContinue = () => {
    if (step === 1) return requireSelection(Boolean(form.firstName.trim()), 'Informe como podemos te chamar.');
    if (step === 2) return requireSelection(selectedGoals.length > 0, 'Escolha pelo menos um objetivo.');
    if (step === 3) return requireSelection(Boolean(form.experienceLevel), 'Escolha há quanto tempo você treina.');
    if (step === 4) return requireSelection(form.trainingDaysPerWeek > 0 && form.sessionDurationMinutes > 0, 'Escolha seus dias disponíveis e a duração do treino.');
    if (step === 5) return requireSelection(Boolean(form.trainingLocation) && Boolean(form.equipmentNotes), 'Escolha onde você treina e a estrutura disponível.');
    if (step === 6) return requireSelection(form.heightCm > 0 && form.weightKg > 0, 'Informe sua altura e seu peso aproximados.');
    if (step === 7) return requireSelection(details.health !== undefined && details.movement !== undefined && details.pain !== undefined && Boolean(form.healthConditions) && Boolean(form.movementRestrictions) && Boolean(form.currentPainDescription), 'Responda às três perguntas de cuidados e limitações.');
    return requireSelection(Boolean(form.nutritionPreferences) && details.nutrition !== undefined && Boolean(form.nutritionRestrictions), 'Escolha quantas refeições costuma fazer e informe se há restrições.');
  };
  const next = async () => {
    if (!canContinue()) return;
    const nextStep = Math.min(step + 1, stepCount);
    try {
      await save.mutateAsync(payload(nextStep));
      Keyboard.dismiss();
      setStep(nextStep);
    } catch (error) { Alert.alert('Não foi possível salvar', error instanceof Error ? error.message : 'Tente novamente.'); }
  };
  const finish = async () => {
    if (!canContinue()) return;
    try {
      await save.mutateAsync(payload(stepCount));
      const completed = await complete.mutateAsync();
      signIn(useAuthStore.getState().accessToken!, completed.firstName);
      router.replace('/prepare-plan');
    } catch (error) { Alert.alert('Ainda falta uma informação', error instanceof Error ? error.message : 'Revise seus dados e tente novamente.'); }
  };
  const reviewRows = [['Objetivos', form.goal], ['Experiência', form.experienceLevel], ['Rotina', `${form.trainingDaysPerWeek} dias · ${form.sessionDurationMinutes} min`], ['Local', `${form.trainingLocation} · ${form.equipmentNotes}`], ['Dados físicos', `${form.heightCm} cm · ${form.weightKg} kg`], ['Cuidados', form.healthConditions], ['Limitações', form.movementRestrictions], ['Dor atual', form.currentPainDescription], ['Nutrição', `${form.nutritionPreferences} · ${form.nutritionRestrictions}`]];

  if (profile.isLoading) return <LoadingView message="Preparando seu onboarding…" />;
  if (profile.isError) return <ErrorView message={profile.error.message} onRetry={() => profile.refetch()} />;

  const content = () => {
    switch (step) {
      case 1: return <><Text style={styles.title}>Vamos nos conhecer</Text><Text style={styles.copy}>Como podemos te chamar?</Text><Input label="Seu nome" value={form.firstName} onChangeText={(value) => update('firstName', value)} autoCapitalize="words" /><Input label="Sobrenome (opcional)" value={form.lastName} onChangeText={(value) => update('lastName', value)} autoCapitalize="words" /></>;
      case 2: return <><Text style={styles.title}>Seu objetivo</Text><Text style={styles.copy}>Você pode escolher mais de uma prioridade.</Text><View style={styles.choiceGrid}>{goals.map((goal) => <Choice key={goal} label={goal} selected={selectedGoals.includes(goal)} onPress={() => toggleGoal(goal)} />)}</View></>;
      case 3: return <><Text style={styles.title}>Sua experiência</Text><Text style={styles.copy}>Há quanto tempo você treina com regularidade?</Text><View style={styles.choiceGrid}>{experienceOptions.map((option) => <Choice key={option} label={option} selected={form.experienceLevel === option} onPress={() => update('experienceLevel', option)} />)}</View></>;
      case 4: return <><Text style={styles.title}>Sua rotina</Text><Text style={styles.copy}>Escolha os dias que normalmente consegue dedicar ao treino.</Text><Text style={styles.label}>Dias disponíveis por semana</Text><View style={styles.numberGrid}>{[1, 2, 3, 4, 5, 6, 7].map((days) => <Choice key={days} label={String(days)} selected={form.trainingDaysPerWeek === days} onPress={() => update('trainingDaysPerWeek', days)} />)}</View><Text style={styles.label}>Tempo por treino</Text><View style={styles.choiceGrid}>{[30, 45, 60, 75, 90].map((minutes) => <Choice key={minutes} label={`${minutes} min`} selected={form.sessionDurationMinutes === minutes} onPress={() => update('sessionDurationMinutes', minutes)} />)}</View></>;
      case 5: return <><Text style={styles.title}>Onde você treina?</Text><Text style={styles.copy}>Isso define a estrutura padrão que vamos registrar.</Text><View style={styles.choiceGrid}>{['Academia', 'Casa'].map((location) => <Choice key={location} label={location} selected={form.trainingLocation === location} onPress={() => { update('trainingLocation', location); update('equipmentNotes', ''); }} />)}</View>{form.trainingLocation && <><Text style={styles.label}>{form.trainingLocation === 'Academia' ? 'Como é a academia?' : 'Como é a estrutura em casa?'}</Text><View style={styles.choiceGrid}>{(form.trainingLocation === 'Academia' ? ['Academia completa', 'Academia com poucos equipamentos'] : ['Casa com equipamentos', 'Casa com poucos equipamentos']).map((option) => <Choice key={option} label={option} selected={form.equipmentNotes === option} onPress={() => update('equipmentNotes', option)} />)}</View></>}</>;
      case 6: return <><Text style={styles.title}>Dados físicos</Text><Text style={styles.copy}>Use valores aproximados. Você poderá atualizá-los depois.</Text><Input label="Altura em cm" value={form.heightCm ? String(form.heightCm) : ''} onChangeText={(value) => update('heightCm', Number(value.replace(',', '.')) || 0)} keyboardType="decimal-pad" /><Input label="Peso em kg" value={form.weightKg ? String(form.weightKg) : ''} onChangeText={(value) => update('weightKg', Number(value.replace(',', '.')) || 0)} keyboardType="decimal-pad" /></>;
      case 7: return <><Text style={styles.title}>Cuidados e limitações</Text><Text style={styles.copy}>Essas informações são registradas para sua jornada e não substituem orientação profissional.</Text><YesNo question="Há alguma condição de saúde relevante?" selected={details.health} onYes={() => setAnswer('health', true, 'healthConditions')} onNo={() => setAnswer('health', false, 'healthConditions')} />{details.health && <Input label="Conte-nos qual condição" value={form.healthConditions} onChangeText={(value) => update('healthConditions', value)} multiline />}<YesNo question="Existe alguma limitação de movimento?" selected={details.movement} onYes={() => setAnswer('movement', true, 'movementRestrictions')} onNo={() => setAnswer('movement', false, 'movementRestrictions')} />{details.movement && <Input label="Conte-nos qual limitação" value={form.movementRestrictions} onChangeText={(value) => update('movementRestrictions', value)} multiline />}<YesNo question="Você sente dor atualmente?" selected={details.pain} onYes={() => setAnswer('pain', true, 'currentPainDescription')} onNo={() => setAnswer('pain', false, 'currentPainDescription')} />{details.pain && <Input label="Onde e como é essa dor?" value={form.currentPainDescription} onChangeText={(value) => update('currentPainDescription', value)} multiline />}</>;
      default: return <><Text style={styles.title}>Sua alimentação</Text><Text style={styles.copy}>Quantas refeições você costuma fazer por dia?</Text><View style={styles.numberGrid}>{[3, 4, 5, 6].map((meals) => <Choice key={meals} label={String(meals)} selected={form.nutritionPreferences === `${meals} refeições por dia`} onPress={() => update('nutritionPreferences', `${meals} refeições por dia`)} />)}</View><YesNo question="Você possui alguma restrição ou alergia alimentar?" selected={details.nutrition} onYes={() => setAnswer('nutrition', true, 'nutritionRestrictions')} onNo={() => setAnswer('nutrition', false, 'nutritionRestrictions')} />{details.nutrition && <Input label="Conte-nos qual restrição" value={form.nutritionRestrictions} onChangeText={(value) => update('nutritionRestrictions', value)} multiline />}</>;
    }
  };

  return <Screen>
    <TopBar eyebrow={`Etapa ${step} de ${stepCount}`} title="Seu ponto de partida" />
    <ProgressBar value={step / stepCount} />
    <Card style={styles.card}>{content()}</Card>
    {step === stepCount ? <><Card style={styles.review}><Text style={styles.reviewTitle}>Revise seu ponto de partida</Text>{reviewRows.map(([label, value]) => <View key={label} style={styles.reviewRow}><Text style={styles.reviewLabel}>{label}</Text><Text style={styles.reviewValue}>{value}</Text></View>)}</Card><View style={styles.actions}><Button variant="secondary" onPress={() => setStep(step - 1)}>Voltar</Button><Button loading={save.isPending || complete.isPending} onPress={finish}>Confirmar e continuar</Button></View></> : <View style={styles.actions}>{step > 1 && <Button variant="secondary" onPress={() => setStep(step - 1)}>Voltar</Button>}<Button loading={save.isPending} onPress={next}>Continuar</Button></View>}
  </Screen>;
}

function Input({ label, value, onChangeText, keyboardType, autoCapitalize = 'sentences', multiline = false }: { label: string; value: string; onChangeText: (value: string) => void; keyboardType?: 'default' | 'decimal-pad'; autoCapitalize?: 'sentences' | 'words'; multiline?: boolean }) {
  return <View style={styles.field}><Text style={styles.label}>{label}</Text><TextInput value={value} onChangeText={onChangeText} keyboardType={keyboardType ?? 'default'} autoCapitalize={autoCapitalize} autoCorrect={keyboardType === undefined} spellCheck={keyboardType === undefined} multiline={multiline} placeholder={label} placeholderTextColor={colors.textMuted} style={styles.input} /></View>;
}

const styles = StyleSheet.create({
  card: { gap: spacing.md }, title: { ...typography.headingLG, color: colors.textPrimary }, copy: { ...typography.bodyMD, color: colors.textSecondary, lineHeight: 22 }, label: { ...typography.caption, color: colors.textSecondary }, field: { gap: spacing.xs }, input: { ...typography.bodyMD, color: colors.textPrimary, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, paddingHorizontal: spacing.md, paddingVertical: spacing.md, minHeight: 52, textAlignVertical: 'top' },
  choiceGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm }, numberGrid: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.sm }, choice: { minHeight: 48, borderWidth: 1, borderColor: colors.border, borderRadius: radius.md, paddingHorizontal: spacing.md, justifyContent: 'center', backgroundColor: colors.surfaceElevated }, choiceSelected: { borderColor: colors.primary, backgroundColor: '#4D1520' }, choicePressed: { opacity: .82 }, choiceText: { ...typography.bodyMD, color: colors.textSecondary }, choiceTextSelected: { color: colors.textPrimary, fontFamily: 'MontserratSemiBold' }, choiceRow: { flexDirection: 'row', gap: spacing.sm }, question: { gap: spacing.xs },
  actions: { flexDirection: 'row', gap: spacing.sm }, review: { gap: spacing.sm }, reviewTitle: { ...typography.headingMD, color: colors.textPrimary }, reviewRow: { gap: 2 }, reviewLabel: { ...typography.caption, color: colors.textMuted }, reviewValue: { ...typography.bodyMD, color: colors.textPrimary },
});
