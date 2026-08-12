# Implementation Backlog v0.1

## M0 — Walking Skeleton
- M0-001 Create monorepo
- M0-002 Local infrastructure (Docker + Postgres + health)
- M0-003 Initial EF Core model
- M0-004 SVR Demo Seed
- M0-005 Dev Authentication
- M0-006 Bootstrap API
- M0-007 Home API
- M0-008 Training Today API
- M0-009 Start Workout (idempotent)
- M0-010 Complete Set (`clientOperationId`)
- M0-011 Complete Workout
- M0-012 Design tokens
- M0-013 Core components
- M0-014 API client
- M0-015 Dev Login Screen
- M0-016 Bootstrap navigation
- M0-017 Home Screen
- M0-018 Workout Screen
- M0-019 Exercise Screen
- M0-020 Rest Timer
- M0-021 Complete Workout Screen

### Definition of Done M0
`open → demo login → Home → workout → exercise → log set → rest → finish → refreshed Home`

Status: concluído (incluindo infraestrutura local com Podman).

## M1 — Demo
- M1-001 Real SVR branding
- M1-002 Premium Splash
- M1-003 Progress API
- M1-004 Progress Screen
- M1-005 Weight Log
- M1-006 Nutrition DB
- M1-007 Nutrition Today API
- M1-008 Nutrition Screen
- M1-009 Meal Screen
- M1-010 Food Alternatives Engine v0
- M1-011 Food substitution UI
- M1-012 Coach Persistence
- M1-013 LLM abstraction
- M1-014 Coach Context Builder
- M1-015 Coach Base Chat
- M1-016 Structured Coach Output
- M1-017 Coach Mobile Screen
- M1-018 Exercise Alternative Engine
- M1-019 Coach Tool: Exercise Substitution
- M1-020 Pain Reporting
- M1-021 Safety Engine v0
- M1-022 Coach Pain Flow
- M1-023 ActionProposal Component
- M1-024 Confirm Coach Action
- M1-025 Haptics
- M1-026 Loading/Error/Empty States
- M1-027 Analytics
- M1-028 Crash Reporting
- M1-029 Demo Reset
- M1-030 iOS Demo Build
- M1-031 Android Demo Build

### Agent execution prompt
`Read AGENTS.md and the relevant documents. Implement task M0-XXX from delivery/backlog.md. Do not implement future tasks. Preserve documented architecture. Run relevant tests. Report ambiguities before making architecture changes.`

### Status M1 (demo)

Concluído: branding e splash, progresso/peso, nutrição/refeições/substituições, persistência e chat do Coach com saída estruturada, alternativas e confirmação de exercício, relato de dor e safety v0, haptics, estados de carregamento/erro, telemetria local, reset de demo e perfis EAS para iOS/Android. Os builds EAS precisam ser executados em uma conta com credenciais de assinatura configuradas; nenhum build remoto é disparado pelo repositório.

## M2-A — Entrada e Plano Inicial

- M2-A-1 Identidade por e-mail e Bootstrap
- M2-A-2 Onboarding e Perfil Persistido
- M2-A-3 Provisionamento Idempotente do Plano Padrão
- M2-A-4 Preparação e Apresentação do Plano
- M2-A-5 Recomeçar Demonstração do Membro Atual

### Definition of Done M2-A

`e-mail → nova conta ou sessão existente → onboarding retomável → revisão → plano padrão próprio provisionado → apresentação do plano → Home`

M2-A-5 é um fluxo explícito de demonstração: o reset remove somente os dados pertencentes ao membro autenticado, encerra a sessão no aplicativo e retorna ao login. Não pode apagar o catálogo global, a conta demo base ou dados de outros membros.

Fora do escopo: IA, RAG, geração individualizada, alteração automática do plano, avaliação clínica e revisão automática após 45 dias. A data de revisão pode ser registrada para uso futuro, mas não dispara automação neste bloco.
