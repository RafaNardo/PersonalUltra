# AGENTS.md — SVR Method

## Regras arquiteturais
1. Leia os docs relevantes antes de alterar arquitetura.
2. Implemente apenas a task solicitada do backlog.
3. Não introduza microsserviços, Redis, GraphQL, Redux, Kubernetes ou infraestrutura extra sem necessidade comprovada.
4. Backend: ASP.NET Core + PostgreSQL + EF Core.
5. Mobile: React Native + Expo + TypeScript.
6. API REST versionada em `/api/v1`.
7. DTOs não expõem entidades EF diretamente.
8. Server state no TanStack Query; client state no Zustand.
9. Treino deve funcionar offline via SQLite.

## Princípios de IA
1. O LLM não é fonte de verdade do domínio.
2. Method Rules são a fonte de verdade.
3. Engines retornam decisões estruturadas.
4. Safety valida antes de mutações.
5. Mudanças materiais exigem confirmação.
6. Ambiguidade de saúde deve favorecer segurança.
7. Toda decisão material deve ser explicável.
8. O LLM nunca escreve diretamente no banco.
9. Treino e nutrição não dependem de cálculos inventados pelo LLM.

## Execução
- Identifique a task em `delivery/backlog.md`.
- Leia os documentos relacionados.
- Faça um plano curto.
- Implemente só o escopo da task.
- Rode testes relevantes.
- Reporte ambiguidades antes de criar decisões arquiteturais novas.
