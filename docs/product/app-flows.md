# App Flows

## Bootstrap
- sem login → Login
- onboarding incompleto → retomar
- onboarding completo sem plano → Gerar Plano
- plano ativo → Home

## Treino
`Home → Treino → Iniciar → Exercício → Série → Descanso → ... → Finalizar → Resumo → Home`

## Troca de exercício
`Exercício → motivo → alternativas válidas → confirmação → substituição`

## Safety
`Dor → contexto → Safety Engine → Green / Yellow / Red`

## Nutrição
`Nutrição → Refeição → Alimento → Alternativas → NutritionEngine → Aplicar`

## Coach
`Mensagem → Intent → Context Router → LLM → Tool Request → Application → Engine → Safety → Proposta → Confirmação → Persistência`

## Revisão do plano
`ReviewDate → métricas → PlanReviewEngine → proposta → aplicar → nova versão`

## Offline
Operações críticas ganham `clientOperationId`, são gravadas localmente e sincronizadas de forma idempotente.
