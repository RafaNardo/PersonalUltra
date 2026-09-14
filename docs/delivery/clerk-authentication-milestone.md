# M5A — Autenticação Clerk e onboarding por convite

## Objetivo

Substituir a autenticação demo por Clerk no app Expo e nas duas APIs, sem
fundir as superfícies de Trainer e Student nem transformar o seletor de papel
em autorização. A conta Clerk identifica a pessoa; o vínculo persistido entre
Trainer e Student continua sendo a fonte de acesso aos dados do produto.

Esta milestone parte do estado atual: Chat foi removido. `TrainerMessage`
unidirecional permanece fora do escopo de autenticação.

## Fluxos aprovados

### Student

1. Abre o app sem sessão e escolhe criar conta ou entrar como aluno.
2. Cria ou acessa sua conta pelo Clerk.
3. Se o `sub` Clerk ainda não estiver vinculado a um `Student`, o app abre o
   onboarding de convite.
4. Informa o código de seis dígitos recebido do personal e confirma o nome do
   personal responsável exibido pelo app.
5. A Student API valida convite, expiração e vínculo; então vincula o `sub`
   Clerk ao `Student` criado/reivindicado.
6. Segue para anamnese pendente, boas-vindas ou tabs conforme o estado real.

Um Student autenticado sem convite válido não vê dados, treinos ou telas do
Trainer. Ele recebe uma tela de onboarding clara com a única ação de informar
um código.

### Trainer

1. Abre o app sem sessão e escolhe criar conta ou entrar como personal.
2. Cria ou acessa sua conta pelo Clerk.
3. Se o `sub` Clerk ainda não estiver vinculado a um `Trainer`, conclui um
   onboarding curto com nome profissional e nome de exibição inicial.
4. A Trainer API cria o `Trainer` e seu branding padrão, associa o `sub` e
   abre o dashboard vazio.
5. O Trainer pode então convidar Students pelo fluxo existente.

Não há ligação automática de uma conta nova ao Trainer ou aos Students de seed.
Isso preserva ownership: os dados demo existentes continuam acessíveis somente
por uma conta explicitamente vinculada para demonstração administrativa, se
essa capacidade for criada em task futura.

## Decisões de arquitetura

- Um único app Expo continua com árvores independentes de autenticação,
  onboarding e navegação para Trainer e Student. O futuro split move essas
  árvores sem reescrever features.
- `TrainerApi` aceita somente um token Clerk cujo `sub` esteja vinculado a um
  `Trainer`; `StudentApi`, somente um `sub` vinculado a um `Student`.
- O papel não vem do role switch, de metadata editável no cliente ou de um
  parâmetro de rota. As APIs resolvem o `sub` do JWT no banco compartilhado e
  validam ownership como já fazem hoje.
- `Trainer` e `Student` recebem um identificador Clerk opcional, único por
  entidade, por migration. Nesta V1 uma conta pode representar somente um
  papel ativo; suportar a mesma pessoa nos dois papéis exige uma task posterior
  com modelo explícito, não uma exceção ad hoc.
- O app usa `@clerk/clerk-expo`, `ClerkProvider` no bootstrap e cache de token
  em `expo-secure-store`. Somente a publishable key entra em `EXPO_PUBLIC_*`;
  secret key e configurações de verificação ficam exclusivamente nas APIs e no
  Railway.
- As APIs validam JWTs Clerk por issuer/JWKS e audience configurados. Não
  chamam o Clerk para cada request e não aceitam mais os bearers demo.
- O código de convite continua sendo criado e consumido pelo banco do produto;
  Clerk não é usado como armazenamento de vínculo, convite ou prescrição.
- O login demo por e-mail, `DemoSessionTokenService`, handlers de autenticação
  demo e o role switch são removidos ao fim da milestone, junto com contratos
  e testes exclusivos deles.

## Plano de execução

### M5A-001 — Preparação Clerk e configuração segura

- Executar o Clerk CLI usando a aplicação
  `app_3J9l8kKFjfwfFyefAZfpOFuOoAS` conforme o roteiro fornecido pelo produto.
- Configurar métodos de sign-up/sign-in definidos no dashboard Clerk e URLs
  de redirect/deep link `personalultra://`.
