# Milestones / Codex Backlog

## M0 — Foundation
- `PU-M0-001`: criar baseline novo reutilizando Expo, ASP.NET Core, Postgres, EF, SQLite/offline e primitives úteis do donor repo.
- `PU-M0-002`: renomear solução/packages/namespaces para PersonalUltra.
- `PU-M0-003`: criar TrainerApi e StudentApi compartilhando Domain/Application/Infrastructure/DbContext.
- `PU-M0-004`: separar mobile em trainer/student/shared e adicionar role switch demo-only.
- `PU-M0-005`: criar Trainer, TrainerBranding, Student, TrainerStudent, StudentInvite e Anamnesis.
- `PU-M0-006`: remover MethodologyVersion/Rule, StandardPlanProvisioner, RecommendedLoad, Coach mutations e progress photos.
- `PU-M0-007`: identidades demo Trainer e Student.
- `PU-M0-008`: aplicar design tokens Ultra e suporte a TrainerBranding.PrimaryColor. Usar como branding temporário os assets licenciados em `docs/assets/brand/` (`personal-ultra-app-icon.png`, logos e brand guide), substituindo os assets SVR no app. O branding dinâmico por Trainer fica para o fechamento do M4.

**DoD M0:** mobile abre em Trainer ou Student; duas APIs sobem; separação preparada para futuro split em dois apps.

## M1 — Trainer Core
- `PU-M1-002`: dashboard API.
- `PU-M1-003`: dashboard mobile.
- `PU-M1-004`: students list API.
- `PU-M1-005`: students list mobile.
- `PU-M1-006`: student detail API.
- `PU-M1-007`: detalhe mobile do aluno com Resumo real (identidade, vínculo e estado da anamnese).
- `PU-M1-008`: TrainerMessage domain/API.
- `PU-M1-009`: composer de mensagem.

**DoD M1:** Trainer acompanha alunos, consulta o resumo e cria mensagens in-app. O tema Personal Ultra temporário permanece estático.

## M2 — Invitations & Anamnesis
- `PU-M2-001`: gerar convite/link.
- `PU-M2-002`: resolver token na Student API.
- `PU-M2-003`: onboarding associado ao Trainer.
- `PU-M2-004`: modelo de anamnese.
- `PU-M2-005`: formulário Student.
- `PU-M2-006`: persistência.
- `PU-M2-007`: visualização Trainer e integração da anamnese concluída ao detalhe do aluno.
- `PU-M2-008`: atividade de anamnese no dashboard.
- `PU-M2-009`: incorporada a `PU-M2-007`, para não duplicar a mesma entrega.
- `PU-M2-010`: capturar e persistir telefone de contato do Student vinculado.
- `PU-M2-011`: WhatsApp deep link do Trainer para o telefone real do Student.
- `PU-M2-012`: expor a TrainerMessage ativa na Student API e Home, usando o vínculo real Trainer/Student.
- `PU-M2-013`: distribuir convite pelo Trainer: código humano de seis dígitos, cópia e mensagem de WhatsApp com links configuráveis de instalação, sem landing page. Um novo convite para o mesmo e-mail invalida o pendente anterior até o cadastro ser iniciado.

**DoD M2:** Trainer convida; Student preenche; Trainer vê, contata via WhatsApp e recebe a mensagem in-app do Trainer.

## M3 — Training Prescription
- `PU-M3-001`: workout template domain.
- `PU-M3-002`: templates API.
- `PU-M3-003`: templates UI.
- `PU-M3-004`: editor de treino (exercício, ordem, séries, reps, descanso, notas).
- `PU-M3-005`: duplicar template.
- `PU-M3-006`: aplicar template copiando snapshot para Student.
- `PU-M3-007`: editar plano do Student.
- `PU-M3-008`: histórico supersedido pela rotina flexível M3RF; a antiga grade semanal não integra a arquitetura atual.
- `PU-M3-009`: Student API retorna recomendado + disponíveis + grade.
- `PU-M3-010`: Student Home/treinos mostra recomendado e alternativas.
- `PU-M3-011`: permitir iniciar qualquer treino disponível.
- `PU-M3-012`: portar execução de treino.
- `PU-M3-013`: portar SQLite/offline sync.
- `PU-M3-014`: histórico de sessões/séries no Trainer.
- `PU-M3-015`: integrar treino e histórico ao detalhe do aluno no Trainer.

