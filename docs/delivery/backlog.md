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
- `PU-M3-008`: grade semanal recomendada.
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
- `PU-M3RR-002`: restaurar Home Student orientada ao dia, com treino recomendado/em andamento, cronograma semanal e cartões reais de mensagem, alimentação e progresso, sem fabricar métricas ausentes.
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

## M4 — Nutrition, Progress, Coach & Polish
- `PU-M4-001`: adaptar domínio nutrição.
- `PU-M4-002`: editor de alimentação Trainer.
- `PU-M4-003`: Student nutrition.
- `PU-M4-004`: Weight API.
- `PU-M4-005`: Student progress somente peso.
- `PU-M4-006`: Trainer weight chart.
- `PU-M4-007`: Coach context builder read-only.
- `PU-M4-008`: remover qualquer Coach mutation.
- `PU-M4-009`: Coach UI read-only.
- `PU-M4-010`: demo seed com 12–20 Students.
- `PU-M4-011`: Student principal com anamnese, 4 treinos, histórico, alimentação, peso e TrainerMessage.
- `PU-M4-012`: demo reset.
- `PU-M4-013`: polish de loading/error/empty/haptics.
- `PU-M4-014`: verificar demo end-to-end.
- `PU-M4-015`: configuração de branding do Trainer (modelo/API/UI), usando as cores semânticas compartilhadas.
- `PU-M4-016`: aplicar branding dinâmico e validado na experiência Student.
- `PU-M4-017`: integrar alimentação e progresso ao detalhe do aluno no Trainer, consolidando as seções disponíveis.
- `PU-M4-018`: concluída antecipadamente por decisão de produto: o fluxo legado SVR baseado em `Member` foi retirado, e a entrada do aluno permanece apoiada apenas em `Student`. A autenticação demo por e-mail permanece até a fundação de autenticação real na M5.

Implementação concluída na demo: alimentação Trainer/Student, registro e consulta de peso, Coach explicativo read-only, seed ampliado, reset demo, branding validado por Trainer e aplicado ao contexto Student, além da consolidação no detalhe do aluno.

Antes de considerar a demo comercialmente fechada, o refactor `M3R` acima tem prioridade sobre novos refinamentos de M4, pois montagem/prescrição de treino é uma superfície central do produto.

**DoD M4:** demo comercial completa, incluindo branding dinâmico por Trainer.

## M5 — Production Foundation (após validação)
Auth real, LGPD, storage, billing, backups, monitoring, rate limiting, push real, legal review, App Store/Play Store e split físico em Trainer Mobile + Student Mobile.

## V2 addendum
- Admin interno para gerenciar catálogo de exercícios.
- Upload/gestão de imagens e vídeos.
- Vídeo por exercício.
- Curadoria e lifecycle do catálogo.
- AI-assisted workout generation baseada na metodologia do Trainer, sempre revisada/publicada pelo profissional.

## Prompt padrão Codex
`Read AGENTS.md and all docs relevant to PU-MX-YYY. Implement only that task. Preserve Trainer/Student boundaries and future mobile splitability. Do not implement future tasks. Run relevant tests/typecheck and report architecture ambiguity before changing documented boundaries.`
