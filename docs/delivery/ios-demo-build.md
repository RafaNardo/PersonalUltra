# Build iOS de demonstração (M1)

O perfil versionado para distribuição interna é `demo` em
`apps/mobile/eas.json`. A identidade iOS da demonstração é
`com.svrmethod.demo`, definida em `apps/mobile/app.json`.

## Validações locais

```sh
cd apps/mobile
npx expo config --type public
npm run typecheck
```

Confirme que a configuração resolvida contém `ios.bundleIdentifier` e que os
assets de ícone, splash e fontes são encontrados pelo Expo.

## Build assinado

Com uma conta Expo/EAS autorizada e credenciais Apple válidas para esse bundle
identifier, execute:

```sh
cd apps/mobile
npx eas build --profile demo --platform ios
```

Esse comando envia um build remoto e pode solicitar login, acesso ao Apple
Developer Program, certificado de distribuição e provisioning profile. Essas
credenciais não pertencem ao repositório e nenhum build remoto é disparado por
este projeto automaticamente.
