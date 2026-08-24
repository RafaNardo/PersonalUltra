# Milestones — Exercise Catalog Factory

Cada milestone é um gate. Implementar, revisar e validar antes de avançar. O projeto pode começar simples, mas não pode omitir retomada, aprovação humana ou rastreabilidade, pois uma geração de centenas de imagens tem custo e não deve ser repetida por acidente.

## ECF-M0 — Fundação

### `PU-ECF-001` — Baseline do projeto

Criar uma CLI Console .NET 10 isolada dentro da solution, com configuração, logging estruturado redigido, testes, convenções de formatação da solution e armazenamento local de jobs. Esta decisão substitui explicitamente a proposta inicial de repositório TypeScript/Node, sem alterar os contratos ou gates posteriores.

Entregas:

- comandos `init`, `import`, `status` e `doctor`;
- config sem segredos (`factory.config.json` ou equivalente);
- `.env.example` somente com nomes de variáveis;
- diretórios `inputs/`, `workspace/`, `outputs/` ignorados conforme sensibilidade;
- `--dry-run` como padrão para comandos que geram ou exportam.

Aceite:

- `doctor` detecta chave ausente, paths inválidos e target profile incompatível;
- logs nunca imprimem token, payload base64 ou conteúdo sensível completo;
- uma execução possui `runId` e pode ser retomada.

### `PU-ECF-002` — Contratos e manifesto versionado

Implementar os schemas de [contracts.md](contracts.md), validação de CSV/JSON, canonicalização e persistência atômica do manifesto.

Aceite:

- linhas inválidas são reportadas com arquivo/linha/campo;
- schema desconhecido falha de forma explícita;
- reimportar a mesma fonte não duplica itens;
- alterações de nome não trocam automaticamente uma chave já congelada.

## ECF-M1 — Conteúdo estruturado

### `PU-ECF-003` — Normalização e deduplicação determinística

Gerar/fixar `externalKey`, nome canônico, slug, asset name e source hash. Detectar duplicatas exatas e candidatas semânticas.

Aceite:

- slug é único, estável, sem acentos e em kebab-case;
- arquivo é único e usa convenção configurável;
- colisões nunca são resolvidas silenciosamente com sufixo aleatório;
- candidatos como “Leg press 45” e “Leg press 45°” ficam em `needs_review`.

### `PU-ECF-004` — Enriquecimento por IA

Criar adapter de texto OpenAI com saída estruturada para propor grupo muscular, equipamento, instruções curtas, aliases e descrição visual usada pelo gerador de imagem.

Aceite:

- resposta é validada contra schema e taxonomia permitida;
- temperatura/variabilidade e prompts são configurados e versionados;
- falha de um item não invalida o lote;
- campos fornecidos e travados pelo operador não são sobrescritos;
- toda proposta fica `needs_review`, nunca `approved` automaticamente.

### `PU-ECF-005` — Review de metadados

Entregar workflow local de revisão, inicialmente por arquivos/comandos, sem exigir painel web.

Comandos esperados:

- `review list --status needs_review`;
- `review show <key>`;
- `review approve <key>`;
- `review reject <key> --reason ...`;
- `review merge <source> --into <target>`;
- `review edit <key> --patch ...`.

Aceite:

- aprovação registra data, responsável e hash do conteúdo;
- editar conteúdo aprovado invalida apenas os estágios derivados necessários;
- merge mantém aliases e trilha das chaves de origem.

## ECF-M2 — Artes de exercícios

> Decisão de simplificação (2026-08-24): o piloto usa somente `images plan`,
> `images generate`, um arquivo texto de aprovações e `images upload`. Manifesto
> e PNGs locais permitem retomada; o CLI limita a dez, exige teto de custo e
> `--execute`, não regenera arquivo existente e publica apenas slugs aprovados.
> Painel, CRUD, contact sheet, derivados, seed e integração API/app ficam fora
> deste piloto e serão reconsiderados somente após a revisão das dez artes.
> O style v1 rejeitado permanece arquivado localmente. A nova geração usa
> `personal-ultra-exercise-image-v2` em diretório próprio e somente o v2 pode ser
> aprovado ou publicado.

### `PU-ECF-006` — Prompt visual e geração unitária

Implementar adapter de geração de imagem usando [image-style-guide.md](image-style-guide.md), primeiro para um lote piloto de 5–10 exercícios variados.

