# Refinamento da execução de treino

## Objetivo

Fechar a experiência de execução sem tratar todo exercício como séries de
repetições. A prescrição continua sendo do Trainer e a ordem continua apenas
sugerida; o Student decide quando iniciar e pode executar exercícios pendentes
em outra ordem.

## Modos de acompanhamento

Cada exercício do catálogo define um modo padrão, copiado para o preset, para a
prescrição do aluno e para o snapshot da sessão:

- `Repetitions`: registra carga e repetições por série;
- `Duration`: registra duração real por bloco, em segundos.

O Trainer pode escolher o modo durante a configuração. Alterar catálogo ou
preset depois não modifica prescrições ou sessões já criadas. Cardio seeded e
isometrias conhecidas começam em `Duration`; os demais exercícios começam em
`Repetitions`.

## Execução Student

A imagem e a identidade do exercício permanecem estáveis na tela. Somente o
painel inferior alterna entre registro, descanso e escolha do próximo exercício.
Ao concluir o último registro do último exercício, a mesma tela apresenta a
confirmação final antes de abrir o resumo.

Imagens de exercício usam `contain` por padrão. Precisão do movimento tem
prioridade sobre preencher completamente o retângulo.

## Conclusão sem detalhamento

O Student pode confirmar um exercício inteiro ou todos os exercícios restantes.
Essa confirmação:

- exige ação e confirmação explícitas;
- fica indisponível no snapshot offline;
- persiste `ConfirmedCompletedAt` no snapshot do exercício;
- não cria `SetPerformance` fictícia;
- não inventa carga, repetições ou duração;
- aparece no histórico/resumo como conclusão sem detalhamento.

A conclusão detalhada continua derivada das performances persistidas. O endpoint
normal de conclusão ainda rejeita sessões incompletas; somente
`confirmRemaining=true` confirma explicitamente o restante.

## Offline e retomada

Registros detalhados mantêm a fila SQLite idempotente existente. O snapshot local
preserva o modo e a duração, e considera concluído um exercício com todas as
performances contíguas. Confirmações sem detalhamento dependem do servidor para
evitar uma conclusão ambígua que ainda não exista como fato compartilhado.
