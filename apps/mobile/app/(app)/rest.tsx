import { router, useLocalSearchParams } from 'expo-router';
import { useEffect, useState } from 'react';
import { StyleSheet, Text, View } from 'react-native';
import { Button, ProgressBar, Tag } from '@/src/components/ui';
import { Screen } from '@/src/components/layout';
import { colors, spacing, typography } from '@/src/design/tokens';
import { useTrainingStore } from '@/src/state/training-store';

export default function RestScreen() {
  const { sessionId, exerciseId, seconds, queued } = useLocalSearchParams<{ sessionId: string; exerciseId: string; seconds: string; queued?: string }>();
  const initial = Number(seconds) || 90;
  const [remaining, setRemaining] = useState(initial);
  const setRestSeconds = useTrainingStore((state) => state.setRestSeconds);
  useEffect(() => { setRestSeconds(remaining); if (remaining <= 0) return; const timer = setInterval(() => setRemaining((value) => Math.max(0, value - 1)), 1000); return () => clearInterval(timer); }, [remaining, setRestSeconds]);
  const minutes = Math.floor(remaining / 60).toString().padStart(2, '0'); const secs = (remaining % 60).toString().padStart(2, '0');
  const back = () => router.replace(`/(app)/exercise/${sessionId}/${exerciseId}`);
  return <Screen scroll={false} style={styles.screen}><View style={styles.top}>{queued === 'true' && <Tag tone="success">Série salva localmente</Tag>}<Text style={styles.label}>DESCANSO</Text></View><View style={styles.timerGroup}><View style={styles.timerRing}><Text style={styles.timer}>{minutes}:{secs}</Text><Text style={styles.timerCaption}>RECUPERAÇÃO</Text></View><Text style={styles.copy}>{remaining ? 'Respire. A próxima série começa com intenção.' : 'Você está pronto para a próxima série.'}</Text><ProgressBar value={remaining / initial} /></View><View style={styles.actions}><Button onPress={back}>{remaining ? 'Pular descanso' : 'Voltar para exercício'}</Button>{remaining > 0 && <Button variant="secondary" onPress={() => setRemaining((value) => value + 30)}>+30 segundos</Button>}</View></Screen>;
}

const styles = StyleSheet.create({ screen: { justifyContent: 'space-between', paddingVertical: spacing.xxl }, top: { gap: spacing.sm }, label: { ...typography.caption, color: colors.primary, letterSpacing: 2 }, timerGroup: { gap: spacing.lg, alignItems: 'center' }, timerRing: { width: 218, height: 218, borderRadius: 109, borderWidth: 8, borderColor: colors.primary, borderLeftColor: colors.surfaceElevated, alignItems: 'center', justifyContent: 'center', backgroundColor: colors.surface }, timer: { ...typography.displayXL, fontSize: 56, lineHeight: 64, color: colors.textPrimary }, timerCaption: { ...typography.caption, color: colors.textMuted, letterSpacing: 1, marginTop: spacing.xs }, copy: { ...typography.bodyLG, color: colors.textSecondary, textAlign: 'center', maxWidth: 280 }, actions: { gap: spacing.md },
});
