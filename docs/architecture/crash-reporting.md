# Crash reporting local (M1)

O aplicativo registra falhas de renderização e exceções globais através de
`apps/mobile/src/platform/telemetry.ts`. Na demonstração, o registro é apenas
local em desenvolvimento e não usa SDK, credencial ou serviço externo.

O payload é propositalmente mínimo: categoria do erro, escopo (`render` ou
`unhandled`) e indicação de fatalidade quando fornecida pelo runtime. Mensagens,
stacks, conteúdo do Coach, identificadores e dados de saúde não são enviados.

O handler global preserva o handler anterior do React Native para não impedir o
comportamento padrão de desenvolvimento. A `AppErrorBoundary` oferece uma tela
de recuperação para erros de renderização.

Para produção, a escolha de um provedor, consentimento, retenção e política de
privacidade precisam ser definidos antes de conectar qualquer integração externa.
