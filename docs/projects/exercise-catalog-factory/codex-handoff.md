# Handoff autoritativo — Exercise Catalog Factory

Atualizado em 2026-08-24. Este é o resumo operacional para retomar o trabalho
sem reler o histórico da conversa. As especificações continuam nos demais
arquivos deste diretório; não duplicá-las aqui.

## Fonte de verdade e limites

- Repositório: `C:\git\PresonalUltra`, branch `main`.
- A sequência executável atual é `PU-ECF-001`–`012` de
  [personal-ultra-integration-milestone.md](personal-ultra-integration-milestone.md).
  O [backlog.md](backlog.md) registra o desenho original e usa números diferentes
  a partir de ECF-006; em caso de conflito, prevalece a milestone de integração.
- A factory é uma CLI .NET 10 em
  `tools/PersonalUltra.ExerciseCatalogFactory`; não é API, microserviço nem
  acesso direto ao PostgreSQL.
- Dry-run é o padrão. Não gerar conteúdo pago, publicar imagem, alterar seed ou
  integrar ao app sem passar pelos gates de revisão correspondentes.
- Nunca ler, imprimir, versionar ou pedir secrets na conversa. Eles entram
  somente no User Secrets próprio da factory.
- Preservar `docs/projects/exercise-catalog-factory.zip`: é artefato local do
  usuário, não deve ser commitado.

## Estado implementado

| Item | Estado | Commit |
|---|---|---|
| ECF-001 — CLI, configuração, logs, run/resume e dry-run | concluído | `551997e` |
| ECF-002 — contratos v1, manifesto e checkpoint atômico | concluído | `e57aa2d` |
| ECF-003 — intake determinístico e identidades | código concluído; gate humano aberto | `d4745e0` |
| ECF-004 — enriquecimento OpenAI estruturado e retomável | concluído; nenhuma chamada paga executada | `72e98d3` |
| ECF-006 — adapter e smoke privado S3 | concluído; smoke real aprovado | `d0f940c` |

Estado validado no commit `72e98d3`: build da solution sem erros, 109 testes da
factory e 68 testes de integração das APIs. O adapter OpenAI de metadados existe,
mas nenhuma chamada paga foi executada. Imagem nova, upload de catálogo, seed
gerado e consumo remoto no app ainda não existem.

O intake contém 232 candidatos, preserva exatamente os 28 GUIDs/slugs legados,
usa UUID v5 para itens novos e produz catálogo/relatório retomáveis. Resultado
atual: 220 normalizados, 12 em `needs_review`, 13 relações de ambiguidade;
17 identidades legadas têm vínculo exato e 11 continuam sem resolução. O
equipamento dos 232 itens não foi inferido porque a fonte não o fornece por
linha; isso pertence ao enriquecimento/review, não a um preenchimento fictício.

## Bloqueios e gates abertos

1. **Catálogo:** aprovar taxonomia e resolver as 11 identidades legadas ambíguas
   listadas em [exercise-inventory-v1.md](exercise-inventory-v1.md), incluindo
   stiff/RDL, afundo, passada, remada baixa, puxada dorsal, desenvolvimento,
   rosca direta, abdução com elástico e agachamento sumô. Não mesclar nem trocar
   IDs por heurística.
2. **Conteúdo:** definir revisor biomecânico. Metadados gerados ficam pendentes
   até aprovação explícita.
3. **Imagem:** confirmar acesso da conta ao modelo configurado. O piloto possui
   dez exercícios, executados em dois lotes de cinco; revisar o primeiro antes
   do segundo e só então congelar `styleVersion`/tratamento de marca.

## Retomada imediata

Em 2026-08-24, `bucket doctor` e o smoke real passaram no endpoint
`t3.storageapi.dev`, em virtual-hosted style. Foram validados PUT, HEAD, GET
autenticado, GET assinado, bytes, SHA-256, MIME, DELETE da chave exata e
`NotFound` final; o cleanup foi confirmado e nenhum objeto permaneceu. O erro
anterior `InvalidAccessKeyId` foi resolvido após a atualização da configuração.

Seguir em sequência:

1. ECF-005: workflow de revisão e resolução do gate de metadados.
2. ECF-007: piloto pago de imagem, cinco + revisão + cinco.
3. ECF-008–012: processamento/review, publicação, exporter, consumo remoto nas
   duas APIs/Expo e validação ponta a ponta.

Antes de cada avanço: implementar, revisar, testar, fazer commit intencional e
push. Parar em falha externa, gasto pago ou decisão humana. Os comandos de
fechamento estão em [validation.md](validation.md).

## Decisões já fixadas

- Novas imagens ficam remotas no bucket privado; os 28 assets legados continuam
  no bundle para compatibilidade histórica.
- Persistir `media://exercise-catalog/...`, nunca URL assinada. Infrastructure
  compartilha apenas o resolver; TrainerApi e StudentApi mantêm contratos
  próprios.
- O Expo usa primitive compartilhada, cache em disco e fallback/placeholder
  offline, mantendo separadas as features Trainer e Student.
- Seed/export é determinístico e gera pacote revisável; não escreve no banco.
- Nenhum mock deve preencher lacunas de dados ou chegar ao produto.

## Trabalho posterior enfileirado

Depois de concluir integralmente bucket + factory + integração do catálogo,
revisar o módulo de nutrição nas visões Trainer e Student. A tarefa inclui:

- auditar API, persistência e UI para localizar mocks, dados estáticos e fluxos
  incompletos;
- pesquisar padrões atuais de apps de prescrição alimentar;
- documentar um plano alinhado à UX acolhedora, explicativa e demo-first do app;
- implementar apenas dentro do módulo de nutrição e suas telas Trainer/Student.

Se a revisão exigir mudanças amplas de domínio, infraestrutura compartilhada ou
outros módulos, não executar automaticamente: documentar o impacto e aguardar o
usuário. Essa revisão ainda não começou.