Aceite:

- prompt final, provider, model, size, seed quando disponível e response ID ficam registrados;
- resultado original e derivado têm SHA-256;
- nenhuma arte é gerada para metadado não aprovado;
- `--max-items`, `--max-cost` e confirmação explícita protegem a execução paga;
- retry não cria cobrança duplicada quando já existe resultado válido.

Implementação enxuta adotada: o catálogo canônico é a fonte direta e prompt,
model, size, quality, filename e SHA ficam no manifesto local. O piloto visual
não depende de um CRUD de revisão de metadados.

### `PU-ECF-007` — Processamento, QA e lote retomável

Adicionar normalização de formato/dimensões, thumbnails de revisão, validações automáticas e processamento concorrente limitado.

Aceite:

- dimensões, formato, tamanho e nome de arquivo são verificados;
- o personagem/equipamento permanece dentro da área segura de crop;
- itens rejeitados podem ser regenerados individualmente com feedback adicional;
- interrupção e retomada não regeneram itens aprovados;
- relatório apresenta progresso, estimativa e custo observado.

### `PU-ECF-008` — Review visual e aprovação biomecânica

Criar contact sheet/galeria local e workflow de aprovação por item.

Aceite:

- revisão separa `visualApproved` de `contentApproved`;
- motivos de rejeição usam categorias: anatomia, execução, equipamento, enquadramento, estilo, artefato ou outro;
- seed final bloqueia imagem sem aprovação visual e biomecânica;
- uma regeneração preserva versões anteriores para auditoria, sem incluí-las no export final.

## ECF-M3 — Integração Personal Ultra

### `PU-ECF-009` — Target profile Personal Ultra

Implementar exporter desacoplado que gere:

- PNGs finais em árvore equivalente a `apps/mobile/assets/training/`;
- entradas de `ExerciseCatalogSeed.cs` com IDs determinísticos;
- registro TypeScript de `ImageRef` → `require(...)` para o Metro;
- manifesto de catálogo legível por máquina;
- relatório de grupos novos e mudanças requeridas no filtro mobile.

Aceite:

- exporter não edita o target por padrão; escreve em `outputs/<runId>/personal-ultra/`;
- slugs já existentes preservam IDs e não são sobrescritos silenciosamente;
- toda referência do seed possui um arquivo e uma entrada estática no registry;
- todo arquivo exportado é referenciado exatamente uma vez, salvo allowlist;
- saída é ordenada para gerar diffs pequenos e reproduzíveis.

### `PU-ECF-010` — Verificador do repositório alvo

Criar `verify-target <path>` somente leitura e `apply --target <path>` opcional, com confirmação e backup/diff.

Aceite:

- verifica schema/domain atual antes de gerar C#;
- detecta slugs, GUIDs, asset names e `ImageRef` conflitantes;
- aplica mudanças apenas em paths explicitamente permitidos;
- nunca altera migrations históricas nem banco;
- após aplicação orienta `dotnet build/test`, typecheck e Expo export.

### `PU-ECF-011` — Execução incremental

Validar um segundo lote contendo itens novos, item renomeado, duplicata e regeneração de imagem.

Aceite:

- itens aprovados e inalterados têm cache hit;
- nome editado preserva chave/ID quando declarado como o mesmo exercício;
- lote incremental exporta somente o delta necessário mais os arquivos agregados regenerados;
- nenhum asset ou seed existente desaparece sem operação explícita de depreciação.

### `PU-ECF-012` — Ensaio completo e handoff

Executar piloto aprovado, documentar custo/tempo, validar o pacote num clone limpo do Personal Ultra e produzir runbook operacional.

Aceite final:

- import → enrich → review → generate → review → export → verify funciona após reinício do processo;
- pacote passa build/test/typecheck/Expo export no target;
- nenhum dado da UI é mockado;
- falhas e itens pendentes aparecem no relatório final;
- operador consegue adicionar um lote futuro sem suporte do autor original.

## DoD do projeto

Uma lista de exercícios validada produz, com revisão humana e execução retomável, um pacote determinístico que amplia o catálogo real do Personal Ultra. O pacote contém metadados, assets e todos os bindings estáticos necessários; não escreve diretamente no banco e não publica conteúdo não aprovado.
