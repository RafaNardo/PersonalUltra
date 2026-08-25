# Revisão da experiência de alimentação

Status: direção implementável para a demo, revisada em 2026-08-24.

## Problema observado

O backend já persistia um plano com várias refeições, mas o editor Trainer
enviava sempre uma única refeição e um único alimento. Ao editar um plano
existente, todo o restante era removido sem aviso. A API também aceitava nomes
vazios, refeições sem itens e quantidades inválidas. No Student, a leitura era
real e read-only, porém não mostrava autoria/data e não tratava coleções
incompletas.

Não foram encontrados mocks no fluxo: Trainer e Student já consultavam o mesmo
`DbContext`. O seed do aluno principal é somente dado demonstrativo.

## Decisão demo-first

O plano continua sendo um documento único por Student e sua atualização é uma
substituição integral e atômica. A ação Trainer é explicitamente
`Salvar e disponibilizar`: depois de salvar, o Student consulta o mesmo estado
persistido. Não existe nesta etapa rascunho, versionamento ou agenda semanal.

A estrutura da demo é:

```text
Plano do Student
  -> refeições ordenadas
    -> itens ordenados (nome, quantidade e unidade)
```

O plano registra criação, última alteração e o Trainer responsável atual. A UI
não atribui ao Trainer título, licença ou habilitação profissional que o produto
não conhece.

## Experiência Trainer

- oferecer `Alimentação` na navegação inferior, com uma visão operacional dos
  alunos e do estado de seus planos;
- substituir a aba `Evolução` do detalhe do aluno por `Alimentação`, mantendo o
  resumo de peso dentro de `Resumo`;
- mostrar o plano atual em cards por refeição, no mesmo vocabulário visual do
  fluxo de treinos;
- adicionar cada refeição escolhendo entre `Usar preset de refeição` e
  `Criar manualmente`;
- editar cada card de refeição isoladamente e reorganizá-los com ações de subir
  e descer, mantendo a edição completa apenas como alternativa;
- manter o aluno identificado no cabeçalho;
- editar nome e observações gerais sem perder o conteúdo persistido;
- registrar, sem cálculo automático, calorias e macronutrientes diários que o
  Trainer decidir informar;
- adicionar, editar, remover e reordenar várias refeições;
- adicionar, editar, remover e reordenar vários itens em cada refeição;
- aceitar unidades simples (`g`, `ml`, `unidade`, `fatia`, `colher`, `dose` e
  `porção`) e a opção `livre` para itens sem quantidade fixa, sem introduzir
  catálogo nutricional;
- revisar toda a estrutura na mesma tela antes de salvar;
- explicar que salvar disponibiliza a atualização ao Student;
- preservar o draft local quando validação ou rede falhar.

### Presets

A biblioteca pertence ao Trainer e permite criar, editar, duplicar e excluir
presets de uma única refeição, por exemplo `Café com ovos`, `Café 2` ou `Café com
tapioca`. Aplicar um preset acrescenta uma cópia independente ao plano: não
substitui as outras refeições e mudanças posteriores no preset não afetam o
Student. Se ainda não existe plano, a primeira refeição cria sua estrutura
inicial; as seguintes são acrescentadas na ordem atual.

A biblioteca mostra até quatro alimentos em bullets por card e resume o
excedente, privilegiando leitura rápida sobre a contagem abstrata de itens. Um
novo preset sempre abre com formulário vazio. Retornos de escolha, biblioteca e
edição preservam explicitamente o contexto do aluno ou da área `Alimentação`,
sem depender do histórico da tab navigator.

A bottom nav Trainer usa a mesma escala de ícones, tipografia e destaque em
forma de cápsula da navegação Student, sem importar componentes entre features
de atores.

## Experiência Student

- permanecer estritamente read-only;
- mostrar plano, observações, refeições e itens na ordem persistida;
- mostrar Trainer responsável e data da última atualização, sem alegar
  credencial profissional;
- quando cadastradas pelo Trainer, apresentar metas diárias de calorias,
  proteínas, carboidratos e gorduras como referência motivacional, sem cálculo
  ou recomendação automática;
- distinguir carregamento, erro, ausência de plano, plano sem refeições e
  refeição sem itens;
- não exibir macros, metas, adesão ou recomendações que não foram cadastradas.

## Validação e limites

A API valida o documento inteiro antes de substituir o estado atual: nome do
plano, limites de texto, de 1 a 20 refeições, de 1 a 30 itens por refeição,
sequências positivas e distintas, quantidade positiva limitada e unidade
informada. Falha de validação não pode apagar o plano anterior.

Trainer API continua exigindo vínculo ativo com o Student. Student API expõe
somente o plano do Student autenticado e não oferece mutation de alimentação.

## Referências de UX e limite regulatório

Produtos atuais como
[Everfit](https://help.everfit.io/en/articles/9423729-client-app-structured-meal-plans),
[Trainerize](https://help.trainerize.com/hc/en-us/articles/360016477211-Create-a-meal-plan-using-the-Smart-Meal-Planner)
e [Practice Better](https://help.practicebetter.io/hc/en-us/articles/360052049372-Sharing-Protocols-With-Clients)
foram usados apenas como referência de hierarquia, agrupamento e clareza de
publicação. Seus recursos de macros, templates, calendário, tracking e versões
não são requisitos desta demo.

A UX não resolve habilitação clínica ou conformidade. A
[Lei 8.234/1991](https://www.planalto.gov.br/ccivil_03/leis/1989_1994/l8234.htm)
e a orientação do
[Conselho Federal de Nutrição](https://www.cfn.org.br/wp-content/uploads/2023/11/RESUMIDA_PRESCRI%C3%87%C3%83O-DIET%C3%89TICA.pdf)
exigem decisão jurídica/produto antes de oferecer prescrição dietética em
produção. Dados de saúde também recebem proteção específica na LGPD, conforme
a [ANPD](https://www.gov.br/anpd/pt-br/acesso-a-informacao/perguntas-frequentes/perguntas-frequentes).
Até essa revisão, a entrega permanece product demo, sem IA, cálculo clínico ou
alegação de habilitação.

## Fora do escopo

- catálogo de alimentos, cálculos ou recomendações automáticas;
- metas calculadas automaticamente;
- diário, adesão, fotos, scanner ou lista de compras;
- alternativas automáticas e geração por IA;
- rascunho/publicação separados, histórico de versões ou vigência;
- sincronização retroativa entre preset e refeições já aplicadas;
- credenciais profissionais e compliance production-ready.
