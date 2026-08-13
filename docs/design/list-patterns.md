# Listagens — Personal Ultra

Listagens devem permitir reconhecer, comparar e abrir itens rapidamente. Na
experiência Trainer, onde dezenas de alunos, treinos ou modelos podem coexistir,
densidade útil é parte da usabilidade: uma linha não deve virar um card alto sem
que seu conteúdo exija essa altura.

## Hierarquia padrão

Uma linha de navegação usa, nesta ordem:

1. título identificador, com no máximo duas linhas;
2. metadado curto e comparável, em uma linha;
3. descrição opcional, somente quando ajuda a decidir antes de abrir;
4. faixa inferior com badge opcional à esquerda;
5. ação textual curta e chevron à direita quando a linha abre outra tela.

O título nunca divide a mesma linha horizontal com o badge. Assim nomes longos
mantêm a largura do card e o estado permanece associado ao item, em uma faixa
inferior previsível. Em telas estreitas, a faixa inferior pode quebrar sem
fragmentar o nome.

O toque vale para toda a linha. A ação textual não é um segundo botão: ela deixa
explícito o destino do toque. Use verbos de produto como `Ver detalhes`, `Abrir
treinos` ou `Escolher`.

## Primitive compartilhado

Use `ListItem`, em `apps/mobile/src/components/ui.tsx`, nas listas lineares de
navegação e gestão. Ele oferece área de toque consistente, feedback de pressão,
hierarquia tipográfica, affordance e semântica de acessibilidade. Use
`SearchField` quando a coleção puder crescer e a busca já existir no fluxo.

- `title`: identidade principal;
- `metadata`: dado curto para comparação;
- `description`: contexto opcional, limitado a duas linhas;
- `badge`: `Tag` com estado real, nunca decoração;
- `actionLabel` e `onPress`: destino explícito;
- `accessibilityLabel` e `accessibilityHint`: descrevem a ação, não a aparência.

Linhas somente informativas podem omitir `onPress`; nesse caso, não exibem
chevron ou linguagem de ação.

## Densidade e conteúdo

- use espaço de 4 px entre linhas e altura mínima de 76 px;
- não coloque um botão grande dentro de cada item quando tocar a linha tem um
  único destino;
- evite repetir no subtítulo o que já está no badge;
- preserve truncamento previsível para nomes, e-mails e observações longas;
- um badge fica na faixa inferior e nunca disputa largura com o título;
- busca filtra a coleção já carregada quando isso não altera regra de negócio;
- busca sem resultados usa o padrão de empty state e oferece limpar o filtro.

## Quando não usar a linha compacta

Algumas coleções têm interação própria e devem preservar uma variante dedicada:

- catálogo visual de exercícios, onde imagem e grupo muscular orientam escolha;
- editor ordenável, que precisa expor editar, remover e mover;
- revisão de prescrição, com configurações antes de confirmar;
- histórico detalhado, que agrupa sessão, exercícios e séries;
- dashboards métricos, onde os cards representam indicadores e não destinos.

Mesmo nessas variantes, título, metadado, feedback de toque e ação explícita
devem seguir a mesma linguagem visual.

## Estados obrigatórios

Toda listagem deve considerar:

- loading sem simular conteúdo;
- erro com tentativa real de recarregar;
- coleção vazia com o `EmptyState` adequado;
- busca sem resultado distinta de coleção vazia;
- conteúdo longo e tela pequena;
- item desabilitado com motivo compreensível;
- feedback de pressão e leitura por tecnologia assistiva.

## Auditoria Trainer

O padrão compacto é aplicado a alunos, alunos recentes, alunos escolhidos para
prescrição, biblioteca de modelos, escolha de modelo e treinos atuais do aluno.
Atividade recente usa a variante informativa. Catálogos, exercícios editáveis,
revisão do modelo e histórico permanecem como variantes funcionais conforme as
exceções acima.
