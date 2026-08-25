# Milestone proposta — substituições manuais de alimentos

Status: planejada. Não iniciar automaticamente sem uma task explícita.

## Objetivo

Permitir que o Trainer registre alternativas explícitas para um alimento de uma
refeição, sem o produto calcular equivalências ou sugerir alterações sozinho.

Exemplo: para `Carne vermelha — 150 g`, o Trainer pode informar `Peixe — 200 g`
como alternativa. O Student consulta essa opção junto ao item original.

## Escopo

- Cada `MealFood` pode conter zero ou mais substituições ordenadas.
- Uma substituição contém somente `FoodName`, `Quantity`, `Unit`, `Sequence` e
  `Notes` opcional.
- O Trainer cria, edita, remove e reordena substituições no editor individual
  da refeição.
- O Student permanece read-only e expande “Alternativas possíveis” somente
  quando houver conteúdo cadastrado.
- `NutritionTemplateFood` também carrega substituições. Aplicar um preset copia
  todos os itens e suas alternativas para um snapshot independente do plano do
  Student.
- O card da refeição no Trainer pode resumir a existência de alternativas, mas
  não precisa repetir todas elas na lista principal.

## Contrato e persistência

Adicionar duas entidades relacionais no mesmo `DbContext` compartilhado:

```text
MealFood
  -> MealFoodAlternative[]

NutritionTemplateFood
  -> NutritionTemplateFoodAlternative[]
```

As duas APIs continuam separadas por ator. Trainer API aceita e devolve as
alternativas dentro do documento nutricional e dos presets; Student API devolve
somente o plano do Student autenticado. Não criar API, banco ou modelo duplicado
por ator.

A validação do documento continua atômica. Para cada substituição: nome
obrigatório com até 200 caracteres, unidade obrigatória com até 40, quantidade
positiva até 10000 (exceto `livre`, que preserva a quantidade técnica 1), nota
até 1000 e sequência positiva/distinta. Limitar a dez alternativas por alimento
mantém o editor e a consulta legíveis na demo.

## Experiência

No Trainer, cada item ganha uma ação secundária “Adicionar alternativa”. A área
fica recolhida até existir pelo menos uma alternativa e usa o mesmo padrão de
adição, edição, remoção e ordenação de itens de refeição. A copy deve dizer
“alternativa cadastrada pelo personal”, não “equivalente nutricional”.

No Student, o alimento original permanece a instrução principal. Um bloco
recolhido “Alternativas possíveis” lista nome, quantidade, unidade e observação
que o Trainer informou. Não existe ação de marcar troca, registro de adesão ou
alteração local do plano.

## Non-goals

- cálculo de macros, calorias ou equivalência;
- geração automática por IA;
- catálogo global de alimentos;
- recomendação clínica, validação nutricional ou alegação de equivalência;
- substituição automática do item original;
- diário alimentar, adesão ou notificações.

## Critérios de aceite

- Trainer consegue cadastrar `Carne vermelha 150 g` com `Peixe 200 g` e nota
  opcional, editar/reordenar/remover a alternativa e salvar sem alterar os
  outros itens/refeições.
- O round-trip Trainer → Student preserva a ordem e o texto das alternativas.
- Aplicar preset copia as alternativas; editar o preset depois não altera a
  cópia já aplicada.
- Falha de validação não modifica o plano ou preset existentes.
- Student não recebe mutation para alimentação nem pode alterar alternativas.
- São validados testes de API, typecheck mobile e export Expo; os estados sem
  alternativas não exibem controles ou espaços vazios.

## Limite de produto

Esta milestone continua sob o mesmo limite demo e jurídico de
`docs/product/nutrition-note.md`: alternativas são conteúdo manual do Trainer,
sem cálculo e sem afirmar habilitação profissional ou adequação clínica.
