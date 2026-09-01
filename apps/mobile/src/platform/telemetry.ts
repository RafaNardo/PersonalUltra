type EventProperties = Record<string, string | number | boolean>;
export type AnalyticsEvent =
  | 'demo_login_completed'
  | 'workout_started'
  | 'workout_completed'
  | 'workout_set_logged'
  | 'exercise_substitution_proposed'
  | 'meal_completed'
  | 'food_substitution_completed'
  | 'pain_reported'
  | 'weight_logged';

// Ponto único de integração: nenhum dado de saúde ou conteúdo do chat é enviado.
// Um provedor (Sentry/analytics) pode ser conectado aqui quando houver credenciais de produção.
export const telemetry = {
  event(name: AnalyticsEvent, properties: EventProperties = {}) { if (__DEV__) console.info(`[analytics] ${name}`, properties); },
  error(error: unknown, context: EventProperties = {}) {
    const category = error instanceof Error && error.name ? error.name : 'UnknownError';
    if (__DEV__) console.warn('[crash]', { category, ...context });
  },
};
