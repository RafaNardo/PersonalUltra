import { router } from 'expo-router';
import { useEffect } from 'react';

export default function StudentEntryScreen() {
  useEffect(() => { router.replace('/'); }, []);
  return null;
}
