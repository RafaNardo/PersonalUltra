# Trainer Training — M3 Refactor UX

Status: **approved direction for M3 refactor**

This document supersedes the simplified Trainer workout-template UI delivered in the first M3 implementation. The Student workout experience should preserve the successful SVR-derived flow and only be adapted where required to consume the new catalog-backed prescription model.

## Product intent

Training prescription is a core Personal Ultra experience. The Trainer must be able to assemble a complete workout quickly from a curated exercise catalog instead of typing arbitrary exercise names or creating a one-exercise template.

V1 optimizes for:
- fast prescription;
- reuse of curated exercises;
- clear configuration of sets, repetitions, rest and notes;
- easy reuse through Trainer templates;
- safe snapshot application to a Student;
- premium mobile UX consistent with the Personal Ultra dark visual system.

## V1 exercise source

The exercise catalog is **system-managed and seeded in the database**.

V1 explicitly has no catalog admin UI.

Catalog administration, media upload and exercise curation UI belong to V2.

The V1 seed should contain enough exercises for a convincing commercial demo and should reuse the existing SVR exercise names/assets where available.

Each catalog exercise should be stable and reusable by many templates and Student workouts. At minimum the UI needs:
- name;
- primary muscle group/category;
- image reference;
- optional instructions;
- active/inactive status for future-proofing.

Video is a V2 concern. The V1 model may reserve a nullable media field if this is cheap and does not increase scope, but no video workflow should be built.

## Trainer flow

### 1. Students

Trainer selects a Student from the existing Students area.

The workout flow must feel student-centric. `Meus treinos` may still exist as a template/library surface, but the primary prescription path starts from the Student detail.

### 2. Student > Training

Show the Student identity and a Training tab/section with the current weekly workouts.

Example:

```text
Ana Carolina
Objetivo: Hipertrofia

[ Treinos ] [ Evolução ]

Treinos da semana                         + Novo treino

Upper A                     Publicado
Segunda • 6 exercícios

Lower A                     Publicado
Terça • 7 exercícios

Upper B                     Publicado
Quinta • 6 exercícios

Lower B                     Publicado
Sexta • 7 exercícios
```

Actions per workout:
- open/edit;
- duplicate;
- delete/archive when allowed;
- change recommended weekday;
- create from a Trainer template when useful.

V1 does not need a sophisticated calendar builder. A clear recommended weekday per workout is enough for the demo.

### 3. Workout editor

The editor is a real multi-exercise editor, not a CRUD form for a single exercise.

Header:
- workout name;
- recommended weekday;
- publication state;
- overflow actions.

Body:
- ordered exercise list;
- image thumbnail;
- exercise name;
- prescription summary;
- drag/reorder affordance;
- edit affordance;
- add exercise button.

Example:

```text
Upper A                                Publicado
Segunda-feira

6 exercícios                         + Adicionar exercício

[img] Supino reto com barra
      4 séries • 8–12 reps
      Descanso: 90s

[img] Remada curvada com barra
      4 séries • 8–12 reps
      Descanso: 90s

[img] Desenvolvimento com halteres
      3 séries • 10–12 reps
      Descanso: 60s

...

[ Publicar alterações ]
```

The Trainer must be able to add multiple exercises in the same workout before publishing.

## Add exercise

`Adicionar exercício` opens the seeded exercise catalog.

Required V1 interactions:
- text search;
- muscle/category filters;
- image thumbnail/grid or rich list;
- select an exercise;
- configure prescription before insertion.

Example filters:

```text
Todos | Peito | Costas | Ombros | Braços | Pernas | Glúteos
```

The catalog should not allow arbitrary free-text exercise creation in the primary V1 flow.

If a Trainer cannot find an exercise, V1 may show a non-functional informational affordance such as `Não encontrou? Solicitar exercício`, but it must not create an admin workflow.

## Exercise details / configuration

Selecting a catalog exercise opens its detail/configuration surface.

Display:
- exercise name;
- image;
- category/equipment where available;
- optional instructions from the catalog;
- prescription controls.

