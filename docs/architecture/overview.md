# Architecture Overview

```text
                    PostgreSQL
                        |
            PersonalUltra.Infrastructure
                        |
              PersonalUltra.Application
                        |
                PersonalUltra.Domain
                 /               \
          TrainerApi          StudentApi
```

## Backend
- um banco;
- um DbContext;
- um domínio;
- duas APIs;
- contratos e autorização por actor.

## Mobile demo
```text
apps/mobile
src/
  features/
    trainer/
    student/
  api/
    trainer-client.ts
    student-client.ts
    shared-http.ts
  shared/
```

## Mobile futuro
```text
apps/
  trainer-mobile/
  student-mobile/
```

### Regra de design para o split
Cada feature específica de actor deve ser autocontida.
`trainer/*` não importa `student/*` e vice-versa.
Compartilhar primitives, não telas completas ou estado de negócio específico.

O objetivo é que o split físico futuro exija mover composição, navegação e bootstrap, sem reescrever as features centrais.
