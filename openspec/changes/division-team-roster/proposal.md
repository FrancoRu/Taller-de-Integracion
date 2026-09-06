# Proposal: Division Team Roster & Playoffs-Only Seeding

**Touches: both** (backend `Club12-Backend`, frontend `Club12-WebClient`).

## Intent

Today a team's membership in a division is not a fact the system stores directly — it exists only
as a side effect of `StageTeamMatch` rows (`Team → StageTeamMatch → Stage → Division`). `Team.cs`
has no `DivisionId` and no `Division` navigation at all. This single missing fact is the root cause
behind three separate defects the exploration confirmed mechanically:

1. A **playoffs-only division** (wizard `hasGroupStage` unchecked) has no Group stage, so
   `TournamentDivisionAssignment.tsx` finds zero assignable groups and renders no widget to enrol a
   team — and even if a team were force-assigned, `SeedKnockoutStageAsync` would throw
   `SeedMissingStandings` because there is no group phase to seed from.
2. **Changing sub-group count (HU-123)** is impossible without destroying the only record that a
   team belongs to the division, because "in this division" and "in this specific sub-group stage"
   are the same `StageTeamMatch` row.
3. There is **no durable roster** to run balanced sub-group distribution (HU-121/122) over.

This change introduces `DivisionTeamRegistration` as the authoritative division-level roster —
independent of stage structure — mirroring the existing `TeamTournamentRegistration` pattern. On
that foundation it delivers: HU-121/122/123 sub-group refinement (configurable count, balanced
distribution, manual reassignment, re-balancing before start without dropping teams), and a new
seeding path for groupless divisions (random draw with preview-before-commit, or manual seeding),
with bye handling, audit logging, and a public "sorteo realizado" transparency element.

## Scope

### In Scope

#### 1. `DivisionTeamRegistration` entity (the roster) — the foundation

A new entity mirroring `TeamTournamentRegistration` exactly:

- Fields: `TeamId` (FK), `DivisionId` (FK), inherited `EntityBase` audit fields
  (`Id`, `DateCreated`, `DateUpdated`, `CreatedBy`, `UpdatedBy`).
- Configuration mirroring `TeamTournamentRegistrationEntityConfiguration`: table name constant in
  `EntityConstants.Tables`, **unique index on `(TeamId, DivisionId)`**, both FKs
  `OnDelete(DeleteBehavior.Cascade)`, `BaseEntityConfiguration` wiring (`CreatedAt` index, etc.),
  plus a single-column index on the `DivisionId` "many" side.
- **No status/lifecycle field** (resolves exploration open question 3). Boolean presence in the
  table means "enrolled in this division." `PlayerTeamRegistration`'s status-enum pattern is
  deliberately not copied — nothing in HU-121/122/123 needs to distinguish "enrolled but not yet
  slotted" from "enrolled and playing" at the roster level; that distinction is already expressed by
  whether a `StageTeamMatch` row exists. A status enum can be added later (`HasConversion<string>()`
  precedent exists) without reshaping the table.

This entity becomes the **source of truth for "which teams are enrolled in this division,"
independent of `Stage`/`StageTeamMatch`.**

#### 2. Relationship between `DivisionTeamRegistration` and `StageTeamMatch` — additive, not replacing

**Decision (resolves open question 1): coexist additively — shape (a).** `DivisionTeamRegistration`
is the new authoritative "enrolled in this division" fact; `StageTeamMatch` keeps its schema
unchanged and narrows *conceptually* to "this team is placed into this specific stage (sub-group
slot or bracket slot)." Placement becomes a **subset relationship**: a team MAY only be placed into
a stage of a division it is registered in. The full-absorb refactor (shape b, `StageTeamMatch`
losing all division meaning) is explicitly **rejected** for this change — it is a larger, higher-risk
rewrite of the assign/unassign/seed surface for no benefit the additive shape doesn't already give.

Rationale: the additive shape is exactly what makes HU-123 clean — a group-count change keeps the
roster untouched and rebuilds only the disposable `Stage` + `StageTeamMatch` layer. It is lower risk
(no rewrite of `SeedKnockoutStageAsync`, `CrossCupGroupSeeder`, standings, etc.), and it preserves
every existing invariant while adding the missing one.

**Enrolment/placement flow going forward:**
- Enrol team → division: create `DivisionTeamRegistration` (new authoritative step).
- Place team into a sub-group / bracket slot: `StageTeamMatch` as today, but assignment now
  **requires** an existing `DivisionTeamRegistration` for that `(TeamId, DivisionId)`.
