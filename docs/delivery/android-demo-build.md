# Build Android de demonstração (M1)

O perfil versionado para distribuição interna é `demo` em
`apps/mobile/eas.json`. Para Android ele fixa `buildType: apk`, adequado para
instalação direta na demonstração. A identidade do app é
`com.svrmethod.demo`, com `versionCode: 1`, definida em `apps/mobile/app.json`.

## Validações locais

```sh
cd apps/mobile
npx expo config --type public
npm run typecheck
```

Confirme que a configuração resolvida contém `android.package`, `versionCode` e
o adaptive icon.

## Build interno

Com uma conta Expo/EAS autorizada, execute:

```sh
cd apps/mobile
npx eas build --profile demo --platform android
```

Esse comando envia um build remoto. A assinatura pode usar a keystore gerenciada
pela EAS ou uma keystore da organização, que não deve ser incluída no
repositório. Nenhum build remoto nem credencial é disparado ou solicitado por
este projeto automaticamente.
