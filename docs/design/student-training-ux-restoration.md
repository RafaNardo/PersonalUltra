# Student Training UX Restoration — M3RR

Status: **implemented and validated (M3RR, 2026-08-13)**

The superseded condensed execution implementation remains recoverable from Git
commit `99df898` (`feat(m3r-010): adapt student workout execution`). The old
Expo paths are compatibility redirects only; the live Student tree has one
implementation for the hub, preview, session overview, focused execution,
rest and summary routes. Validation passed with mobile typecheck, .NET build,
46 integration tests and Android Expo export. Podman was installed but its
local machine/socket did not respond, so container-backed live flow validation
was not available in this gate.

## Why this corrective track exists

M3R corrected the training catalog, prescription model, snapshots, Trainer editor, demo seed and offline idempotency. Its Student adaptation retained the data but not the established interaction model: all exercises and set inputs were placed on one long screen.

The successful donor flow was intentionally guided:

```text
Student Home
  -> recommended or available workout
  -> workout preview
  -> start or resume
  -> one exercise / one set at a time
  -> rest
  -> next set or exercise
  -> real session summary
```

M3RR restores that interaction rhythm using Personal Ultra data, branding and actor boundaries. It is not a restoration of the SVR product model.

## Valuable behavior in the current Ultra screen

The post-M3R condensed screen must not be treated as discarded work. Although it is too dense to be the primary execution experience, it solved useful problems:

- the complete workout is visible in one place;
- each exercise shows image, sequence and prescription context;
- completed-set progress is immediately comparable across exercises;
- instructions and Trainer notes remain easy to inspect;
- offline state and locally queued sets are visible in the workout context;
- completion is guarded while pending sets still need synchronization;
- the Student can understand how much of the whole session remains.

These qualities become a **session overview**, not a second implementation of set entry.

### Recovery reference

The condensed implementation being superseded is available from Git commit `99df898` (`feat(m3r-010): adapt student workout execution`), principally at:

- `apps/mobile/app/student-training.tsx`;
- `apps/mobile/app/student-training/[id].tsx`;
- `apps/mobile/src/features/student/invite/api.ts`;
- `apps/mobile/src/features/student/offline/training-db.ts`.

If a later regression requires comparison, inspect or restore individual files from that commit rather than keeping dead duplicate routes in the live Expo tree. The offline hardening added after that point must not be rolled back when consulting the reference.

## Approved hybrid interaction

The restored flow combines the donor's focus with the current Ultra screen's overview:

```text
Workout preview (session not created)
  -> Start workout
  -> Session overview
       - global progress
       - ordered exercise cards
       - completed/current/pending state
       - offline/sync state
       - open current exercise
  -> Focused exercise
       - one prescribed exercise
       - one next set form
       - actual weight + repetitions
  -> Rest
  -> Focused exercise (next set) or session overview (exercise complete)
  -> Summary
```

The default progression after saving a set remains guided. The Student may return to the session overview to understand the whole workout or reopen the current incomplete exercise. The overview must not expose independent set forms for every exercise, because that recreates the cognitive load M3RR is correcting.

### Session overview responsibilities

The overview should display only persisted or locally queued facts:

- workout name and status;
- completed sets / prescribed sets;
- ordered exercise list;
- image and prescription summary per exercise;
- instructions/Trainer notes when expanded or opened;
- exercise state: completed, current or pending;
- session connectivity/synchronization state;
- primary action: start current exercise, continue current exercise or finish when allowed.

It must not:

- register sets inline;
- allow arbitrary completion of future exercises;
- calculate recommended load;
- invent calories, duration or performance scores;
- silently discard pending offline operations.

### Focused execution responsibilities

The focused screen owns the only set-entry form and displays:

- current exercise identity and media;
- its position in the workout;
- prescription and rest duration;
- catalog instructions and Trainer notes;
- current set number and exercise progress;
- actual weight and actual repetitions;
- save feedback: confirmed by API or queued locally.

This division makes the future physical Student app extraction easier: overview and execution remain actor-local screens backed by the same Student session state rather than two competing implementations.

## Preserve from the donor

- independent Student tab navigation;
- recommended workout as the primary action;
- weekly workout visibility;
- visual workout preview with exercise media;
- explicit start and resume actions;
- focused exercise/set execution;
- a whole-session overview for orientation and progress inspection;
- actual weight and repetitions entry;
- rest timer and clear transitions;
- completion summary;
- resume and offline foundations.

## Do not restore

- SVR branding or methodology language;
- recommended load;
- mandatory RIR;
- exercise substitution or Coach mutations;
- automatic plan decisions or generation;
- mobile-authored workout data or metrics;
- legacy Member-based flows.

## Navigation boundary

Student routes belong to an independently extractable tree under `app/student`. Business state and actor API access remain under `features/student`. Trainer code must not be imported by Student code.

The visible tabs are:

- Início;
- Treino;
- Coach;
- Nutrição;
- Progresso.

Workout preview, active execution, rest and summary are internal Student routes and hide the tab bar to preserve focus.

The active-session overview also hides the tab bar. It is a focused workout surface, not the public Treino tab.

## Data and API rules

- Opening a workout preview is read-only and must not create `WorkoutSession`.
- Starting a workout creates or returns its existing in-progress session.
- Resume state is server-authoritative when connected and hydrated from the Student SQLite snapshot while offline.
- The session overview and focused exercise must consume the same hydrated session state; neither screen keeps a competing copy of workout progress.
- Set registration remains idempotent and records only actual weight/repetitions.
- A summary must derive from persisted session/performance data. The mobile must not invent duration or performance claims.
- A pending offline queue must be synchronized before final completion.

## Visual direction

Reuse the donor's hierarchy and interaction rhythm, not its red palette or product copy. Personal Ultra tokens remain authoritative: dark background, raised surfaces, titanium text, Ultra orange actions and semantic colors only for state.

The supplied M3 mockup remains a reference for compact exercise cards and focused Student execution. It is not authority for recommended load or other excluded fields shown in illustrative screens.

## Delivery gates

Implement `PU-M3RR-001` through `PU-M3RR-008` sequentially. Each gate must be reviewed and validated before the next begins. Do not collapse the track into a broad rewrite.

When the current condensed execution route is replaced, its useful behavior must first exist in the session overview and shared Student session state. Do not delete it merely because the focused screen renders successfully.

## Regression gates

- Trainer and Student API surfaces remain distinct.
- No imports cross between `features/trainer` and `features/student`.
- Trainer still sees the real session/set history.
- Existing invite, onboarding, nutrition, progress and read-only Coach flows remain reachable.
- Offline operations do not persist actor tokens and do not duplicate sets.
- Mobile typecheck and relevant .NET tests pass after every gate.
