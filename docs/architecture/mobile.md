# Mobile Architecture

## Demo
Um único app Expo com role switching dev/demo-only.

```text
app/
  demo-role-switch/
  trainer/
  student/

src/
  features/
    trainer/
    student/
  api/
    trainer-client.ts
    student-client.ts
    shared-http.ts
  shared/
```

## Futuro
```text
apps/
  trainer-mobile/
  student-mobile/
```

## Regras de splitabilidade
- `features/trainer/*` não importa `features/student/*`;
- `features/student/*` não importa `features/trainer/*`;
- cada actor possui navegação, hooks e queries próprios;
- role switching serve somente para composição da demo;
- regras de autorização ficam no backend;
- API clients permanecem separados mesmo dentro do mesmo app;
- evitar estado global misturando dados dos dois atores;
- compartilhar primitives de UI, formatação, HTTP, mídia de exercícios e utilitários;
- não compartilhar telas inteiras apenas para reduzir pequena duplicação.

## Offline
Offline é prioritário na execução de treino do Student. O editor do Trainer não precisa ser offline na demo.

Imagens de exercício passam pela primitive compartilhada `ExerciseImage`.
O catálogo não empacota imagens no Expo: `expo-image` baixa WebP 640 px, mantém
cache em disco com chave estável `imageRef` e mostra placeholder quando os bytes
ainda não estiverem disponíveis offline. URLs assinadas não são persistidas nos
snapshots SQLite nem em drafts locais.
