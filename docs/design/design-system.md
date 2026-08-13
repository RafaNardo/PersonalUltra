# Design System — Personal Ultra

Direção visual: premium, dark, técnica e esportiva, inspirada na sensação de performance da linha Apple Watch Ultra sem copiar identidade proprietária, assets ou layouts.

Os assets temporários licenciados para a identidade-base estão em
`docs/assets/brand/`. Eles serão integrados no `PU-M0-008`; até então, assets
visuais herdados não devem orientar novas decisões de branding.

## Base
- background `#080808`
- surface `#151515`
- surfaceRaised `#222220`
- titanium `#B8B3A9`
- titaniumLight `#E8E3DA`
- textPrimary `#F5F5F3`
- textSecondary `#AAA8A1`
- border `#353530`
- ultraOrange `#FF6A13`
- signalGreen `#B8F500`

O verde é pontual.

## White-label Student
`TrainerBranding.PrimaryColor` pode substituir o accent principal na experiência do aluno.

Customizável:
- profile photo;
- logo;
- primary color;
- cover image;
- methodology name;
- Coach avatar.

Não permitir que branding substitua cores semânticas de erro/sucesso/acessibilidade.

## Linguagem por actor
Trainer UI: mais operacional, densa e orientada a dashboard/atividade.

Student UI: mais visual, emocional, com CTAs maiores e maior presença da marca do personal.

## Empty states

Empty state é uma orientação de produto, não uma mensagem de erro. Ele deve confirmar o estado atual, explicar o que acontecerá e oferecer uma próxima ação somente quando ela for real.

Estrutura padrão:

1. marcador visual simples no accent da experiência;
2. status curto em caixa alta, sem linguagem de falha;
3. título humano e específico;
4. explicação que deixa claro se a ação depende do Student, Trainer ou do sistema;
5. até três itens de prévia quando ajudam a entender o conteúdo futuro;
6. uma ação real e direta, quando disponível;
7. nota final opcional para reduzir incerteza.

Variantes do primitive compartilhado `EmptyState`:

- `page`: uma área inteira ainda não possui conteúdo; usa maior presença visual e pode listar o que aparecerá ali;
- `section`: lista ou editor principal vazio; mantém marcador, contexto e ação sem dominar a tela;
- `inline`: ausência secundária dentro de dashboard/card; remove o círculo, mas preserva status, título e explicação.

Regras de conteúdo:

- não usar apenas `Nenhum item`;
- não culpar o usuário nem sugerir erro quando o estado é válido;
- não prometer prazo, notificação ou automação inexistente;
- não exibir CTA sem destino funcional;
- busca sem resultado deve permitir limpar o filtro;
- Trainer deve receber linguagem operacional (`Convide`, `Adicione`, `Crie`);
- Student deve receber linguagem acolhedora e explicar quando o conteúdo depende do personal;
- loading, erro e empty state são estados distintos e não devem compartilhar copy.
