import { useAuth } from '@clerk/expo';
import { Redirect } from 'expo-router';
import { LoadingView } from '@/src/components/ui';

export default function Entry() {
  const { isLoaded, isSignedIn } = useAuth();
  if (!isLoaded) return <LoadingView message="Carregando sua sessão…" />;
  return <Redirect href={isSignedIn ? '/auth-choice' : '/login'} />;
}
