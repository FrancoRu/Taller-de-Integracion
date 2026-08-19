# Copilot Instructions

## Project Guidelines
- User prefers bulk, one-shot code changes and does not want per-file apply interactions; expects automatic file modifications without manually pressing Apply for each file.
- User wants a VS Code-like Copilot edit workflow: give instruction, AI applies changes automatically, then user decides Keep or Undo without per-file Apply.
- For tournaments, `CreateTournamentRequest` should not include a `Status` field because new tournaments must always default to `Scheduled` on the backend.

## Code Style
- Use constant naming in uppercase with underscores (e.g., `POINTS_FOR_WIN`) for constants in this codebase.

## Migration Workflow
- User prefers using Package Manager Console for EF migrations and wants the simplest Add-Migration workflow instead of CLI commands.