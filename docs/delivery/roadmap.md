# Roadmap

## M0 — Foundation
Criar Personal Ultra como produto novo reutilizando a base técnica do SVR Method, com duas APIs e um único app demo preparado para split futuro.

## M1 — Trainer Core
Dashboard, alunos, mensagens in-app e WhatsApp deep link. O app usa o tema Personal Ultra temporário e estático nesta etapa.

## M2 — Invitations & Anamnesis
Fluxo Trainer -> convite -> cadastro -> anamnese -> acompanhamento.

## M3 — Training Prescription
Templates, editor, aplicação ao aluno, ordem sugerida e liberdade de escolha de treino.

## M4 — Nutrition, Progress, Coach & Demo Polish
Alimentação, progresso somente de peso, Coach read-only, seed, demo comercial completa e, por último, branding dinâmico do Trainer aplicado ao Student. A retirada do fluxo SVR/`Member` já foi concluída antecipadamente, deixando a entrada do aluno apoiada somente em `Student`.

A partir do refactor da M4, toda entrega que criar ou alterar telas deve tratar empty state como critério de aceite, usando as variantes compartilhadas e as regras de conteúdo de `docs/design/design-system.md`. A regra continua valendo para M5 e V2; tasks exclusivamente técnicas não precisam criar estados visuais artificiais.

## M4S — Student Journey & Hydration Refinement
Boas-vindas após a anamnese, evolução de peso com visualização e correção de
registros reais, e hidratação registrada pelo próprio aluno. Não encerra a V1:
ajustes posteriores de anamnese e Coach permanecem planejáveis separadamente.

## M5 — Production Foundation
Somente após validação:
- auth real;
- LGPD;
- storage;
- billing;
- backups;
- monitoring;
- rate limiting;
- push notification real;
- revisão legal;
- App Store / Play Store;
- split físico em `trainer-mobile` e `student-mobile`.

## V2 Product
- avaliação física detalhada;
- fotos e medidas;
- AI-assisted workout generation baseada na metodologia do Trainer;
- sugestões inteligentes com aprovação humana;
- times/múltiplos profissionais;
- automações.
