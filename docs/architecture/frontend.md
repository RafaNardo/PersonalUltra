# Frontend Architecture v0.1

## Stack
React Native, Expo SDK 54, TypeScript, Expo Router, TanStack Query, Zustand, React Hook Form, Zod e Expo SQLite.

## Features
`auth`, `onboarding`, `home`, `training`, `nutrition`, `coach`, `progress`, `plan`, `health`.

## Estado
Server state no TanStack Query; estado local de UX no Zustand; treino offline em SQLite.

## Offline tables
`cached_workout`, `cached_exercises`, `pending_operations`, `local_sets`.

## API
Screens não chamam fetch diretamente. Usar client base + feature hooks + mutations.

## Coach UI
Union de mensagens: TextCoachMessage, ActionProposalMessage, ChoiceMessage, ProgressInsightMessage.

## Não fazer
Redux sem motivo, server state no Zustand, web/tablet/light mode no MVP, mega-components genéricos.
