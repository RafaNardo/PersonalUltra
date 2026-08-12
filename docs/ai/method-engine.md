# Method Engine v0.1

## Layers
`Methodology → Engines → Safety → Coach`

## Versioning
Every Plan references an immutable MethodologyVersion.

## Rule types
TrainingSelection, TrainingVolume, Progression, Fatigue, ExerciseSubstitution, Scheduling, HealthConstraint, NutritionTarget, FoodSubstitution, NutritionAdjustment, PlanReview.

## Workout Engine
BuildPlan, RecommendLoad, EvaluateProgression, FindExerciseAlternatives, RescheduleWorkout, EvaluateFatigue.

Returns structured decisions with reason codes and applied rules.

## Nutrition Engine
Targets, meal strategy, food substitutions, adherence and adjustments.

## Plan Review
Uses adherence, performance, trends, check-ins, pain and workout completion; should not overreact to isolated datapoints.

## Rule Book
Approved methodology knowledge becomes explicit rules with codes and status.

## Golden Tests
Synthetic approved cases must run in CI to avoid methodology regressions.
