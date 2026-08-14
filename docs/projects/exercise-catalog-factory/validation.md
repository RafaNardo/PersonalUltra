# Validação e gates operacionais

## Antes de chamar IA

- input passa schema;
- source hash e run ID foram persistidos;
- taxonomia está carregada;
- duplicidades exatas foram bloqueadas;
- limites `max-items`, concorrência e orçamento foram informados;
- operador viu o plano e confirmou a etapa paga;
- `OPENAI_API_KEY` existe apenas no ambiente;
- dry-run não chama provider.

## Metadados

Validações automatizadas:

- limites de tamanho compatíveis com `Exercise`;
- slug/asset name únicos;
- grupo/equipamento dentro da taxonomia ou `needs_review`;
- instruções não vazias para export Personal Ultra;
- sem linguagem de carga recomendada, diagnóstico ou prescrição clínica;
- JSON do provider estritamente aderente ao schema;
- campos locked preservados.

Validações humanas:

- nome e aliases não criam exercício duplicado;
- grupo primário e equipamento correspondem à variação;
- instrução é curta, compreensível e tecnicamente aceitável;
- visual description representa exatamente a variação aprovada.

## Imagens

Aplicar o checklist de [image-style-guide.md](image-style-guide.md). O item só fica `approved` quando `metadata`, `visual` e `biomechanics` estiverem aprovados para os hashes atuais.

## Consistência do pacote

Para cada item exportado:

- slug único;
- GUID único;
- `ImageRef` único ou compartilhamento explicitamente permitido;
- arquivo existe e SHA-256 confere;
- registry contém `require` literal;
- `require` aponta para o arquivo correto;
- seed e manifesto concordam em todos os campos;
- nenhum arquivo órfão inesperado;
- nenhum item rejeitado/pendente no seed.

## Compatibilidade com o target

O verifier do profile Personal Ultra deve inspecionar, sem alterar:

- schema atual da entidade e limites EF;
- construtor/record usado pelo seed atual;
- política de upsert por slug;
- slugs/GUIDs existentes;
- diretório real de assets;
- formato do media registry;
- grupos musculares suportados pela UI;
- working tree limpa ou alterações claramente reportadas.

Mudança incompatível deve bloquear a geração e pedir atualização do target profile. Não “adaptar” por heurística.

## Validação após aplicação no Personal Ultra

Comandos mínimos atuais:

```powershell
dotnet build PersonalUltra.sln --no-restore
dotnet test tests/PersonalUltra.Api.IntegrationTests/PersonalUltra.Api.IntegrationTests.csproj --no-build
npm run mobile:typecheck
npx expo export --platform ios --output-dir .expo-export-catalog-check
git diff --check
```

Também validar:

- seed duas vezes sem duplicar ou sobrescrever entradas existentes;
- endpoint Trainer lista/pesquisa novos itens;
- todos os grupos relevantes aparecem/filtram corretamente;
- catálogo e configuração de exercício exibem thumbnail;
- preview, execução e resumo Student resolvem a imagem;
- app faz bundle sem `Unable to resolve`;
- banco existente recebe somente itens ausentes conforme a política aprovada;
- snapshots históricos não são reescritos.

## Cenário incremental obrigatório

Segundo run com:

1. um exercício idêntico — cache hit;
2. um exercício novo — gera e exporta;
3. um nome corrigido mantendo `externalKey` — preserva slug/ID conforme decisão;
4. um provável duplicado — bloqueia para review;
5. uma imagem rejeitada — regenera apenas ela;
6. uma falha transitória — retoma sem repetir sucessos;
7. um grupo novo — reporta impacto na UI antes de exportar.

## Relatório de conclusão

Cada run deve terminar com:

- status geral `ready`, `blocked` ou `partial`;
- contagens e lista de pendências;
- custo/tempo observado;
- versões de schema/prompt/style/profile;
- diffs previstos/aplicados;
- comandos executados e resultados;
- decisões humanas ainda necessárias.

Não marcar `ready` se qualquer item destinado ao seed não tiver todas as aprovações ou se o target verifier tiver erro.

