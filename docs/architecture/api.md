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
- `GET /workouts`
- `POST /workout-sessions`
- `POST /workout-sessions/{id}/sets`
- `POST /workout-sessions/{id}/complete`
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

O registro de séries é idempotente e contíguo dentro de cada exercício, sem
obrigar a ordem entre exercícios. A conclusão retorna conflito enquanto faltar
qualquer série persistida; o progresso das respostas é derivado das performances
reais, e não apenas de um contador cliente.

## Fronteiras
Student API não expõe mutations de prescrição.
Trainer API sempre valida ownership do Student.

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
