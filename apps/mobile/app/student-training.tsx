import { Redirect } from 'expo-router';

/** Compatibility redirect for the pre-M3RR Student route. */
export default function LegacyStudentTrainingRoute() {
  return <Redirect href="/student/training" />;
}
