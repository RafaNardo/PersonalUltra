# API Surfaces

Base: `/api/v1`

## Trainer API
- `GET /dashboard`
- `GET /students`
- `GET /students/{id}`
- `GET /students/{id}/anamnesis`
- `GET /students/{id}/progress/weight`
- `POST /student-invites`
- `GET/POST/PUT/DELETE /training/templates`
- `GET /training/exercises`
- `GET/PUT /settings/prescription`
- `POST /students/{id}/training/from-template/{templateId}`
- `GET/PUT /students/{id}/training`
- `DELETE /students/{id}/workouts/{workoutId}` (remoção lógica; preserva histórico)
- `PUT /students/{id}/workouts/order`
- `GET/PUT /students/{id}/nutrition`
- `GET/POST /nutrition/templates`
- `GET/PUT/DELETE /nutrition/templates/{templateId}`
- `POST /nutrition/templates/{templateId}/duplicate`
- `POST /students/{id}/nutrition/meals/from-template/{templateId}`
- `POST /students/{id}/messages`

### Dashboard inicial
`GET /dashboard` é autenticado como Trainer e retorna somente alunos com vínculo
ativo desse Trainer, além das contagens de anamnese pendente/concluída. Métricas
de treino, peso e atividade entram quando seus respectivos fluxos estiverem
associados ao `Student`; o endpoint não inventa esses dados durante a transição.

`GET /training/templates` inclui os grupos musculares distintos derivados dos
exercícios de cada preset para busca/filtro no mobile; não existe categoria
manual duplicada no modelo de domínio. As rotas mantêm `templates` como nome
técnico compatível, enquanto a UI usa `preset de treino`.

As respostas Trainer de treino do aluno expõem `suggestedOrder`. Criar do zero
ou aplicar um preset reserva no servidor a próxima posição persistida, sem
receber dia da semana ou indicador de recomendação. A reordenação exige todos os
treinos ativos do aluno exatamente uma vez e sempre valida o vínculo com o
Trainer autenticado. Não existem campos de dia ou recomendação no domínio ou
nos contratos finais.

Todo DTO de exercício mantém o `imageRef` estável. Quando ele usa o esquema
`media://`, as duas APIs acrescentam uma `imageUrl` HTTPS assinada somente na
resposta. O catálogo atual usa somente WebP remotos de entrega; ausência de URL
é tratada pelo placeholder/cache offline do mobile. Credenciais do bucket nunca
entram nos contratos nem no app.

## Student API
- `GET /bootstrap`
- `GET /home`
- `GET /invite/{token}`
- `GET /invite/code/{code}`
- `POST /invite/code/{code}/accept`
- `POST /auth/student-login` (somente demo; verifica um `Student` já cadastrado)
- `PUT /anamnesis`
- `POST /anamnesis/complete`
- `GET /home/trainer-message`
- `GET /training`
- `GET /training/{workoutId}`
- `POST /training/{workoutId}/start`
- `GET /training/sessions/{sessionId}`
- `POST /training/sessions/{sessionId}/exercises/{exerciseId}/sets`
- `POST /training/sessions/{sessionId}/exercises/{exerciseId}/confirm`
- `POST /training/sessions/{sessionId}/complete`
- `POST /sync`
- `GET /nutrition`
- `GET /progress/weight`
- `POST /progress/weight`
- `GET /coach/conversation`
- `POST /coach/messages`

As respostas de lista e preview de treino Student usam `suggestedOrder` e uma
coleção neutra `workouts`. Não expõem dia recomendado, indicador de recomendação
nem separam treinos em `recommended`/`available`. A preparação e a prévia são
read-only; somente uma confirmação explícita inicia ou retoma a sessão.

O registro é idempotente e contíguo dentro de cada exercício, sem obrigar a
ordem entre exercícios. Exercícios `Repetitions` recebem carga/repetições;
exercícios `Duration` recebem duração em segundos. A conclusão comum retorna
conflito enquanto faltar qualquer registro persistido. A confirmação explícita
de um exercício, ou `complete?confirmRemaining=true`, marca o que foi realizado
sem criar performances fictícias. As respostas distinguem registros reais de
`confirmedWithoutDetails`.

## Fronteiras
Student API não expõe mutations de prescrição.
Trainer API sempre valida ownership do Student.

Os contratos de nutrição preservam a ordem de refeições e itens, quantidades
com unidade, data da última atualização e o nome do Trainer responsável. O
`PUT` Trainer valida o documento inteiro e o disponibiliza ao Student somente
depois da persistência bem-sucedida; não existe mutation Student, rascunho ou
versionamento na demo.

Cada preset de alimentação pertence ao Trainer autenticado e representa uma
única refeição com seus itens. Aplicá-lo acrescenta um snapshot ao plano do
Student, portanto não remove refeições existentes e edições futuras no preset
não alteram cópias já aplicadas. Se ainda não existe plano, a primeira refeição
cria a estrutura inicial automaticamente.

As APIs compartilham Domain/Application/Infrastructure e banco, mas não devem compartilhar controllers/endpoints específicos dos atores.

## Error contract
```json
{
  "code": "STRING_CODE",
  "message": "Mensagem humana",
  "details": {},
  "traceId": "..."
}
```
