# Safety Engine v0.1

## Green
Routine action within approved rules: equipment substitution, safe reschedule, approved food swap.

## Yellow
Requires more context/confirmation: fatigue, performance drop, mild pain or meaningful routine change.

## Red
Do not automate: severe pain, acute symptoms, unknown high-risk context, diagnosis/treatment requests or actions conflicting with restrictions.

## Rules
Pain is not equivalent to equipment unavailability. The system does not diagnose. A health condition does not automatically map to simplistic exercise bans unless explicit methodology rules exist. Health ambiguity fails safely.

## Pain v0 rules
- `PAIN_LOW_INTENSITY`: intensity 0–3 with context → Green.
- `PAIN_MODERATE_INTENSITY`: intensity 4–6 with context → Yellow and confirmation required.
- `PAIN_HIGH_INTENSITY`: intensity 7–10 with context → Red and no automated action.
- `PAIN_CONTEXT_INCOMPLETE` or `PAIN_INTENSITY_INVALID` → Red by the conservative ambiguity rule.

The decision is persisted with the report for auditability. It classifies risk only;
it does not diagnose, prescribe treatment, or alter a workout.
