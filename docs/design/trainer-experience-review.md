# Trainer Experience Review

Status: revisão-base para o refactor da M4.

## Premissa

A experiência Trainer precisa reduzir decisão e esforço, sem parecer um painel administrativo genérico. Em cada fluxo, o personal deve entender imediatamente o aluno em contexto, a finalidade da tela, a ação principal e o resultado esperado.

O contrato de experiência está em `docs/design/design-system.md`: contexto, significado, próximo passo, consequência e recuperação devem estar explícitos.

## Revisão das superfícies

### Aplicar modelo a um aluno

Problema identificado: o fluxo student-centric enviava o personal ao CRUD de modelos. A aplicação dependia de descobrir que o card abria o editor e competia visualmente com `Duplicar modelo`.

Direção aplicada:

- fluxo contextual separado em duas etapas;
- etapa 1 identifica o aluno e oferece `Escolher este modelo` em cada card;
- etapa 2 mostra exercícios, dia da semana e opção de destaque;
- CTA final nomeia o aluno e confirma a criação;
- biblioteca permanece dedicada a criar, editar e duplicar modelos.

### Biblioteca de modelos

O CRUD está funcional e deve continuar separado da aplicação ao aluno. Cards devem usar ações explícitas `Editar modelo` e `Duplicar modelo`; não depender apenas de toque implícito. Linguagem de `modelo reutilizável` é preferível a jargão como `acelerador`.

### Tab Treinos

A entrada continua student-centric. A seleção do aluno deve apresentar CTA explícito para abrir seus treinos, enquanto a biblioteca aparece como ferramenta secundária.

### Dashboard e lista de alunos

O estado atual já apresenta métricas reais, alunos recentes, busca, estados de anamnese e acesso a detalhes. No refactor da M4, revisar apenas hierarquia e prioridade:

- destacar pendências que exigem ação real;
- manter `Ver detalhes` explícito;
- não fabricar alertas ou métricas;
- preservar busca e empty states padronizados.

### Detalhe do aluno

A tela concentra resumo, anamnese, mensagem, treinos, histórico, alimentação e peso. É funcional, mas densa. O refactor da M4 deve melhorar a leitura sem mudar regras:

- identidade e estado do vínculo sempre visíveis;
- cada seção explica o que o personal pode fazer;
- ação principal contextual por seção;
- alimentação, progresso e histórico com loading/error/empty independentes;
- evitar duplicar CTAs quando o próprio empty state já oferece a ação.

### Convites

O fluxo de código e WhatsApp é compreensível. Manter a sequência `dados opcionais → gerar código → copiar/enviar`. A ação deve falar em `código de convite`, não `link`, e o resultado deve explicar com clareza o próximo passo do aluno.

### Alimentação Trainer

É a maior lacuna de experiência restante da M4: o formulário linear atual representa apenas uma refeição e um alimento por vez. O refactor deve seguir o domínio real já previsto, sem mocks:

- cabeçalho identifica o aluno;
- plano contém múltiplas refeições;
- refeição contém múltiplos alimentos e quantidades;
- adição/edição/remoção são explícitas;
- revisão do plano antes de salvar;
- empty state padronizado para plano/refeições ainda não criados;
- Student vê somente o plano persistido.

### Progresso Trainer

O detalhe atualmente resume o último peso. A M4 já prevê gráfico do Trainer; a experiência deve mostrar dados reais, período legível e estado sem medições, sem inferir tendências ou recomendações inexistentes.

## Critérios para o refactor M4

- aplicar o princípio de experiência e o guia de empty states;
- manter Trainer/Student separados e compartilhar somente primitives;
- preservar ações e dados reais já integrados;
- não transformar polish em nova regra de negócio;
- validar caminhos principal, vazio, erro e retorno de cada tela;
- confirmar em dispositivo/tela pequena que textos e CTAs permanecem visíveis.