- The cross-division conflict rule (`EnsureNoCrossDivisionConflictAsync`, open question 4) moves to
  validate at **registration** time against `DivisionTeamRegistration` as the authoritative "which
  division(s) is this team in" fact: a team MAY hold at most one registration in a regular
  (non-cross-cup) division of a tournament, plus optionally one in a `IsCrossDivisionCup` division.
  Stage assignment then only checks roster membership, not cross-division conflicts (the conflict is
  already caught at the roster layer). This preserves the existing "one regular zone + optionally one
  cross-cup" invariant exactly, just enforced one layer up.

#### 3. Backfill / migration (resolves open question 2 and mandatory decision 4)

The EF migration that creates the table MUST also **backfill** existing data so no tournament
silently loses roster history, following the `RebackfillDivisionStageSlugs`-style data-migration
precedent (raw SQL inside the migration):

```sql
INSERT INTO "DivisionTeamRegistrations" (Id, TeamId, DivisionId, DateCreated, ...)
SELECT <new-guid>, stm."TeamId", s."DivisionId", <now>, ...
FROM "StageTeamMatches" stm
JOIN "Stages" s ON stm."StageId" = s."Id"
GROUP BY stm."TeamId", s."DivisionId";
```

**Exact backfill rule (mandatory decision 4 — cross-division-cup correctness):** produce exactly
**one `DivisionTeamRegistration` per distinct `(TeamId, DivisionId)` pair**, where `DivisionId` is
resolved through `Stage.DivisionId`. Deduplication is on the **pair**, never on `TeamId` alone.
Consequences that the migration MUST honour:
- A team sitting in multiple sub-group stages *of the same division*, or in a group stage AND a
  same-division bracket stage, collapses to **one** registration for that division (dedup applies).
- A team legitimately in its home regular zone (division A) **and** a cross-division cup (division B)
  produces **two** registrations, one per division — these MUST NOT be collapsed. The distinct-pair
  grouping guarantees this automatically.
- The unique index on `(TeamId, DivisionId)` is consistent with this rule (the pair is unique; the
  team is not).

#### 4. HU-121/122/123 — configurable sub-groups with balanced distribution

- **HU-121 (count + balance):** organizer picks a sub-group *count* `G`. Teams distribute so each
  sub-group has `floor(T/G)` or `ceil(T/G)` teams (never a gap ≥ 2). Minimum 4 teams per sub-group;
  reject `T/G < 4`. Validated as a non-blocking warning in the wizard (no real enrolments yet) and
  blocking at completability-guard time (`TournamentCompletabilityValidator`, HU-109 extension).
- **HU-122 (assignment):** one-click "auto-distribute" (random-balanced) layered on top of the
  existing `TeamPickerDialog` / `TournamentDivisionAssignment.tsx` manual per-group flow; manual
  reassignment always available. Distribution runs over the **`DivisionTeamRegistration` roster**,
  not over stage rows.
- **HU-123 (re-balance / change count before start):** changing the sub-group count with teams
  already placed MUST keep the roster untouched, delete and rebuild only the `Stage` +
  `StageTeamMatch` layer, then re-run HU-121/122 balanced distribution over the unchanged roster. No
  team is ever orphaned. Bounded by the existing tournament-status lock
  (`EnsureDivisionStructureEditableAsync`: blocked once `Ongoing`/`Finished`/`Canceled`), which is
  the correct granularity for this action.
- The naming convention from the audit stands: the new level is a **"sub-group" / "pool,"** never
  reusing "zona" (which already means division in this code).

#### 5. Playoffs-only division seeding (random draw / manual)

For a division with no group phase, a **new seeding path** (the existing `SeedKnockoutStageAsync`
cannot serve it — no standings to seed from):

- **Ordered-list source:** the new mechanism only has to *produce* an ordered `List<Guid>` and hand
  it to the existing `PlayoffSeeder.SeedPairs` + `FillStageWithSeedsAsync` machinery — **reuse it
  unchanged.** `SeedPairs` already pads to the next power of two with `null` (bye), builds the
  classic 1-vs-N seed order, and `FillStageWithSeedsAsync` + `TryAdvanceStageWinnerAsync` already
  walk a bye's implicit winner into the next round. **Do not reinvent bye handling** — it
  generalizes as-is.
  - **Random draw ("sorteo aleatorio"):** a `Random`-shuffled order of the division's roster.
  - **Manual seeding:** admin specifies the order / slot per team.
