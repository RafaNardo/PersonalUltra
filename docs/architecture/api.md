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
- `GET/PUT /settings/prescription`
- `POST /students/{id}/training/from-template/{templateId}`
- `GET/PUT /students/{id}/training`
- `DELETE /students/{id}/workouts/{workoutId}` (remoção lógica; preserva histórico)
- `PUT /students/{id}/training/schedule`
- `GET/PUT /students/{id}/nutrition`
- `POST /students/{id}/messages`

### Dashboard inicial
`GET /dashboard` é autenticado como Trainer e retorna somente alunos com vínculo
ativo desse Trainer, além das contagens de anamnese pendente/concluída. Métricas
de treino, peso e atividade entram quando seus respectivos fluxos estiverem
associados ao `Student`; o endpoint não inventa esses dados durante a transição.

`GET /training/templates` inclui os grupos musculares distintos derivados dos
exercícios de cada modelo para busca/filtro no mobile; não existe categoria
manual duplicada no modelo de domínio.

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
- `GET /workouts/recommended`
- `POST /workout-sessions`
- `POST /workout-sessions/{id}/sets`
- `POST /workout-sessions/{id}/complete`
- `POST /sync`
- `GET /nutrition`
- `GET /progress/weight`
- `POST /progress/weight`
- `GET /coach/conversation`
- `POST /coach/messages`

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
