import { router } from 'expo-router';
import { useEffect, useRef } from 'react';
import { Animated, Image, StyleSheet, Text, View } from 'react-native';
import { useBootstrap } from '@/src/api/hooks';
import { useAuthStore } from '@/src/state/auth-store';
import { colors, typography } from '@/src/design/tokens';
import { useDemoRoleStore } from '@/src/state/demo-role-store';

export default function BootstrapScreen() {
  const role = useDemoRoleStore((state) => state.role);
  const accessToken = useAuthStore((state) => state.accessToken);
  const hasHydrated = useAuthStore((state) => state.hasHydrated);
  const bootstrap = useBootstrap();
  const opacity = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    if (!role) { router.replace('/demo-role-switch'); return; }
    if (role === 'trainer') { router.replace('/trainer'); return; }
    if (!hasHydrated) return;
    if (!accessToken) { router.replace('/login'); return; }
    if (bootstrap.data?.nextRoute === 'Home') router.replace('/student/home');
    if (bootstrap.data?.nextRoute === 'Onboarding') router.replace('/onboarding');
    if (bootstrap.data?.nextRoute === 'PreparePlan') router.replace('/prepare-plan');
  }, [accessToken, bootstrap.data?.nextRoute, hasHydrated, role]);
  useEffect(() => { Animated.timing(opacity, { toValue: 1, duration: 280, useNativeDriver: true }).start(); }, [opacity]);

  return <View style={styles.splash}><Animated.View style={[styles.brandGroup, { opacity }]}><Image source={require('../assets/brand/personal-ultra-logo-horizontal.png')} resizeMode="contain" style={styles.logo} /><Text style={styles.tagline}>Performance com acompanhamento.</Text></Animated.View></View>;
}

const styles = StyleSheet.create({ splash: { flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: colors.background }, brandGroup: { alignItems: 'center' }, logo: { width: 220, height: 100 }, tagline: { ...typography.bodyMD, color: colors.textMuted, marginTop: 24, textAlign: 'center' } });