A primeira implementação conectou prescrição/aplicação/execução ao backend, porém a superfície Trainer ficou simplificada demais: criação baseada em texto livre, editor visual começando com um único exercício e templates tratados como o centro do fluxo. Isso não atende a experiência comercial desejada.

### M3R — Refactor obrigatório antes de considerar M3 fechada

A direção aprovada está em `docs/design/trainer-training-refactor.md` e no adendo de `docs/architecture/domain.md`.

- `PU-M3R-001`: introduzir/refatorar `Exercise` como catálogo global reutilizável, system-owned e abastecido exclusivamente por seed na V1; remover exercício por texto livre como caminho principal de prescrição.
- `PU-M3R-002`: ampliar o seed com catálogo demonstrável de exercícios, reaproveitando nomes/assets do SVR donor quando disponíveis; cada exercício deve carregar ao menos grupo muscular e referência de imagem.
- `PU-M3R-003`: refatorar `WorkoutTemplateExercise` para referenciar `ExerciseId` e suportar `Sequence`, `Sets`, `RepetitionsMin`, `RepetitionsMax`, `RestSeconds` e `Notes`.
- `PU-M3R-004`: refatorar snapshots de `StudentWorkoutExercise`/`WorkoutSessionExercise` para preservar o contexto visual/histórico necessário sem depender do estado atual do template/catálogo.
- `PU-M3R-005`: criar endpoint/query Trainer para pesquisar/listar catálogo por texto e grupo muscular; somente leitura na V1.
- `PU-M3R-006`: substituir a tela atual `Meus treinos`/editor simplificado por fluxo student-centric: Student > Treinos > workout > editor multi-exercício.
- `PU-M3R-007`: implementar seletor de catálogo no Trainer com busca, filtros e thumbnails; selecionar exercício abre configuração de prescrição.
- `PU-M3R-008`: implementar editor real multi-exercício: adicionar/remover/editar vários exercícios, rep range, descanso, notas e ordenação arbitrária.
- `PU-M3R-009`: manter templates como aceleradores opcionais; template deve aceitar múltiplos exercícios e continuar aplicando snapshot editável ao Student.
- `PU-M3R-010`: preservar a UX Student derivada do SVR Method e apenas adaptá-la para consumir imagens/instruções/rep ranges do novo modelo; não redesenhar o fluxo Student durante este refactor.
- `PU-M3R-011`: garantir que Student execute treino atualizado, registre carga/reps reais e Trainer veja histórico sem regressão do SQLite/offline existente.
- `PU-M3R-012`: demo seed deve entregar Student principal com pelo menos quatro workouts completos e múltiplos exercícios/imagens, tornando a montagem e execução demonstráveis ponta a ponta.

**Non-goals M3R V1:** sem admin do catálogo, sem upload de mídia, sem vídeos, sem IA gerando treino, sem recommended load, sem versionamento enterprise de treino, sem microserviços.

**DoD M3/M3R:** Trainer abre um aluno, visualiza vários treinos completos, adiciona exercícios pesquisando o catálogo seeded, configura séries/faixa de reps/descanso/notas, reordena e salva; Student recebe a atualização no fluxo já consolidado, executa e registra carga/reps; Trainer vê o histórico.

**Status M3R (2026-08-13): concluído.** O catálogo global seeded, snapshots históricos, busca Trainer, fluxo student-centric, editor multi-exercício, templates opcionais, execução Student com mídia e carga/repetições reais, sincronização offline idempotente e quatro treinos completos do Student demo foram implementados e validados. O catálogo continua system-owned e sem administração na V1.

### M3RR — Restauração corretiva da experiência Student

A revisão pós-M3R constatou que catálogo, snapshots, execução e offline foram entregues, porém a UX Student consolidada do donor SVR não foi preservada como exigido por `PU-M3R-010`: a execução ficou condensada em uma tela longa com todos os exercícios. A direção corretiva aprovada está em `docs/design/student-training-ux-restoration.md`.