- **Preview-before-commit (resolves open question 5): server-side.** A stateless
  `POST .../preview-draw` returns the pairing **plus a draw token** (the RNG seed / the exact ordered
  list) without persisting anything; commit submits that same token/order so **the previewed bracket
  is guaranteed to equal the committed bracket.** This matters because organizers run a public
  "sorteo" and must be able to show the result before it is final; a client-side shuffle cannot
  guarantee preview == commit unless the exact seed round-trips, so the server-side path is the safer
  choice and is chosen deliberately.
- **Re-draw / re-seed lock (resolves open question 3 / mandatory decision 3):** a **new
  bracket-scoped guard**, distinct from `EnsureDivisionStructureEditableAsync` (which is
  tournament-status-scoped and the wrong granularity). The guard condition, in prose:

  > A (re-)draw of a bracket is permitted **only while that specific bracket has zero played
  > matches.** A match counts as "played" if it `IsFinished`, or has a recorded score, or has a
  > recorded actual start/played date. The guard scopes to the target `Stage` **and its
  > `BracketName`** (so parallel brackets under one division — "Copa de Oro" / "Copa de Plata" — lock
  > independently). It is evaluated independently of tournament status, because a legitimate playoff
  > draw happens *after* the tournament is `Ongoing` (groups already played). The first played match
  > in the bracket permanently freezes that bracket's seeding; any earlier state allows re-draw.

  `sdd-design` turns this prose into the concrete query and exception (thrown as
  `InvalidOperationException` → mapped to 409, consistent with existing guard style).
