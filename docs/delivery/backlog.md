# Milestones / Codex Backlog

## M0 — Foundation
- `PU-M0-001`: criar baseline novo reutilizando Expo, ASP.NET Core, Postgres, EF, SQLite/offline e primitives úteis do donor repo.
- `PU-M0-002`: renomear solução/packages/namespaces para PersonalUltra.
- `PU-M0-003`: criar TrainerApi e StudentApi compartilhando Domain/Application/Infrastructure/DbContext.
- `PU-M0-004`: separar mobile em trainer/student/shared e adicionar role switch demo-only.
- `PU-M0-005`: criar Trainer, TrainerBranding, Student, TrainerStudent, StudentInvite e Anamnesis.
- `PU-M0-006`: remover MethodologyVersion/Rule, StandardPlanProvisioner, RecommendedLoad, Coach mutations e progress photos.
- `PU-M0-007`: identidades demo Trainer e Student.
- `PU-M0-008`: aplicar design tokens Ultra e suporte a TrainerBranding.PrimaryColor. Usar como branding temporário os assets licenciados em `docs/assets/brand/` (`personal-ultra-app-icon.png`, logos e brand guide), substituindo os assets SVR no app. O branding dinâmico por Trainer continua em M1.

**DoD M0:** mobile abre em Trainer ou Student; duas APIs sobem; separação preparada para futuro split em dois apps.

## M1 — Trainer Core
- `PU-M1-001`: branding model/API.
- `PU-M1-002`: dashboard API.
- `PU-M1-003`: dashboard mobile.
- `PU-M1-004`: students list API.
- `PU-M1-005`: students list mobile.
- `PU-M1-006`: student detail API.
- `PU-M1-007`: student detail com Resumo/Anamnese/Treino/Alimentação/Progresso.
- `PU-M1-008`: TrainerMessage domain/API.
- `PU-M1-009`: composer de mensagem.
- `PU-M1-010`: WhatsApp deep link.
- `PU-M1-011`: branding dinâmico na Student Home.
- `PU-M1-012`: mensagem do Trainer na Student Home.

**DoD M1:** Trainer acompanha alunos e Student Home reflete branding/mensagem.

## M2 — Invitations & Anamnesis
- `PU-M2-001`: gerar convite/link.
- `PU-M2-002`: resolver token na Student API.
- `PU-M2-003`: onboarding associado ao Trainer.
- `PU-M2-004`: modelo de anamnese.
- `PU-M2-005`: formulário Student.
- `PU-M2-006`: persistência.
- `PU-M2-007`: visualização Trainer.
- `PU-M2-008`: atividade de anamnese no dashboard.

**DoD M2:** Trainer convida; Student preenche; Trainer vê.

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

**DoD M3:** Trainer prescreve; Student escolhe/executa; Trainer vê resultado.

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

**DoD M4:** demo comercial completa.

## M5 — Production Foundation (após validação)
Auth real, LGPD, storage, billing, backups, monitoring, rate limiting, push real, legal review, App Store/Play Store e split físico em Trainer Mobile + Student Mobile.

## Prompt padrão Codex
`Read AGENTS.md and all docs relevant to PU-MX-YYY. Implement only that task. Preserve Trainer/Student boundaries and future mobile splitability. Do not implement future tasks. Run relevant tests/typecheck and report architecture ambiguity before changing documented boundaries.`
