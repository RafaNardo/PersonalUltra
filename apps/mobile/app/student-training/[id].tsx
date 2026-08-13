import { Redirect, useLocalSearchParams } from 'expo-router';

/** Compatibility redirect for pre-M3RR workout deep links. */
export default function LegacyStudentTrainingSessionRoute() {
  const { id } = useLocalSearchParams<{ id: string }>();
  return <Redirect href={{ pathname: '/student/training/[id]', params: { id: id ?? '' } }} />;
}