- `PU-M3RR-001`: criar shell e árvore de navegação Student independentes, com tabs Início, Treino, Coach, Nutrição e Progresso; detalhes, execução, descanso e resumo devem ser rotas internas sem tab bar.
- `PU-M3RR-002`: histórico supersedido por M3RF; a Home manteve cartões reais, mas removeu recomendação e cronograma prescritivo.
- `PU-M3RR-003`: restaurar hub e preview de treino com recomendado, alternativas, imagens e prescrição; consultar detalhes não pode iniciar uma sessão, e iniciar/continuar devem ser ações distintas.
- `PU-M3RR-004`: restaurar execução guiada, mostrando um exercício e uma série por vez, registrando somente carga e repetições reais e respeitando a sequência persistida; preservar a utilidade da tela condensada como visão geral consultiva da sessão.
- `PU-M3RR-005`: restaurar descanso e transições com timer local baseado em `RestSeconds`, pular descanso, adicionar 30 segundos e avançar corretamente entre séries/exercícios.
- `PU-M3RR-006`: restaurar conclusão e histórico Student com resumo derivado da sessão real e sem alegações de ajuste automático de plano, carga ou metodologia.
- `PU-M3RR-007`: endurecer retomada/offline para restaurar sessão, exercício e próxima série após navegação/reinício, sincronizando a fila SQLite sem duplicação.
- `PU-M3RR-008`: validar o fluxo integrado Student → execução → histórico Trainer, remover a superfície condensada substituída e revisar loading/error/empty/accessibility sem alterar regras de negócio.

**Non-goals M3RR:** sem recommended load, RIR obrigatório, substituição automática, Coach mutation, geração de plano, catálogo admin, vídeos, métricas inventadas no mobile ou regras SVR reintroduzidas no domínio.

**DoD M3RR:** Student navega por uma árvore própria, vê dados reais na Home, abre um treino sem iniciá-lo, inicia ou retoma uma sessão, executa uma série por vez com descanso e suporte offline, conclui em um resumo real e o Trainer vê o histórico resultante.

**Status M3RR (2026-08-13): concluído.** A validação integrada confirmou login/Home/tabs, hub e prévia read-only, início/retomada, visão geral da sessão, execução focada por série, descanso com pulo e acréscimo de 30 segundos, resumo/histórico Student e leitura do histórico real no Trainer. O typecheck Expo, `dotnet build`, 46 testes de integração e `npx expo export --platform android` passaram. A conexão local do Podman não respondeu, portanto o fluxo ao vivo com os containers não foi executado nesta etapa.

As rotas legadas `student-training*`, `student-coach`, `student-nutrition` e `student-progress` permanecem somente como redirects de compatibilidade; não são implementações concorrentes. A tela condensada substituída pode ser recuperada para comparação pelo commit Git `99df898` (`feat(m3r-010): adapt student workout execution`), principalmente nos arquivos listados em `docs/design/student-training-ux-restoration.md`. O estado offline posterior não deve ser revertido ao consultar esse commit.

### M3RF — Rotina flexível de treino

A revisão de produto pós-M3RR concluiu que dia da semana e treino
`recomendado` tornam uma prescrição flexível parecida com agenda obrigatória. A
direção aprovada está em `docs/design/flexible-training-routine.md`, e o loop de
execução está em `docs/delivery/m3rf-codex-handoff.md`.

- `PU-M3RF-001`: introduzir ordem sugerida persistida no `StudentWorkout`, com backfill determinístico e contratos aditivos; manter temporariamente os campos legados apenas para compatibilidade entre gates.
- `PU-M3RF-002`: adaptar Trainer API/mobile para criação, aplicação, listagem e reordenação por ordem sugerida; remover da UX badges `ATIVO`/`RECOMENDADO` e qualquer seleção/exibição de dia da semana.
- `PU-M3RF-003`: neutralizar Student API e criar preparação de sessão: Home oferece `Iniciar treino`, a tela intermediária explica a escolha e mostra uma lista compacta ordenada com resumos reais; nenhum treino é obrigatório ou recomendado.
- `PU-M3RF-004`: permitir escolher qualquer exercício pendente na visão geral da sessão, preservando a ordem prescrita apenas como sugestão e sem alterar permanentemente a prescrição do Trainer.
- `PU-M3RF-005`: adaptar execução guiada, descanso, retomada e offline para ordem de execução livre, mantendo séries idempotentes, conclusão correta e indicação clara do próximo exercício sugerido.
- `PU-M3RF-006`: refatorar o calendário/Home Student para mostrar somente sessões reais realizadas ou em andamento; remover workouts futuros por dia e qualquer linguagem de agenda obrigatória.
- `PU-M3RF-007`: remover `RecommendedDay`/`IsRecommended` legados de domínio, contratos, seed, Coach e banco; revisar histórico, empty/error/loading/accessibility e validar Trainer → Student → execução → calendário end-to-end.

