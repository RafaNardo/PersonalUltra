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

Durante a transição M3RF, as respostas Trainer de treino do aluno expõem
`suggestedOrder` de forma aditiva, preservando temporariamente `recommendedDay`
e `isRecommended`. Criar do zero ou aplicar um modelo já reserva no servidor a
próxima ordem persistida; os requests legados continuam válidos até suas telas
serem migradas em `PU-M3RF-002`.

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

As respostas de lista e preview de treino Student também incluem
`suggestedOrder` de forma aditiva. Neste primeiro gate, a composição legada de
`recommended`/`available` e seus campos antigos permanece intacta; sua
neutralização pertence a `PU-M3RF-003`.

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
