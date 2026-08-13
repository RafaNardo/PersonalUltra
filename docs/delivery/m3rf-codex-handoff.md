# Codex handoff — M3RF Rotina flexível

Leia `AGENTS.md`, `docs/design/flexible-training-routine.md`,
`docs/design/student-training-ux-restoration.md`, `docs/architecture/domain.md`,
`docs/architecture/api.md` e a seção M3RF do backlog antes de cada gate.

## Estratégia

Cada item é um gate independente: implementar, revisar, testar, commitar e
enviar antes do próximo. Não agrupar toda a refatoração num único diff.

```text
001  ordem sugerida aditiva + backfill
002  Trainer neutro + reordenação
003  Student API neutra + preparação/seleção
004  escolha livre de exercício
005  guided/offline/retomada flexíveis
006  calendário factual
007  remoção legada + E2E/polish
```

`PU-M3RF-001` foi entregue com backfill determinístico de `SuggestedOrder` por
Student. O gate final removeu os campos transitórios de agenda do domínio,
contratos, seed e banco; a ordem sugerida permanece como o único conceito de
ordenação da prescrição.

## Regras invariantes

- Trainer e Student APIs continuam separadas e compartilham o mesmo domínio e banco;
- ordem sugerida não é calendário nem obrigação;
- criação/aplicação acrescenta ao fim de forma determinística;
- Student não altera a prescrição persistida ao trocar ordem de execução;
- preview não inicia sessão;
- sessões e performances históricas permanecem reconstruíveis;
- offline é isolado por Student e idempotente;
- calendário usa somente sessões reais;
- não introduzir recomendação automática, frequência ou dados mockados;
- todo gate de UI cobre loading/error/empty/acessibilidade e lista compacta.

## Validação por gate

- build da solução .NET;
- testes direcionados e suíte de integração completa quando contratos mudarem;
- mobile typecheck;
- export Expo iOS ou Android;
- `git diff --check`;
- revisão de imports Trainer/Student e ausência de dados fabricados.

## Delegação sugerida

- `001`, `004`, `005` e `007`: agente de maior capacidade, por envolverem migração, estado de sessão/offline ou remoção transversal;
- `002`, `003` e `006`: agente intermediário pode implementar com este handoff, seguido de revisão rigorosa pelo agente principal.

O agente principal planeja, fornece o gate exato, revisa o diff, executa os gates
de validação, corrige pendências, faz commit/push e só então dispara o próximo.