**Status M3RF:** concluído. `PU-M3RF-001` a `PU-M3RF-007` entregaram ordem
sugerida persistida, Trainer/Student neutros, escolha livre de treino e
exercício, execução offline idempotente, calendário factual e remoção física
dos campos transitórios de agenda. O fluxo foi validado no backend e no Expo.

**Non-goals M3RF:** sem frequência semanal, calendário prescritivo, algoritmo de recomendação, metodologia automática, alteração da prescrição pelo Student, exclusão de histórico, IA ou recommended load.

**DoD M3RF:** Trainer ordena treinos disponíveis sem amarrá-los a dias; Student inicia pela Home, escolhe livremente um treino, pode executar exercícios pendentes em outra ordem, conclui com suporte offline e vê no calendário somente o que realmente executou.

### M3RX — Execução multimodal e fluida

Refinamento pós-M3RF documentado em
`docs/design/training-execution-refinement.md`:

- `PU-M3RX-001`: adicionar acompanhamento por repetições ou duração nos snapshots de catálogo, preset, prescrição e sessão.
- `PU-M3RX-002`: configurar exercícios temporizados no Trainer e preservar imagens completas com `contain` nas duas experiências.
- `PU-M3RX-003`: unificar registro e descanso Student, mantendo imagem/contexto e alternando apenas o painel de ação.
- `PU-M3RX-004`: permitir conclusão confirmada por exercício e por sessão sem performances fictícias.
- `PU-M3RX-005`: preservar retomada/offline detalhada, atualizar histórico/resumo e validar o fluxo ponta a ponta.

**Status M3RX (2026-08-24): concluído.** Cardio e isometrias seeded
recebem modo temporal; prescrições podem escolher modo e duração; a execução
Student alterna registro/descanso na mesma tela, oferece escolha livre do
próximo exercício e registra conclusões rápidas de forma explícita e factual.

**Non-goals M3RX:** cronômetro automático de cardio, sensores, GPS, calorias,
prescrição automática, recomendação de carga ou dados sintéticos de execução.

## Critério transversal de UI — M4 em diante

Toda task da M4 ou posterior que crie ou refatore uma tela deve identificar os estados válidos sem dados e tratá-los conforme o guia de `docs/design/design-system.md`. Esse é um critério de aceite da própria task, não um polish opcional para uma etapa posterior.

Para ser considerada concluída, a task de UI deve:

- distinguir loading, erro e ausência válida de dados;
- usar o primitive compartilhado `EmptyState` nas variantes `page`, `section` ou `inline`, sem criar uma apresentação concorrente;
- informar o estado atual, quem ou o que libera o próximo conteúdo e oferecer somente uma ação que já funcione;
- manter linguagem acolhedora para Student e operacional para Trainer;
- cobrir busca/filtro sem resultado e coleções vazias quando existirem no fluxo;
- revisar o estado vazio em tela pequena, com texto ampliado e sem dados de seed que o ocultem;
- incluir o cenário vazio na revisão manual da milestone e preservar typecheck/export do Expo.

Mensagens pontuais dentro de conteúdo existente — por exemplo, um dia sem treino dentro de uma agenda preenchida — podem continuar compactas. Endpoints, migrations, seeds e outras tasks sem superfície visual não precisam fabricar um empty state.

## M4 — Nutrition, Progress, Coach & Polish

O refactor das superfícies Trainer da M4 deve seguir `docs/design/trainer-experience-review.md`, além do critério transversal de UI acima. A revisão prioriza clareza, contexto do aluno e ações explícitas sem adicionar regras de negócio.

