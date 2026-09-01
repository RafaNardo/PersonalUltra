# AGENTS.md — Personal Ultra

A fonte principal de instruções para agentes está no `AGENTS.md` da raiz.

## Resumo obrigatório
- Demo: um app mobile com role switching.
- Futuro: `trainer-mobile` e `student-mobile` separados.
- Backend: `TrainerApi` e `StudentApi` separados, compartilhando Domain/Application/Infrastructure/PostgreSQL.
- Features de Trainer e Student não devem importar umas às outras.
- Role switching é somente conveniência de demo e nunca fonte de autorização.
- Chat V1 é humano entre Trainer e Student; sem IA ou automação.
- Não implementar milestones futuras por antecipação.

Antes de cada task, leia também os documentos citados no backlog correspondente.
