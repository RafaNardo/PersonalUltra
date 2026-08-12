# Design System v0.1

## Direção
Escuro, forte, premium e esportivo. Preto/cinza/branco como base e vermelho SVR como ação. Consulte também `brand-guidelines.md` para assets, fontes e licença.

## Tipografia
- `display`: Montserrat ExtraBold para títulos, números, métricas e chamadas; os demais pesos da Montserrat são usados na interface. MonumentExtended-UltraBold é alternativa autorizada documentada em `brand-guidelines.md`
- `interface`: corpo, formulários e chat

A fonte oficial será substituída por token: `fontFamily.display = SVR_FONT`.

## Tokens
Colors: Background, Surface, SurfaceElevated, Primary, PrimaryPressed, TextPrimary, TextSecondary, TextMuted, Success, Warning, Danger.

Spacing: `4, 8, 12, 16, 20, 24, 32, 40, 48`.

Typography: displayXL, displayLG, headingLG, headingMD, bodyLG, bodyMD, caption, metricXL.

## Navegação
Home, Treino, Coach, Nutrição, Progresso. Durante treino, fluxo full-screen.

## Componentes
PrimaryButton, SecondaryButton, GhostButton, MetricCard, WorkoutCard, ExerciseCard, MealCard, CoachInsightCard, ProgressCard, BottomNavigation, TopBar, ProgressBar, SetRow, NumericStepper, RestTimer, Tag, PainBadge, Modal, BottomSheet, HealthWarning, EmptyState, Skeleton, ActionProposal.

`ActionProposal` apresenta tipo, motivo, nível de segurança e necessidade de confirmação. É informativo: não deve conter controles que executem, confirmem ou rejeitem alterações.

## UX
Durante treino: operacional, poucos dados, alvos grandes. Fora do treino: emocional, evolução, metodologia e coaching.
