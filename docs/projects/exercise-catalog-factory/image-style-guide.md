# Guia visual — artes de exercícios

## Objetivo

Gerar uma biblioteca coesa, legível em cards pequenos e útil para reconhecer o exercício. A arte não substitui instrução profissional e não deve fingir precisão clínica. Consistência entre 200 imagens importa tanto quanto a qualidade isolada de uma imagem.

## Baseline observado no app

- ilustração digital esportiva, com contornos definidos;
- fundo escuro cinza/preto, discreto e sem ambiente poluído;
- personagem e equipamento com alto contraste;
- paleta majoritariamente preta/cinza com acentos quentes;
- uma pose principal, sem texto, moldura, watermark ou logo;
- maioria dos assets atuais em 1024 × 1536 (vertical);
- o app usa `resizeMode="cover"` em cards largos, portanto o elemento essencial precisa sobreviver ao crop central.

## Style token

O style prompt não deve ser reescrito livremente por exercício. Ele é uma versão imutável, por exemplo `personal-ultra-illustration-v1`, combinada com a descrição aprovada do movimento.

Elementos fixos sugeridos:

```text
Ilustração digital fitness premium, anatomia humana natural e proporcional,
contornos nítidos, iluminação cinematográfica suave, fundo de academia escuro
e desfocado, roupa esportiva preta/cinza com pequeno acento laranja ou vermelho,
equipamento correto e totalmente reconhecível, um único atleta, composição
central, sem texto, sem logo, sem watermark, sem interface, sem colagem.
```

Elementos variáveis vêm somente dos metadados aprovados:

- exercício e variação exata;
- equipamento;
- posição corporal e fase representada;
- ângulo de câmera necessário para legibilidade;
- partes do corpo/equipamento que precisam permanecer visíveis.

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

Target inicial recomendado para compatibilidade: PNG 1024 × 1536. Se o provider não entregar esse tamanho, manter o original e criar derivado com processamento determinístico, sem esticar a anatomia.

Área segura:

- ação/equipamento principal dentro dos 60% centrais da imagem;
- cabeça, tronco e articulações críticas não devem depender das bordas;
- conferir thumbnail de card e crop horizontal 2:1;
- o master vertical deve continuar disponível para futuras superfícies.

O pipeline deve gerar uma folha de revisão com, lado a lado:

1. imagem completa;
2. crop 2:1 semelhante ao hero do app;
3. thumbnail semelhante ao card em grade;
4. nome, slug, equipamento e versão.

## QA automático

Bloquear ou sinalizar:

- formato diferente do permitido;
- dimensões incorretas;
- arquivo vazio/corrompido;
- alpha inesperado quando o profile exige fundo opaco;
- arquivo acima do limite configurado;
- hash igual em exercícios diferentes;
- nome/path não correspondente ao manifesto;
- ausência de entrada no media registry;
- baixa entropia/imagem quase vazia;
- imagem já aprovada sendo sobrescrita sem nova versão.

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