- `PU-M4-001`: adaptar domínio nutrição.
- `PU-M4-002`: editor de alimentação Trainer, incluindo estado padrão para plano/refeições ainda não criados e CTA funcional de criação.
- `PU-M4-003`: Student nutrition, incluindo estado `page` acolhedor quando o personal ainda não publicou o plano.
- `PU-M4-004`: Weight API.
- `PU-M4-005`: Student progress somente peso, incluindo primeira medição como empty state orientado à ação.
- `PU-M4-006`: Trainer weight chart, incluindo estado sem medições que explique a dependência do registro do Student.
- `PU-M4-007`: Coach context builder read-only.
- `PU-M4-008`: remover qualquer Coach mutation.
- `PU-M4-009`: Coach UI read-only, incluindo revisão do estado inicial/sem resposta sem sugerir mutations ou capacidades inexistentes.
- `PU-M4-010`: demo seed com 12–20 Students.
- `PU-M4-011`: Student principal com anamnese, 4 treinos, histórico, alimentação, peso e TrainerMessage.
- `PU-M4-012`: demo reset.
- `PU-M4-013`: polish de loading/error/empty/haptics; auditar todas as superfícies Student e Trainer contra o padrão compartilhado de empty states.
- `PU-M4-014`: verificar demo end-to-end, incluindo cenários sem dados de seed para cada empty state introduzido ou revisado na M4.
- `PU-M4-015`: configuração de branding do Trainer (modelo/API/UI), usando as cores semânticas compartilhadas e tratando qualquer ausência de configuração conforme o padrão de empty state quando aplicável.
- `PU-M4-016`: aplicar branding dinâmico e validado na experiência Student, garantindo que o primitive compartilhado de empty state receba o accent validado sem alterar cores semânticas.
- `PU-M4-017`: integrar alimentação e progresso ao detalhe do aluno no Trainer, consolidando as seções disponíveis e seus estados `inline` sem plano ou medições.
- `PU-M4-018`: concluída antecipadamente por decisão de produto: o fluxo legado SVR baseado em `Member` foi retirado, e a entrada do aluno permanece apoiada apenas em `Student`. A autenticação demo por e-mail permanece até a fundação de autenticação real na M5.
- `PU-M4-019`: presets de refeição Trainer-owned, aplicação por snapshot,
  entrada `Alimentação` na bottom nav e seção operacional no detalhe do aluno.

Implementação concluída na demo: alimentação Trainer/Student, registro e consulta de peso, Coach explicativo read-only, seed ampliado, reset demo, branding validado por Trainer e aplicado ao contexto Student, além da consolidação no detalhe do aluno.

**Revisão de nutrição (2026-08-24): concluída.** O modelo agora preserva ordem
de refeições e itens, quantidade/unidade, autoria e atualização; Trainer edita
o documento completo sem perda silenciosa e Student consulta a mesma versão
persistida em modo read-only. A API valida o payload integral antes de substituir
o plano e o gate cobre o roundtrip Trainer → Student, ownership, ausência válida
e rejeições atômicas. A direção e os non-goals estão em
`docs/design/nutrition-experience-review.md`.

**Presets de refeição (2026-08-24): concluídos.** A biblioteca suporta CRUD e
duplicação de refeições reutilizáveis; cada aplicação acrescenta um snapshot
independente sem apagar o conteúdo existente. O Trainer ganhou uma superfície
`Alimentação` na navegação inferior; no detalhe do aluno, `Evolução` foi
substituída por `Alimentação` e o peso permanece disponível em `Resumo`.

**Refinamento de alimentação (2026-08-25): concluído.** Cards do Trainer editam
e reordenam refeições individualmente, `livre` cobre itens sem quantidade fixa,
novos presets não reaproveitam o formulário anterior, a biblioteca antecipa os
alimentos e as rotas de retorno preservam o contexto de origem. A bottom nav
Trainer recebeu a mesma escala e o mesmo destaque ativo da Student.

Antes de considerar a demo comercialmente fechada, o refactor `M3R` acima tem prioridade sobre novos refinamentos de M4, pois montagem/prescrição de treino é uma superfície central do produto.

**DoD M4:** demo comercial completa, incluindo branding dinâmico por Trainer.

### M4N — Substituições manuais de alimentos

Permitir que o Trainer cadastre alternativas explícitas para um alimento
prescrito, sem cálculo nutricional ou recomendação automática. A direção
completa está em `docs/design/nutrition-substitutions-milestone.md`.

- `PU-M4N-001`: modelar alternativas ligadas ao item de refeição e ao item de
  preset, com nome, quantidade, unidade, sequência e observação opcional.
- `PU-M4N-002`: incluir alternativas na persistência e nos contratos das duas
  APIs, preservando a validação atômica do documento.
- `PU-M4N-003`: permitir ao Trainer adicionar, editar, remover e reordenar
  alternativas no editor individual da refeição e nos presets.
