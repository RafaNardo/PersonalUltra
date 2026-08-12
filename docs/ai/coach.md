# SVR Coach v0.1

## Pipeline
`Message → Intent → Context Router → LLM → Tool Request → Application → Engine → Safety → Proposal → Confirmation → Persist`

## Initial intents
ASK_GENERAL, ASK_TRAINING, ASK_NUTRITION, CHANGE_EXERCISE, CHANGE_WORKOUT_DAY, CHANGE_FOOD, REPORT_PAIN, REPORT_FATIGUE, REPORT_EQUIPMENT, CHECK_PROGRESS, CHECK_PLAN, UNKNOWN.

## Read tools
get_member_summary, get_active_plan, get_today_workout, get_recent_training_history, get_today_nutrition, get_progress_summary, get_recent_checkins.

## Action tools
request_exercise_substitution, request_workout_reschedule, request_food_substitution, record_pain, record_feedback, request_plan_review.

No unrestricted write tool.

## Memory
Profile Memory, Operational Memory, Conversation Memory.

## Structured UI
Text, ActionProposal, ChoiceMessage, ProgressInsight.

## Structured output contract
Every persisted assistant message uses one of `Text`, `Choice`, `ActionProposal` or
`ProgressInsight`. Its `MetadataJson` contains `reasonCode`, `messageType`,
`requiresUserInput` and `requiresConfirmation`.

`ActionProposal` always has `requiresConfirmation: true`. A proposal without an
`actionId` is presentation-only. A server-created proposal with an `actionId` may
be confirmed or rejected by its owner; confirmation revalidates the rule and
safety constraints before its specific domain mutation. Unsupported kinds, blank
content and invalid reason codes are rejected before persistence.

## Pain flow v0
After a pain report is classified by Safety v0, the API persists a text message
from the SVR Coach in that member's conversation using the safety reason code.
The message is guidance only: no exercise substitution, plan mutation, or other
automatic action is created.

## Fatigue flow v0
For a fatigue report, the Coach states that no load/volume adjustment is approved
by default. When a planned workout exists, it persists Yellow proposals for a
rest day and, only if the following day has no scheduled workout, rescheduling to
that day. Both require explicit confirmation; no workout changes on the fatigue
message itself.

## RAG future
Classify sources by category and authority so current official protocols outrank older social content.
