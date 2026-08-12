# AGENTS.md — Personal Ultra

## Prioridade
Este projeto começa como **product demo**. Priorize UX, fluxo completo e velocidade de validação; não antecipe produção sem task explícita.

## Backend obrigatório
Existem duas API surfaces:
- `TrainerApi`
- `StudentApi`

Ambas compartilham:
- `PersonalUltra.Domain`
- `PersonalUltra.Application`
- `PersonalUltra.Infrastructure`
- um `DbContext`
- um PostgreSQL

Não criar microserviços, bancos separados ou modelos duplicados.

## Mobile obrigatório
Na demo existe **um app Expo com role switching**.
No futuro haverá dois apps físicos:
- `trainer-mobile`
- `student-mobile`

A demo deve ser modelada para que o split futuro seja uma extração, não uma reescrita.

Regras:
- separar `features/trainer`, `features/student` e `shared`;
- proibir imports cruzados entre features de atores;
- criar `trainer-client` e `student-client` separados;
- não usar o role switch como regra de autorização;
- evitar hooks/componentes com branches extensos por role;
- manter árvores de navegação independentes;
- manter estado de negócio específico dentro da feature do actor;
- compartilhar primitives, não telas inteiras apenas para reduzir duplicação pequena.

## Coach V1
Read-only.

Não pode:
- alterar treino;
- alterar alimentação;
- recomendar carga;
- gerar plano;
- executar mutation tools.

## Não implementar sem task explícita
- billing;
- push notification real;
- WhatsApp API;
- auth production-ready;
- microserviços;
- Redis/Kubernetes;
- AI workout generation;
- avaliações físicas avançadas;
- fotos de progresso;
- app white-label separado por personal.

## Regra Codex
Implemente apenas a task solicitada. Leia os docs relevantes. Preserve fronteiras Trainer/Student e a splitabilidade futura. Rode testes e typecheck. Se uma task exigir mudança de arquitetura não documentada, reporte a ambiguidade antes de alterar a fronteira.
