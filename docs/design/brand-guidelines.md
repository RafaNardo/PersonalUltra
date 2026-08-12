# SVR Brand Guidelines — App

## Fonte e licença

O aplicativo usa `Montserrat` em toda a interface, com os pesos Regular, Medium, SemiBold, Bold e ExtraBold. Os arquivos estão em `apps/mobile/assets/brand/` e são disponibilizados pelo Google Fonts.

`MonumentExtended-UltraBold` permanece em `apps/mobile/assets/brand/` como alternativa autorizada pela SVR e exige licença válida para redistribuição e uso em aplicativo. Para voltar a ela, alterar os `fontFamily` dos tokens em `apps/mobile/src/design/tokens.ts`.

Se nenhuma das fontes for adequada, usar a fonte do sistema temporariamente. O texto de interface deve permanecer na fonte do sistema até que uma fonte de interface licenciada seja definida.

## Assets oficiais

- Logo original: `apps/mobile/assets/brand/svr-logo.png`
- Logo para fundo escuro: `apps/mobile/assets/brand/svr-logo-transparent.png`
- Ícone: `apps/mobile/assets/brand/app-icon.png`
- Origem: https://svrhouse.com.br/

## Imagens licenciadas para a interface

- Avatar do SVR Coach: `docs/assets/avatar.png`
- Exercícios: `docs/assets/treinos/`

Esses arquivos foram fornecidos pela SVR como imagens licenciadas. O avatar está incorporado em `apps/mobile/assets/avatar.png` e as imagens usadas nos cards de treino estão em `apps/mobile/assets/training/`; o mapeamento explícito por nome de exercício fica em `apps/mobile/src/design/exercise-media.ts`. Não usar imagens de mockups como assets de produção.

Os assets são usados no ícone do aplicativo, na splash nativa e nas telas de carregamento. Alterações de logo, recorte de ícone ou cor devem partir de arquivos oficiais aprovados pela SVR.

## Estilo

- Base: `#020202`; superfícies escuras e texto branco.
- Cor de ação SVR: `#DF001A`.
- Direção: escura, forte, premium e esportiva; alto contraste, pouco ruído e foco em acompanhamento e resultado.
- Interface: Montserrat em todos os pesos; alternativa autorizada: Monument Extended UltraBold.

## Splash

A splash é nativa, com fundo `#020202` e logo SVR centralizado. Depois que fontes e base local estão prontas, ela faz uma transição curta de fade para a tela de bootstrap do aplicativo.
