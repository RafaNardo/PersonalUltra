# Student feature

This feature owns the Student invitation, session and anamnesis flow. It also
owns the actor-local SQLite/offline workout foundation retained for the future
workout experience. It must not import from `features/trainer`.

The Expo Router files for login, invitations and Student access compose this
feature. The retired SVR workout, nutrition, Coach and progress routes do not
remain as fallback routes.
