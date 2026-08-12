# SVR Method

Pacote de especificação do produto **SVR Method**.

## Visão
O SVR Method transforma a metodologia SVR em um acompanhamento fitness digital escalável. O usuário não compra apenas um app de treino: compra acesso a uma metodologia aplicada ao próprio objetivo, rotina, evolução e restrições.

> **A IA conversa. A metodologia decide.**

## Stack proposta
- Mobile: React Native + Expo SDK 54 + TypeScript
- Navegação: Expo Router
- Server state: TanStack Query
- Client state: Zustand
- Offline crítico: Expo SQLite
- Backend: ASP.NET Core
- Banco: PostgreSQL + EF Core
- Arquitetura: Modular Monolith
- API: REST `/api/v1`
- RAG futuro: PostgreSQL + pgvector
- Jobs: Hangfire
- Observabilidade: OpenTelemetry + logs estruturados

## Estrutura
- `product/` — visão, PRD, fluxos e MVP
- `design/` — design system e telas
- `architecture/` — técnica, frontend, banco e API
- `ai/` — IA, Method Engine, Safety e Coach
- `delivery/` — backlog e roadmap
- `assets/mockups/` — referências visuais
- `AGENTS.md` — instruções para agentes de código
