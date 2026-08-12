import { router } from 'expo-router';
import { useEffect, useRef } from 'react';
import { Animated, Image, StyleSheet, Text, View } from 'react-native';
import { useBootstrap } from '@/src/api/hooks';
import { useAuthStore } from '@/src/state/auth-store';
import { colors, typography } from '@/src/design/tokens';

export default function BootstrapScreen() {
  const accessToken = useAuthStore((state) => state.accessToken);
  const hasHydrated = useAuthStore((state) => state.hasHydrated);
  const bootstrap = useBootstrap();
  const opacity = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    if (!hasHydrated) return;
    if (!accessToken) { router.replace('/login'); return; }
    if (bootstrap.data?.nextRoute === 'Home') router.replace('/(app)/home');
    if (bootstrap.data?.nextRoute === 'Onboarding') router.replace('/onboarding');
    if (bootstrap.data?.nextRoute === 'PreparePlan') router.replace('/prepare-plan');
  }, [accessToken, bootstrap.data?.nextRoute, hasHydrated]);
  useEffect(() => { Animated.timing(opacity, { toValue: 1, duration: 280, useNativeDriver: true }).start(); }, [opacity]);

  return <View style={styles.splash}><Animated.View style={[styles.brandGroup, { opacity }]}><Image source={require('../assets/brand/svr-logo-transparent.png')} resizeMode="contain" style={styles.logo} /><Text style={styles.tagline}>Método, consistência, evolução.</Text></Animated.View></View>;
}

const styles = StyleSheet.create({ splash: { flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: colors.background }, brandGroup: { alignItems: 'center' }, logo: { width: 220, height: 100 }, tagline: { ...typography.bodyMD, color: colors.textMuted, marginTop: 24, textAlign: 'center' } });
