# Rotina flexível de treino

Status: direção aprovada para `M3RF`.

Fundação entregue em `PU-M3RF-001`: `StudentWorkout` já persiste
`SuggestedOrder`, contratos Trainer/Student o expõem de forma aditiva e novas
criações recebem o próximo índice. A UI e os leitores ainda usam campos legados
até os gates próprios; isso evita uma troca transversal não faseada.

## Problema

`Ativo`, `recomendado` e dia da semana misturam disponibilidade, destaque e
agenda. Na rotina real, o personal entrega um conjunto de treinos, mas o aluno
decide quando executá-los e pode mudar a ordem dos exercícios quando um aparelho
está ocupado. O calendário deve registrar fatos, não cobrar uma agenda que o
produto não controla.

## Modelo mental aprovado

- todo treino visível está disponível; `ATIVO` não é informação de interface;
- nenhum treino recebe destaque obrigatório ou algoritmo de recomendação;
- o Trainer define apenas uma **ordem sugerida** para a lista de treinos;
- não há dia ou frequência semanal na prescrição V1;
- o Student escolhe qual treino executar hoje;
- dentro da sessão, a ordem prescrita orienta, mas qualquer exercício pendente
  pode ser escolhido;
- a troca de ordem durante uma sessão não reescreve a prescrição;
- calendário e progresso usam sessões reais iniciadas/concluídas.

## Trainer

O detalhe do aluno mostra `Treinos disponíveis`, ordenados pelo personal. Cada
linha contém nome, quantidade de exercícios, notas úteis e ação explícita. Não
mostrar `ATIVO`, `RECOMENDADO`, segunda-feira ou equivalentes.

Criar do zero e aplicar modelo acrescentam o treino ao fim da ordem atual. A
interface oferece controles determinísticos para reorganizar a ordem sugerida.
Não é necessário drag-and-drop se mover para cima/baixo for mais confiável.

## Entrada Student

A Home possui um CTA claro, `Iniciar treino` (ou `Continuar treino` quando houver
sessão aberta). Iniciar sem sessão aberta leva a uma preparação intermediária:

1. título acolhedor explicando que o aluno pode escolher o treino de hoje;
2. texto curto confirmando que a ordem foi organizada pelo personal, não
   imposta por calendário;
3. lista compacta, nunca cards do tamanho da tela;
4. nome, exercícios, séries prescritas e última execução quando existirem;
5. `Ver treino` abre preview sem iniciar sessão;
6. início continua sendo confirmação explícita no preview.

## Sessão e exercícios

A visão geral da sessão é o ponto de escolha. Ela mostra todos os exercícios e
seus estados `pendente`, `em andamento` ou `concluído`. A ordem do personal
permanece visível; o próximo pendente nessa ordem pode receber o rótulo
`PRÓXIMO SUGERIDO`, sem bloquear os demais.

O Student pode abrir qualquer exercício pendente. Ao concluir suas séries, o
app volta à visão geral ou oferece continuar pelo próximo sugerido. O histórico
registra performances reais; não modifica `Sequence` da prescrição ou snapshot.

Retomada e SQLite devem identificar a próxima série por exercício, não por uma
posição global rígida. Sincronização continua idempotente e isolada por Student.

## Calendário factual

O calendário da Home marca apenas dias com sessão real. Ao tocar ou ler um dia,
o Student vê treino executado e estado real. Não renderizar treinos futuros por
`RecommendedDay`, `Sem treino prescrito` ou linguagem semelhante.

Uma sessão em andamento pode aparecer no dia em que foi iniciada. Sessões
concluídas usam seu registro real. Alterar a escolha do treino não exige editar
calendário.

## Estados e linguagem

- sem treinos: explicar que o personal ainda não disponibilizou a rotina;
- sem sessões: calendário acolhedor, sem sugerir falha ou atraso;
- sessão aberta: priorizar `Continuar treino`;
- lista longa: padrão compacto e busca somente se volume justificar;
- loading, erro e empty state seguem `docs/design/design-system.md`;
- ações e listas seguem `docs/design/list-patterns.md`.

## Fora de escopo

- frequência sugerida;
- agenda recorrente;
- recomendação automática por fadiga, tempo ou metodologia;
- Student alterar exercícios, séries ou ordem persistida;
- calendário planejado;
- push/lembretes;
- recommended load.