- **Audit logging (mandatory decision on enum naming):** every draw (initial or re-draw) logs
  through the existing `IAuditService.LogAsync` with a **new `AuditAction.PlayoffDraw`** member
  (chosen over `BracketSeeding`/`DivisionSeeding` for plain readability). Three-file change: backend
  `AuditAction` enum, frontend `AuditAction` string-union type alias
  (`modules/auditLog/type/auditLog.d.ts`), and `ACTION_LABELS` Spanish map in `AuditLogsPage.tsx`.
  Entry sets `TargetType = "Stage"`, `TargetId` = the bracket stage, `Detail` = a human line
  (`"Sorteo aleatorio — 8 equipos"` or the manual order). Logging is fire-and-forget (mirroring
  `AuditService.LogAsync`'s existing resilience) — a logging failure MUST NOT block the draw.
- **"Sorteo realizado el [fecha]" on the public bracket view (mandatory decision on where it lives):**
  a **new nullable `Stage.DrawnAt` column** (option a), set at commit time, surfaced through
  `IStageResponse` and rendered on the public bracket view. Chosen over reading the audit trail
  because the audit trail is `[Authorize(Roles = AdminOrOwner)]` and the bracket view is
  **public** — a public label cannot read `GET /api/audit-logs`, and `Stage.DrawnAt` is consistent
  with how the rest of the public bracket already gets its data (`IStageResponse`/`IMatchResponse`
  directly).

#### 6. HU-124 dead endpoint — remove it (mandatory decision 2)

**Decision: delete** `StageService.CreateAutomatedStagesAsync`, its route
`POST /api/stages/generate/{id}` (`StageController.GenerateStagesAndMatches`), and the orphaned
frontend `generateStages` (`stage.service.ts` / `stage.context.tsx`). It is dead (no UI caller),
rigid (fixed 4-team groups, requires exactly 8/16/32/64 teams), and incompatible with HU-121's
organizer-chosen count. Leaving it alive under a name that "sounds like" what HU-121 builds is a
foot-gun: a future developer could wire the wrong mechanism. Removing it makes HU-121's
balanced-distribution logic the single source of truth for "build group stages." This is a plain
deletion, not a retrofit — retrofitting a dead, differently-shaped endpoint costs more than it
returns. (Impact analysis before deletion is required per project rules; the exploration already
confirmed zero UI callers, but the deletion task MUST re-verify no other backend caller exists.)

### Out of Scope

- **Promotion/relegation across seasons** — explicitly out. Nothing here touches cross-season
  promotion; `TeamTournamentRegistration`/`PlayerTeamRegistration` are already season-scoped via
  `TournamentId` and need no change.
- **HU-125 (position→cup classification per sub-group)** — adjacent and *surfaced* by shipping
  sub-groups (once a division has N sub-groups, the HU-112 single-standings-table assumption for cup
  qualification breaks), but it is a **separate change**. This proposal deliberately does not solve
  cup-qualification-across-sub-groups; it is flagged in Risks so it is not silently lost. The roster
  model does not preclude it (HU-110 cross-cup pooling is the existing precedent to reuse).
- **The four forward-compat formats below** — the schema must not *preclude* them, but none are
  designed or built here.
- Any status/lifecycle enum on `DivisionTeamRegistration` (deferred, see §1).

### Forward-compatibility check (must not preclude; not designed here)

- **Split-league (round-robin → championship/relegation pools):** the roster-as-durable-fact /
  stage-as-disposable-layer split already generalizes to "rebuild the structural layer twice, once
  per phase." One caveat to carry to design: `CreateStageAsync` currently enforces "at most one Group
  stage per non-cross-cup division" as a hard invariant — split-league would need that invariant
  loosened or exempted. Flagged, not worked around.
- **Consolation bracket:** `Stage.BracketName` already lets parallel elimination paths coexist; a
  consolation bracket is the same shape with a different seed *source* (first-round losers). The
  bracket primitives don't care where the ordered list came from — no schema change needed, just a
  future "who feeds the seed list" adapter, exactly like the random/manual adapter this change builds.
- **Repechaje / playoff-in:** an extra `Stage` whose winners feed the next stage's seed list;
  `TryAdvanceStageWinnerAsync` already pushes a decided slot's winner forward. Needs future design
  for the "winner joins pre-seeded direct qualifiers" case, but the entity model does not block it.
- **Swiss-system (tentative, lowest priority):** the one direction that genuinely does not fit the
  fixed pre-generated `Match` shape. Nothing to design now; `DivisionTeamRegistration` (team ↔
  division, no pairing structure) is naturally Swiss-agnostic, so the roster itself doesn't need
  redesign to add it later.

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `Club12-Backend/Domain/Entities/Models` | New | `DivisionTeamRegistration` entity |
| `Club12-Backend/Domain/Enums/AuditAction.cs` | Modified | New `PlayoffDraw` member |
| `Club12-Backend/Domain/Entities/Models/Stage.cs` | Modified | New nullable `DrawnAt` |
| `Club12-Backend/Infrastructure/Persistance/Configurations` | New | `DivisionTeamRegistrationEntityConfiguration` (unique `(TeamId, DivisionId)`, cascade FKs) |
| `Club12-Backend/Infrastructure/Persistance/EntityConstants.cs`, `ApplicationDBContext.cs` | Modified | Table-name constant, `DbSet` |
| `Club12-Backend/Infrastructure/Migrations` | New | Create-table migration + **data backfill** + `Stage.DrawnAt` column |
| `Club12-Backend/Application/Services/StageService.cs` | Modified | Roster-aware assign/unassign; new random/manual draw + preview; new bracket-scoped re-draw guard; sub-group rebuild for HU-123; **delete `CreateAutomatedStagesAsync`** |
| `Club12-Backend/Application/Utils/Helper/Playoff/PlayoffSeeder.cs` | Reused | `SeedPairs` bye logic reused unchanged |
| `Club12-Backend/Application/Services/AuditService.cs` + `IAuditService` | Reused | `LogAsync` call for `PlayoffDraw` |
| `Club12-Backend/API/Controllers/StageController.cs` | Modified | New roster/draw/preview endpoints; **delete `GenerateStagesAndMatches`** |
| `Club12-Backend/API` DTOs / `IStageResponse` | Modified | Surface `DrawnAt`; draw request/response DTOs |
| `Club12-WebClient/src/views/tournament/wizard/*` (`ZoneEditor`, `DivisionesStep`, `types.ts`, `submitWizard.ts`) | Modified | Sub-group count input; wizard-side balance warning |
| `Club12-WebClient/src/views/tournament/TournamentDivisionAssignment.tsx` | Modified | Fix dead-fallback bug; auto-distribute + manual sub-group assignment; re-balance action; **first-time test coverage** |
| Division detail / bracket page + `modules/playoff/buildBracket.ts` | Modified | Draw action, manual seeding, server-side preview |
| Public bracket view | Modified | "Sorteo realizado el [fecha]" from `DrawnAt` |
| `Club12-WebClient/src/modules/stage/service/stage.service.ts`, `type/stage.ts` | Modified | New endpoints/types; **remove `generateStages`** |
| `Club12-WebClient/src/modules/auditLog/type/auditLog.d.ts`, `views/panel/AuditLogsPage.tsx` | Modified | `PlayoffDraw` type + Spanish label |
| `Club12-Backend/API.Tests`, `Club12-WebClient` tests | New | Roster/backfill/draw/guard/rebuild + UI tests (strict TDD) |
| `Docs/historias-de-usuario.md` | Modified | Refined HU-121/122/123 text (below) |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Backfill collapses a cross-division-cup team's two divisions into one (roster loss) | Med | Distinct-pair grouping rule (§3) verified by a dedicated migration test using a team in a regular zone + a cross-cup division |
| Additive `DivisionTeamRegistration` drifts from `StageTeamMatch` (a placed team with no registration, or vice versa) | Med | Assignment requires an existing registration; backfill seeds every historical placement; consistency test asserts every `StageTeamMatch` has a matching registration |
| HU-123 rebuild orphans teams if roster/stage delete order is wrong | Med | Roster is never touched during rebuild; only stage layer is deleted+recreated; test asserts roster count invariant across a count change |
| New bracket-scoped re-draw guard mis-scoped (locks/unlocks the wrong bracket) | Med | Guard keys on `Stage` + `BracketName`; tests cover parallel brackets locking independently, and first-played-match freezing |
| Client-shown preview ≠ committed bracket for a public "sorteo" | Low | Server-side preview returns a draw token that is replayed on commit; preview==commit is a tested guarantee |
| Deleting HU-124 endpoint breaks a hidden caller | Low | Mandatory impact analysis before deletion; exploration already found zero UI callers |
| HU-125 (cup qualification per sub-group) breaks once sub-groups ship but is out of scope | High | Explicitly flagged out of scope; sub-group-enabled divisions with configured playoff cups are the boundary `sdd-spec` must fence off so the two changes don't collide |
| `TournamentDivisionAssignment.tsx` has zero existing tests | High | Budget first-time component tests; design the fix to make the component testable |

## Rollback Plan

The change ships one EF migration (new `DivisionTeamRegistrations` table + `Stage.DrawnAt` column +
data backfill). Rollback specifics:

- **Schema is cleanly revertible.** The migration's `Down()` drops the `DivisionTeamRegistrations`
  table and the `Stage.DrawnAt` column. `DivisionTeamRegistration` holds only membership metadata
  derived from (and still fully implied by) `StageTeamMatch` — dropping it loses **no** fact that
  isn't reconstructable from the still-present `StageTeamMatch`/`Stage` rows. `DrawnAt` is a display
  timestamp only; dropping it loses the "sorteo realizado el" label, nothing structural.
- **Roster rows created between deploy and rollback are not orphaned data loss.** Any team enrolled
  via the new roster path also has (or gets) its `StageTeamMatch` placement under the old model, so a
  rollback to the pre-change code keeps operating on `StageTeamMatch` exactly as before — the dropped
  registration rows were a projection, not the only record. The one genuine loss on rollback is
  **playoffs-only divisions enrolled purely via the roster with no group phase**: those teams exist
  only as `DivisionTeamRegistration` + bracket `StageTeamMatch`; the bracket placements survive
  (they're `StageTeamMatch`), so the seeded bracket is intact, but the roster-level "enrolled, not
  yet placed" state for any team not yet drawn into a slot would be lost. This is the expected and
  acceptable rollback cost and is called out so it is not a surprise.
- **Code rollback is a plain revert.** Removing the new endpoints/guard/draw path returns the system
  to today's behavior (including the original playoffs-only bug). Reverting the HU-124 deletion
  restores the dead endpoint — harmless, since it had no caller.
- Because the migration includes a data backfill, a forward re-deploy after rollback re-runs the
  backfill idempotently (distinct-pair `INSERT` is safe to re-run against an empty table; guard with
  `NOT EXISTS` if re-applied over partial data).

## Dependencies

- No new packages. Reuses `PlayoffSeeder`, `IAuditService`, `TournamentCompletabilityValidator`,
  `TeamPickerDialog`, and the existing migration/backfill tooling.
- `sdd-design` must turn the prose re-draw guard (§5) into a concrete query, and settle the exact DTO
  shapes for the draw/preview endpoints and `IStageResponse.DrawnAt`.

## Success Criteria

- [x] `DivisionTeamRegistration` exists with a unique `(TeamId, DivisionId)` index and cascade FKs; a
      migration creates it and backfills every historical `(TeamId, DivisionId)` pair.
- [x] A cross-division-cup team ends up with two registrations (one per division) after backfill; a
      team in two sub-groups of one division ends up with one.
- [x] A playoffs-only division can enrol teams (widget renders), draw its bracket (random preview →
      commit, or manual), with byes handled by the reused `SeedPairs`.
- [x] Preview and committed bracket are identical for a random draw.
- [x] A bracket can be re-drawn until its first match is played, and not after; parallel brackets lock
      independently.
- [x] Every draw writes a `PlayoffDraw` audit entry; a logging failure never blocks the draw.
- [x] The public bracket view shows "Sorteo realizado el [fecha]" from `Stage.DrawnAt`.
- [x] Sub-group count is organizer-chosen and balanced (`floor`/`ceil`, gap < 2, min 4); changing the
      count before start re-balances over the unchanged roster with no orphaned teams.
- [x] `CreateAutomatedStagesAsync` / `POST /api/stages/generate/{id}` / frontend `generateStages` are
      gone; no caller remains.
- [x] Backend and frontend suites pass; new logic is TDD-covered, including first-time
      `TournamentDivisionAssignment.tsx` tests.

## Refined HU-121/122/123 text (draft for `Docs/historias-de-usuario.md`)

`sdd-spec` will formalize these into Given/When/Then; this is the refined replacement prose, updated
to reflect the `DivisionTeamRegistration` roster decision.

### HU-121 · La cantidad de sub-grupos la define el organizador; el reparto es balanceado — `M`
**Como** owner/admin **quiero** elegir CUÁNTOS sub-grupos tendrá una división (no un tamaño fijo por
grupo) y que el sistema reparta los equipos inscriptos lo más parejo posible **para** no terminar con
grupos desbalanceados ni tener que armarlos a mano.
- El organizador manda la **cantidad de sub-grupos** `G`; con `T` equipos inscriptos en la división,
  cada sub-grupo recibe `floor(T/G)` o `ceil(T/G)` equipos — nunca una diferencia ≥ 2 entre el más
  chico y el más grande.
- Mínimo **4 equipos por sub-grupo**; si `T/G < 4` el sistema rechaza esa cantidad con un mensaje
  claro.
- El reparto opera sobre el **roster de la división** (`DivisionTeamRegistration`), que es la fuente
  de verdad de "qué equipos están inscriptos en esta división", independiente de la estructura de
  stages. Elegir/cambiar la cantidad de grupos reconstruye la capa de stages, nunca el roster.
- Se valida dos veces: advertencia no bloqueante en el wizard (aún sin inscriptos reales) y validación
  bloqueante en la guarda de completitud (`TournamentCompletabilityValidator`) al cerrar
  inscripción/iniciar el torneo.
- La cantidad elegida al armar la estructura es un punto de partida, no definitiva (ver HU-122/123).

### HU-122 · Asignar equipos a sub-grupos: automático por defecto, manual siempre disponible — `S`
**Como** owner/admin **quiero** que el sistema reparta automáticamente los equipos del roster entre
los sub-grupos, pudiendo moverlos a mano **para** ajustar por criterios que el sistema no conoce
(cercanía, nivel, rivalidades).
- Extiende `TournamentDivisionAssignment.tsx` y `TeamPickerDialog`: se agrega una acción "repartir
  automático" (aleatorio balanceado de HU-121) de un clic, dejando el ajuste manual por sub-grupo como
  paso posterior, no como único camino.
- El auto-reparto es aleatorio balanceado por defecto (no hay ranking/seed histórico confiable en una
  liga amateur) — decisión de producto, no dificultad técnica.
- Enrolar un equipo en la división (crear su `DivisionTeamRegistration`) y ubicarlo en un sub-grupo
  (`StageTeamMatch`) son dos pasos distintos: el equipo puede estar inscripto en la división sin estar
  todavía ubicado en un sub-grupo.

### HU-123 · Editar la cantidad de sub-grupos antes de que arranque el torneo — `S`
**Como** owner/admin **quiero** cambiar la cantidad de sub-grupos de una división después de creada la
estructura pero antes de iniciar el torneo **para** ajustar el armado a la cantidad real de equipos
inscriptos.
- Disponible mientras el torneo no esté `Ongoing`/`Finished`/`Canceled` (misma guarda
  `EnsureDivisionStructureEditableAsync`).
- Cambiar la cantidad **mantiene el roster intacto** y reconstruye solo la capa de stages
  (`Stage` + `StageTeamMatch`), volviendo a correr el reparto balanceado de HU-121/122 sobre el roster
  sin cambios. Ningún equipo queda huérfano: antes, cambiar la cantidad de grupos destruía el único
  registro de que el equipo pertenecía a la división; con el roster como hecho durable, eso ya no pasa.
```
