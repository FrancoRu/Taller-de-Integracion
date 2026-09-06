# Proposal: Tournament Cloning (wizard-prefill)

## Intent

Setting up a new tournament that mirrors a prior one (same divisions, zones, stage types, brackets, points, playoff mappings) means re-entering the whole wizard by hand every time. This is a once-a-season, high-stakes, error-prone chore. Organizers need to start a new tournament pre-filled from an existing one's structure, then review/edit before committing — with zero instance data (rosters, matches, standings) carried over.

## Scope

### In Scope
- New additive backend read endpoint returning a `TournamentStructureResponse` DTO tree: divisions (name, category, `IsCrossDivisionCup`, points, `QualifiersPerGroup`), their Stages (type, order, `BracketName`, `BestOf`, `RoundRobinLegs`), and `DivisionPlayoffMapping`s.
- New frontend reverse-mapper (structure tree → `WizardState`), reconstructing `subGroupCount`/`CupConfig[]`/`bestOfByStage` from the flat Stage list — with dedicated new tests; flag ambiguity rather than guess silently.
- "Clonar torneo" action on the tournament **detail** page → navigates into the wizard pre-filled via the existing `location.state` pattern. Requires an explicit target `Category` choice on the action.
- Full clone; organizer edits/deletes zones inside the wizard afterward. Submits through the SAME existing `/full` transaction (`CreateFullTournamentAsync`).

### Out of Scope
- Rosters, team registrations, matches, standings, sanctions, audit logs, `DrawnAt` — none carry over; new divisions start empty.
- Dedicated `POST /clone` deep-copy write path (rejected: duplicates wizard validation, drifts).
- Date auto-shift, source-season restriction, selective checkbox picker (all confirmed out).

## Capabilities

### New Capabilities
- `tournament-cloning`: read a source tournament's structure and pre-fill the creation wizard for review/edit before submitting through the existing creation transaction.

### Modified Capabilities
- None (no existing spec-level behavior changes; wizard submit path is reused unchanged).

## Approach

Wizard-prefill (exploration Approach 1). Backend assembles a structure tree via a new explicit DTO — never widening load-bearing `TournamentResponse`/`DivisionResponse`. Frontend reverse-maps it into `WizardState` and seeds `TournamentWizardPage.tsx` through its proven `location.state` mechanism. Single tested write path; no drift. Use distinct naming to avoid collision with the unrelated `RosterCopy*` player-roster feature.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Application/DTOs/Tournament/Response/TournamentStructureResponse.cs` | New | Structure tree DTO |
| `Application/Services/TournamentService.cs` + `ITournamentService.cs` | Modified | New structure-read method |
| `API/Controllers/TournamentController.cs` | Modified | New read endpoint |
| `WebClient/.../wizard/{types,wizardLogic,submitWizard,TournamentWizardPage}.tsx` | Modified | Reverse-mapper + prefill |
| Tournament detail page | Modified | "Clonar torneo" action + category choice |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Reverse-mapper not mechanically reversible (subGroupCount/cup/bestOf) | High | Dedicated tests; flag ambiguity, never guess silently |
| DTO change breaks unrelated live pages | Low | Additive-only new DTO; do not touch existing responses |
| Category silently inherited | Med | Explicit organizer choice on clone action |
| Naming collision with `RosterCopy*` | Low | Distinct feature naming |

## Rollback Plan

Feature is additive. Revert the frontend detail-page action + reverse-mapper and the new backend endpoint/DTO; existing wizard and creation transaction are untouched, so no data or migration rollback is needed.

## Dependencies

- Existing `/api/tournaments/full` creation transaction and wizard `location.state` pre-seeding pattern.

## Success Criteria

- [ ] Organizer clones from a tournament detail page and lands in a fully pre-filled wizard.
- [ ] Reverse-mapper round-trips structure faithfully (covered by new tests).
- [ ] Cloned tournament created via the existing `/full` path with empty rosters and blank dates.
- [ ] No changes to `TournamentResponse`/`DivisionResponse` or unrelated pages.