Trainer-editable prescription fields:
- series;
- repetition minimum;
- repetition maximum;
- rest seconds;
- Trainer notes.

Example:

```text
Supino reto com barra
Peito • Barra

[ exercise image ]

Séries           4
Repetições       8 — 12
Descanso         90 segundos
Observações      "Manter escápulas retraídas."

[ Adicionar ao treino ]
```

There is **no recommended load in V1**. The Trainer prescribes sets/repetition range/rest/notes. The Student records actual load while executing.

## Reordering

Workout exercises must have explicit sequence/order.

Preferred UX is drag-and-drop/reorder. If reliable drag-and-drop is disproportionately expensive for the demo, provide an equally clear deterministic move-up/move-down interaction, but the domain/API must support arbitrary sequence ordering.

## Publication behavior

Trainer edits should have a clear final action such as `Publicar alterações` / `Salvar treino`.

The demo does not need enterprise-grade draft/version infrastructure. The UX should simply make it obvious when the Student-visible prescription was saved.

Do not implement speculative versioning infrastructure only because the mockup uses the word `Publicado`.

## Templates

Trainer templates are reusable accelerators, not the central object of the Trainer UX.

V1 templates should support:
- multiple exercises;
- full prescription fields;
- duplication;
- application to a Student by snapshot copy.

The template library should remain usable with dozens of items: use a compact searchable list that opens a read-only detail surface. Editing, deleting and creating a new model from an existing one belong to that detail surface, not to every list row.

The Student workout list exposes one primary `Adicionar treino` action. A short
choice screen then explains `Criar do zero` and `Usar um modelo`; the management
screen should not force both paths to compete as equal buttons.

Template discovery combines name search with muscle-group filters derived from
the catalog exercises currently present in each template. A template may appear
in several groups. V1 does not store or maintain a second manual category field.

`Create new from this model` prepares a local-only draft before any API mutation. The local payload must carry an explicit schema version; an unreadable or unsupported version is discarded safely instead of being migrated implicitly. Saving the new model persists it through the normal Trainer API and removes the local draft. This is lightweight mobile draft resilience, not enterprise template versioning.

A template references catalog exercises while being edited. Applying it to a Student creates a Student-owned editable workout snapshot. Later template changes do not mutate already-applied Student workouts.

## Student experience

The existing Student training experience is the baseline and should not be redesigned during this refactor unless needed to display catalog media or repetition ranges correctly.

Preserve:
- recommended workout presentation;
- ability to choose another available workout;
- workout exercise list with images;
- execution one exercise/set at a time;
- actual weight and repetitions entry;
- offline workout foundations where already working.

The Student should receive the same image/instructions context from the prescribed exercise snapshot/catalog-backed model.

## Visual direction

Trainer workout surfaces use the Personal Ultra dark system:
- background `#080808`;
- raised surfaces around `#151515`;
- titanium/off-white text;
- Ultra orange `#FF6A13` for primary actions and highlights;
- green only for semantic states such as success/published.

Avoid white CRUD screens, generic admin-table aesthetics and purple/blue SaaS styling.

## Explicit non-goals — V1

Do not implement:
- exercise catalog admin UI;
- media uploader;
- video management/playback requirements;
- AI workout generation;
- recommended load;
- complex program periodization;
- microservices/event sourcing;
- template versioning infrastructure;
- custom free-text exercise creation as the normal prescription path.

## Acceptance experience

A demo is acceptable only when this complete path works:

1. Trainer opens a Student.
2. Trainer sees several workouts assigned to the Student.
3. Trainer opens `Upper A`.
4. Trainer sees multiple exercises with images and prescription summaries.
5. Trainer taps `Adicionar exercício`.
6. Trainer searches/filters the seeded catalog.
7. Trainer selects an exercise and configures sets, rep range, rest and notes.
8. Exercise is inserted into the workout.
9. Trainer reorders exercises.
10. Trainer saves/publishes.
11. Student sees the updated workout using the established Student UX.
12. Student executes it and records actual load/repetitions.
13. Trainer can see the resulting session/history.
