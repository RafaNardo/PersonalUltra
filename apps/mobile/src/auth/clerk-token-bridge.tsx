import { useAuth } from '@clerk/expo';
import { useEffect } from 'react';
import { setClerkTokenProvider } from './clerk-token';

export function ClerkTokenBridge() {
  const { isSignedIn, getToken } = useAuth();

  useEffect(() => {
    setClerkTokenProvider(async () => isSignedIn ? getToken({ template: 'personal-ultra-api' }) : null);
    return () => setClerkTokenProvider(undefined);
  }, [getToken, isSignedIn]);

  return null;
}
