# Guia visual — artes de exercícios

## Objetivo

Gerar uma biblioteca coesa, legível em cards pequenos e útil para reconhecer o exercício. A arte não substitui instrução profissional e não deve fingir precisão clínica. Consistência entre 200 imagens importa tanto quanto a qualidade isolada de uma imagem.

## Baseline observado no app

- ilustração digital esportiva, com contornos definidos;
- fundo escuro cinza/preto, discreto e sem ambiente poluído;
- personagem e equipamento com alto contraste;
- paleta majoritariamente preta/cinza com acentos quentes;
- uma pose principal, sem texto editorial, moldura ou watermark; somente o
  pequeno wordmark `ULTRA` é permitido na roupa;
- maioria dos assets atuais em 1024 × 1536 (vertical);
- o app usa `resizeMode="cover"` em cards largos, portanto o elemento essencial precisa sobreviver ao crop central.

## Style token

O style prompt não deve ser reescrito livremente por exercício. Ele é uma versão imutável, por exemplo `personal-ultra-illustration-v1`, combinada com a descrição aprovada do movimento.

Elementos fixos sugeridos:

```text
Ilustração digital fitness premium, anatomia humana natural e proporcional,
contornos nítidos, iluminação cinematográfica suave, fundo de academia escuro
e desfocado, roupa esportiva usando somente preto #080808, grafite/titânio
#151515/#222220 e laranja #FF6A13, com pequeno wordmark ULTRA,
equipamento correto e totalmente reconhecível, um único atleta, composição
central, sem outras marcas, sem watermark, sem interface, sem colagem.
```

O fundo permanece premium e escuro, porém levemente mais claro e legível, com
equipamentos visíveis e preenchimento neutro suave. O style vigente é
`personal-ultra-exercise-image-v2`.

No piloto enxuto, os elementos variáveis vêm somente do catálogo canônico:

- exercício e variação exata;
- grupo muscular quando disponível;
- alternância determinística entre atleta mulher e homem;
- equipamento e variação expressos no próprio nome canônico.

## Template de prompt

```text
[STYLE TOKEN]

Exercício: {canonicalName}.
Equipamento: {equipment}.
Representar: {approvedVisualDescription}.
Fase: uma posição estável e reconhecível do movimento, sem setas ou texto.
Enquadramento: corpo e equipamento centralizados; ação principal dentro dos
60% centrais para continuar legível após crop horizontal; mãos, pés, articulações
e pontos de contato essenciais visíveis.

Evitar: anatomia extra ou deformada, membros ocultos necessários para entender
o movimento, equipamento incorreto, carga impossível, pegada incoerente,
ambiente claro, multidão, texto, marca, watermark, diagrama ou múltiplas fases.
```

O prompt final e sua versão devem ser guardados no manifesto.

## Representação humana

A decisão de personagem precisa ser tomada antes do lote pago:

- personagem consistente em todo o catálogo; ou
- alternância determinística e equilibrada definida no manifesto; ou
- visual mais neutro/anônimo.

Não deixar o provider decidir aleatoriamente a cada item. Variar corpos pode ser positivo, mas precisa de uma regra editorial consistente, não de drift acidental.

## Formato e área segura

O piloto usa PNG 1024 × 1024, formato adequado aos cards e econômico para a
primeira validação. Outros formatos e derivados só serão avaliados depois.

Área segura:

- ação/equipamento principal dentro dos 60% centrais da imagem;
- cabeça, tronco e articulações críticas não devem depender das bordas;
- conferir thumbnail de card e crop horizontal 2:1;
- o master vertical deve continuar disponível para futuras superfícies.

Uma folha de revisão pode ser adicionada futuramente. No piloto, o operador olha
diretamente os PNGs locais considerando:

1. imagem completa;
2. crop 2:1 semelhante ao hero do app;
3. thumbnail semelhante ao card em grade;
4. nome, slug, equipamento e versão.

## QA automático mínimo do piloto

Bloquear ou sinalizar:

- assinatura/formato diferente de PNG;
- arquivo vazio/corrompido;
- alpha inesperado quando o profile exige fundo opaco;
- arquivo acima do limite configurado;
- hash igual em exercícios diferentes;
- nome/path não correspondente ao manifesto;
- imagem existente sendo sobrescrita ou regenerada silenciosamente.

QA automático não confirma biomecânica.

## Revisão humana obrigatória

Para cada arte, um revisor deve confirmar:

- exercício visualmente identificável;
- equipamento e sua configuração corretos;
- número e posição de mãos/pés/membros plausíveis;
- pegada, apoio e amplitude representada coerentes;
- ausência de postura obviamente perigosa ou impossível;
- anatomia sem deformações;
- roupa e fundo dentro do padrão;
- leitura boa no crop e thumbnail;
- ausência de texto, marca e artefatos.

Uma pessoa com conhecimento técnico de exercício deve dar a aprovação biomecânica. Aprovação estética isolada não libera export.

## Regeneração

Regenerar somente o item rejeitado. O feedback humano deve virar um adendo estruturado, por exemplo:

```text
Correção obrigatória: manter ambos os pés inteiramente apoiados; barra alinhada
sobre o meio do pé; mostrar o banco horizontal completo.
```

Cada tentativa recebe `contentVersion`/`attempt`. Não apagar o artefato anterior até o novo ser aprovado e exportado.

## Piloto antes do lote de 200

O piloto deve cobrir pelo menos:

- peso corporal;
- barra livre;
- halteres;
- cabo;
- máquina;
- exercício unilateral;
- exercício sentado/deitado;
- membros superiores e inferiores.

Só congelar `styleVersion` depois que o piloto for visto nos cards e heroes reais do app. Alterar estilo após gerar 200 itens deve ser uma decisão consciente, com plano de invalidação e custo.
