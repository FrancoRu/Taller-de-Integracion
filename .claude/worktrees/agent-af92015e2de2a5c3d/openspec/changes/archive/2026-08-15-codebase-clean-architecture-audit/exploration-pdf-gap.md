# Exploration: PDF Requirements vs. Implementation Gap Analysis

## Current State

Both academic requirement documents (`Informe 1 Taller Integral.pdf`, `Informe 2 Taller integracion.pdf`) were read in full. Informe 1 defines 7 functional modules (Jugadores, Equipos, Sanciones, Estadísticas, Usuarios, Visitantes, Backups) plus NFRs (RBAC/password recovery, DB backups, performance/scalability, responsive design, documentation, transitional coexistence). Informe 2 defines the tech stack (ASP.NET 8 + EF + PostgreSQL, React + TS), a 7-sprint Scrum plan (Nov 2023–Jun 2024), explicit Sprint 2 security requirements, an explicit Sprint 7 unit+integration testing/vulnerability-report deliverable, and a DER with 11 entities.

Cross-checked against the actual repo (paths verified via Glob/Grep, not assumed):
- Backend: `Club12-Backend/API/Controllers/{Auth,BlogPost,Division,Match,Player,PlayerSanction,PlayerStatistic,Scorer,Stage,Team,Tournament,User,Venue}Controller.cs` (13 controllers — note `PlayerSanctionController`, `ScorerController`, `StageController` exist, beyond the originally assumed list).
- Frontend: `Club12-WebClient/src/modules/{15 domains}` and `src/views/{15 domains + home/public}`.
- DB provider confirmed as PostgreSQL (`UseNpgsql` at `Club12-Backend/API/Utils/StartupExtensions.cs:59,275`), matching Informe 2's final DB decision.

## Gap Matrix — Informe 1 Functions

| Function | Status | Key evidence |
|---|---|---|
| Gestión de Jugadores | **Fully implemented** | `PlayerController.cs` filter/search endpoints; unique-DNI constraint migration `20250731014937_...` |
| Gestión de Equipos | **Implemented** | `TeamController.cs`, `views/team/*`; W/L stats surfaced via Scorer/PlayerStatistic rather than a dedicated field |
| Registro de Sanciones | **Fully implemented, exceeds spec** | `PlayerSanctionController.cs` has a full appeal/resolve workflow (`SanctionAppealStatus`) beyond the PDF's one-liner |
| Registro de Estadísticas | **Implemented** | `PlayerStatisticController.cs`, `ScorerController.cs`, `Position` entity for live standings |
| Gestión de Usuarios | **Fully implemented, exceeds spec** | `UserController.cs` full CRUD + activate/deactivate + admin-forced password reset + RBAC |
| Visualización visitantes | **Fully implemented** | `[AllowAnonymous]` endpoints + dedicated `views/home/{matches,teams,tournaments,scorers,sanctions}/Public*.tsx` |
| **Creación de copias de seguridad** | **MISSING ENTIRELY** | Grep for `backup\|Backup\|BackgroundService\|IHostedService\|Cron` across the whole backend returned zero matches |

## Cross-Cutting Findings

- **RBAC + password recovery**: implemented (`Roles` enum, `[Authorize(Roles=...)]`, `password-reset/confirm`).
- **Regular DB backups (NFR)**: missing — same gap as the function above, doubly documented in the source PDF, doubly absent in code.
- **Rendimiento/Escalabilidad**: unverifiable, not just unmet — zero test coverage means this NFR can't currently be validated either way.
- **Diseño Adaptativo (responsive)**: genuinely implemented — 115 breakpoint occurrences across 33 view files.
- **Documentación y Soporte**: partial — Swagger/XML docs + 2 README.md files exist; no end-user manual found.
- **Sprint 7 testing deliverable** (unit+integration tests + formal vulnerability report): **never fulfilled** — reconfirmed zero test projects (no `*.Tests.csproj`, no vitest/jest in `package.json`). This upgrades the existing clean-code audit's "no test runner" observation from a best-practice gap to a **documented, contracted deliverable that was never delivered**.
- **"MVC pattern"**: PDF uses this loosely; actual Clean/Layered Architecture + React SPA/REST is a stronger, correct fit — not a real gap, flagged so it isn't mis-scored as one.

## Scope Creep (implemented, undocumented, but legitimate)

BlogPost (traces to the Informe 1 interview's mention of match recaps published on the website), Venue (canchas are operationally necessary but absent from the DER — a gap in the source document, not the code), Scorer/Stage/Position (refinements of "Estadísticas" never broken into DER entities), magic-link/guest login/refresh tokens (modern auth conveniences beyond spec), the sanction-appeal workflow (a legitimate superset).

## Recommendation

Extend (not replace) the existing proposal:
1. Re-justify test scaffolding as closing an overdue academic deliverable, not just hygiene — keep it first.
2. Add one new, independent follow-on slice: **scheduled backup capability** — genuine new functional behavior, needs its own tests, sequenced after scaffolding, sized well under 800 lines.
3. Add a low-priority docs-only slice for the end-user manual.
4. Note explicitly that "MVC pattern" language should not force a literal re-architecture.
5. No other functional expansion needed — the existing 45+ file clean-code remediation program stays as sliced.

## Risks

- Backup slice needs a storage/retention decision (not specified in either PDF) before `sdd-tasks` can size it.
- Performance/scalability NFR remains unverifiable until both test scaffolding and some perf-testing decision exist.
- The "MVC" reinterpretation is a judgment call, not an explicit PDF concession — worth a one-line disclaimer in the revised proposal.

## Ready for Proposal

Yes — proceed to `sdd-propose` to revise the existing proposal with the backup-capability slice and the strengthened test-scaffolding justification, keeping the rest of the phased program intact.
