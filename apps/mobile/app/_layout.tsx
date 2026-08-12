import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Stack } from 'expo-router';
import { useEffect, useState } from 'react';
import { StatusBar } from 'react-native';
import { useFonts } from 'expo-font';
import * as SplashScreen from 'expo-splash-screen';
import { initializeTrainingDatabase } from '@/src/features/student/offline/training-db';
import { telemetry } from '@/src/platform/telemetry';
import { AppErrorBoundary } from '@/src/components/app-error-boundary';

void SplashScreen.preventAutoHideAsync();
SplashScreen.setOptions({ duration: 350, fade: true });

export default function RootLayout() {
  const [fontsLoaded, fontError] = useFonts({ MontserratRegular: require('../assets/brand/Montserrat-Regular.ttf'), MontserratMedium: require('../assets/brand/Montserrat-Medium.ttf'), MontserratSemiBold: require('../assets/brand/Montserrat-SemiBold.ttf'), MontserratBold: require('../assets/brand/Montserrat-Bold.ttf'), MontserratExtraBold: require('../assets/brand/Montserrat-ExtraBold.ttf') });
  const [databaseReady, setDatabaseReady] = useState(false);
  useEffect(() => {
    const previousHandler = ErrorUtils.getGlobalHandler();
    const handler = (error: Error, isFatal?: boolean) => {
      telemetry.error(error, { scope: 'unhandled', fatal: Boolean(isFatal) });
      previousHandler(error, isFatal);
    };
    ErrorUtils.setGlobalHandler(handler);
    return () => { if (ErrorUtils.getGlobalHandler() === handler) ErrorUtils.setGlobalHandler(previousHandler); };
  }, []);
  const [queryClient] = useState(() => new QueryClient({ defaultOptions: { queries: { retry: 1, staleTime: 15_000 } } }));
  useEffect(() => { void initializeTrainingDatabase().finally(() => setDatabaseReady(true)); }, []);
  useEffect(() => { if (databaseReady && (fontsLoaded || fontError)) void SplashScreen.hideAsync(); }, [databaseReady, fontError, fontsLoaded]);

  if (!databaseReady || (!fontsLoaded && !fontError)) return null;

  return <AppErrorBoundary><QueryClientProvider client={queryClient}>
    <StatusBar barStyle="light-content" />
    <Stack screenOptions={{ headerShown: false, animation: 'fade' }} />
  </QueryClientProvider></AppErrorBoundary>;
}