- `PU-M4N-004`: exibir alternativas no Student em seção expansível e read-only.
- `PU-M4N-005`: preservar alternativas ao editar/reordenar refeições e aplicar
  presets por snapshot independente.
- `PU-M4N-006`: cobrir ownership, round-trip Trainer → Student, validação,
  snapshots e ausência de alternativas.

**Non-goals M4N:** cálculo de equivalência, recomendação ou substituição
automática, catálogo nutricional, scanner, IA, diário de adesão ou alteração do
plano original pelo Student.

**DoD M4N:** Trainer informa, por exemplo, `Carne vermelha — 150 g` com a
alternativa `Peixe — 200 g`; Student a visualiza com clareza, sem o sistema
afirmar equivalência nutricional ou modificar a prescrição.

**Status M4N (2026-08-25): concluído.** As alternativas manuais são ordenadas,
validadas e copiadas tanto ao duplicar presets quanto ao aplicá-los no plano do
Student. O Trainer as edita por alimento; o Student apenas consulta a seção
expansível quando houver conteúdo.

### M4S — Jornada inicial, progresso e hidratação Student

Refinar a experiência do aluno após a anamnese e tornar o acompanhamento de
progresso mais útil, sem alterar a prescrição do Trainer nem introduzir metas
ou recomendações clínicas automáticas.

- `PU-M4S-001`: substituir a tela de espera pós-anamnese por uma boas-vindas
  motivadora, com contexto do personal e somente uma ação funcional para
  seguir ao início; não oferecer atalhos para conteúdos ainda não publicados.
- `PU-M4S-002`: evoluir o progresso de peso Student com gráfico baseado
  exclusivamente nos registros reais, estado apropriado para zero/um registro
  e edição ou exclusão segura dos lançamentos.
- `PU-M4S-003`: criar registros de hidratação por Student, com quantidade em
  mililitros e horário, persistidos no `DbContext` compartilhado e expostos
  somente pela `StudentApi` na V1.
- `PU-M4S-004`: incluir na Home um card de hidratação diária com ações rápidas
  (`+500 ml`, `+1 L`) e entrada de outro valor, levando ao detalhe sem alegar
  meta individual ou benefício clínico.
- `PU-M4S-005`: ampliar a tela Progresso com total diário, histórico editável e
  gráfico de hidratação derivado dos registros reais; manter peso e hidratação
  claramente separados.
- `PU-M4S-006`: validar ownership, datas/quantidades, edição/exclusão e
  round-trip da hidratação; revisar loading/error/empty, fonte ampliada e
  export do Expo nas telas alteradas.

**Non-goals M4S:** meta automática de água, lembretes/push, cálculos de saúde,
recomendações nutricionais, alteração de plano pelo Student, mudanças no
formulário de anamnese ou expansão do Coach. Ajustes futuros de anamnese e do
Coach serão milestones separadas, deliberadamente fora deste escopo.

**DoD M4S:** após concluir a anamnese, o Student é acolhido sem promessas de
conteúdo indisponível; na Home registra hidratação em poucos toques; em
Progresso consulta e corrige seu histórico real de peso e água por gráficos e
listas legíveis.

## M5 — Production Foundation (após validação)
Auth real, LGPD, storage, billing, backups, monitoring, rate limiting, push real, legal review, App Store/Play Store e split físico em Trainer Mobile + Student Mobile.

Qualquer decomposição futura da M5 em tasks de interface herda obrigatoriamente o critério transversal de UI acima. O split físico deve extrair o mesmo primitive e preservar comportamento/copy dos empty states, sem duplicá-los de forma divergente entre os apps.

## V2 addendum
- Admin interno para gerenciar catálogo de exercícios.
- Upload/gestão de imagens e vídeos.
- Vídeo por exercício.
- Curadoria e lifecycle do catálogo.
- AI-assisted workout generation baseada na metodologia do Trainer, sempre revisada/publicada pelo profissional.

Toda futura task V2 com superfície visual também herda o critério transversal de UI da M4 em diante.

## Prompt padrão Codex
`Read AGENTS.md and all docs relevant to PU-MX-YYY. Implement only that task. Preserve Trainer/Student boundaries and future mobile splitability. For every UI task from M4 onward, inventory and validate empty states against docs/design/design-system.md as an acceptance criterion. Do not implement future tasks. Run relevant tests/typecheck and report architecture ambiguity before changing documented boundaries.`
