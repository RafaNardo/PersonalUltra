# Roadmap

## M0 — Walking Skeleton
Repo, API, Postgres, EF, demo seed, dev auth, Home, today workout, exercise, set logging, rest timer and workout completion.

## M1 — Demo
Branding real, Progress, Nutrition, Coach, exercise substitution, Safety flow, analytics/crash reporting and iOS/Android demo builds.

## M2 — Alpha

### M2-A — Entrada e Plano Inicial (sem IA/RAG)

1. **M2-A-1 — Identidade por e-mail e bootstrap**: reconhecer e-mail existente ou novo, criar sessão de demonstração por membro e direcionar corretamente para login, onboarding, preparação de plano ou Home.
2. **M2-A-2 — Onboarding e perfil persistido**: etapas retomáveis para objetivo, experiência, rotina, equipamentos, dados físicos, saúde/limitações, nutrição e revisão final.
3. **M2-A-3 — Provisionamento do plano padrão**: criar de forma idempotente um plano, treino, agenda e nutrição próprios para cada novo membro, usando a metodologia e o catálogo padrão; sem usar IA e sem apagar dados globais.
4. **M2-A-4 — Preparação e apresentação do plano**: experiência mobile de estados motivadores, loading mínimo visual, tratamento de erro/retry e resumo do plano antes da Home.
5. **M2-A-5 — Recomeçar demonstração**: permitir que o membro autenticado apague apenas os próprios dados de demonstração, encerre a sessão e volte ao login para refazer o fluxo.

O M2-A cria a fundação para geração real futura, mas não inclui LLM, RAG, recomendações clínicas, personalização automatizada do plano ou revisão automática. O plano inicial é uma cópia versionada da estrutura padrão; a IA futura poderá substituir somente o provisionador, preservando identidade, onboarding, versões e ciclos de plano.

### Próximos blocos M2

Health profile aprofundado, geração real de plano, progression rules, check-ins, scheduling, fotos, auth oficial, privacidade e sandbox de assinatura.

## M3 — Beta
50–100 convidados. Medir ativação, adesão, retenção, Coach usage e falhas reais.

## M4 — Launch
Billing real, stores, hardening, suporte, legal/privacy, backups e lançamento.