- Registrar apenas nomes de variáveis em `.env.example`; nunca imprimir nem
  commitar chaves privadas.
- Configurar no Railway issuer, JWKS/audience e secret necessário para cada
  API.

### M5A-002 — Identidade persistida e autorização das APIs

- Adicionar identificadores Clerk a `Trainer` e `Student`, índices únicos e
  migration compatível com PostgreSQL compartilhado.
- Criar uma primitive compartilhada de resolução de subject para as APIs, sem
  unificar endpoints de atores.
- Trocar os handlers demo por JWT Bearer Clerk e manter `RequireAuthorization`
  em todas as rotas protegidas.
- Preservar endpoints públicos estritamente necessários para resolver convite;
  a reivindicação do convite passa a exigir sessão Clerk Student.

### M5A-003 — Onboarding Trainer

- Criar endpoint e tela de bootstrap para o primeiro perfil Trainer.
- Exigir somente nome profissional e nome de exibição inicial; não antecipar
  credenciais, billing, equipe ou white-label.
- Garantir dashboard vazio, convite e ownership do novo Trainer.

### M5A-004 — Onboarding Student por código

- Substituir o login demo por sign-up/sign-in Clerk.
- Depois da autenticação, resolver o estado Student pelo `sub`; se inexistente,
  apresentar o código de convite como primeiro passo obrigatório.
- Reaproveitar validação de expiração, substituição de convite e vínculo atual,
  exigindo compatibilidade com e-mail verificado quando o convite a restringir.
- Depois de reivindicar, encaminhar pela anamnese e pelas tabs existentes sem
  criar sessão paralela no Zustand.

### M5A-005 — Navegação, remoção demo e UX

- Criar rotas autenticadas e não autenticadas independentes por ator.
- Remover escolha de papel demo, login de e-mail demo e ações de troca de
  contexto da UI final.
- Manter Perfil Student como última tab e usar os controles Clerk adequados
  para sessão/saída, sem duplicar tela.
- Cobrir loading, erro, convite inválido/expirado, conta sem vínculo e primeiro
  acesso vazio conforme o design system.

### M5A-006 — Testes, deploy e corte

- Cobrir JWT ausente, inválido e de ator errado; subject desconhecido;
  ownership; criação Trainer; Student sign-up + código; e regressão de convite.
- Atualizar clientes mobile para obter token Clerk antes de cada chamada e
  testar renovação/saída.
- Executar typecheck, Expo Doctor, export iOS, build .NET e regressão API.
- Fazer deploy sequencial no Railway: migration/Student API, health check,
  Trainer API, health check e teste manual dos dois onboarding flows.
- Só então remover variáveis e caminhos de autenticação demo remanescentes.

## Critérios de aceite

- Não autenticado não acessa Dashboard Trainer nem qualquer dado Student.
- Um novo personal cria o próprio perfil e só acessa sua carteira.
- Um novo aluno se cadastra no Clerk, informa código válido e vê apenas o
  vínculo daquele personal.
- Código inválido ou expirado não cria nem vincula Student.
- Logout limpa acesso local e retorna ao início correto.
- Não há bearer demo, sessão Student customizada ou role switch exercendo
  autorização.
- TrainerApi e StudentApi continuam separadas sobre Domain/Application/
  Infrastructure/DbContext/PostgreSQL compartilhados.

## Não escopo

- Organizations, equipe de Trainer, RBAC avançado, impersonation ou convite de
  equipe;
- billing, LGPD completa, MFA obrigatório, SSO, social-login obrigatório ou
  recuperação customizada;
- split físico dos apps, push, WhatsApp, Chat ou novas regras de prescrição.

## Dependências e decisões operacionais

- O dono do projeto conclui login no Clerk CLI e fornece apenas a configuração
  não secreta que o app precisar. Secrets são inseridos diretamente no Railway
  ou User Secrets locais.
- Antes de M5A-001, confirmar no dashboard Clerk o método de autenticação
  desejado (e-mail/senha, código por e-mail ou ambos) e os ambientes
  development/production.
- A implantação remove o acesso anônimo dos dados demo. Caso seja necessário
  demonstrar a carteira seeded, isso precisa de uma conta Clerk vinculada de
  forma administrativamente controlada; não será inferida de e-mail.
