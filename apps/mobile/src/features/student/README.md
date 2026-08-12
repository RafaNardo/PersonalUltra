# Student feature

This feature owns Student navigation, API client/contracts, state, offline
workout persistence, hooks and screens. It must not import from
`features/trainer`.

The Expo Router files under `app/student` compose this feature. Routes outside
that group which support the Student demo entry (login, onboarding and plan
preparation) import Student code only from this directory.
