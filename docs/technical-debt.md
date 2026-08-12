# Débito técnico

Itens deliberadamente simplificados para a demonstração e que precisam de revisão antes de uso em produção.

## Fotos de avaliação e evolução

As fotos em `apps/mobile/assets/progress-model/` são assets locais de demonstração, fornecidos como imagens geradas. O carrossel não representa um sistema de fotos de usuários.

As imagens de exercícios em `apps/mobile/assets/training/` são assets locais de demonstração. Antes de produção, sua origem, licença e adequação de uso comercial devem ser verificadas e registradas.

`desenvolvimento-com-halteres.png` foi gerada para a demonstração com uma ilustração de mulher. Antes de produção, validar sua licença, origem e adequação comercial junto aos demais assets de exercício.

Antes de permitir fotos reais, definir e implementar:

- consentimento explícito, finalidade e possibilidade de revogação;
- política de privacidade, retenção e exclusão definitiva;
- armazenamento privado, criptografia, controle de acesso por membro e URLs de upload/consulta temporárias;
- remoção de metadados EXIF e tratamento de cópias/redimensionamentos;
- redimensionamento, compressão e cache para não embutir imagens de alta resolução no bundle do aplicativo;
- fluxo de denúncia/remoção e trilha de auditoria;
- revisão de LGPD e de requisitos aplicáveis a imagens corporais e dados de saúde;
- proibição de inferências biométricas, clínicas ou de composição corporal a partir da imagem sem uma decisão de produto e validação apropriada.

Os endpoints previstos em `docs/architecture/api.md` para upload de fotos não devem ser expostos como funcionais até que esses pontos estejam implementados.

## Imagens de interface licenciadas

Os assets fornecidos pela SVR em `docs/assets/treinos/` e `docs/assets/avatar.png` estão documentados em `docs/design/brand-guidelines.md`. Antes de distribuição pública, confirmar escopo de licença, atribuição quando exigida e versão final aprovada de cada arquivo.

## Registro de séries e linguagem de esforço

O registro atual pede RIR (*repetições em reserva*), um conceito técnico que boa parte dos alunos não conhece. Antes de produção, validar uma experiência mais acessível, com linguagem em português e escolhas rápidas, por exemplo:

- foi fácil / foi difícil;
- completei todas as repetições / falhei antes da meta;
- ainda conseguiria fazer mais algumas / me esgotei;
- seleção por toque em vez de text boxes e digitação de valores técnicos quando possível;
- revisão da entrada de carga e repetições para reduzir o número de toques e evitar que o aluno precise preencher campos manualmente a cada série.

O desenho deve preservar a rastreabilidade do esforço para a metodologia — inclusive uma conversão explícita e validada para RIR, quando aplicável — sem fingir precisão clínica. A decisão de UX, a taxonomia final das opções e qualquer regra de conversão precisam ser validadas com o Coach antes de substituir o fluxo atual.
