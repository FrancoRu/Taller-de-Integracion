# Club12 Backend — XML Documentation Standard

> **Scope:** This document covers **XML doc comments only** (`/// <summary>` and friends) across `Domain`, `Application`, `Infrastructure`, and `API`. It intentionally does not cover naming, style, nullable reference types, async conventions, or testing — those aren't in scope for this pass. `API.Tests` is excluded (test methods are self-documenting via their names; see the project's existing `MethodName_Scenario_ExpectedResult` convention).
>
> **Stack facts this document is grounded in (verified 2026-09-05):** .NET 8, xUnit 2.9.3. No `.editorconfig` exists. `Directory.Build.props` sets `<NoWarn>$(NoWarn);CS1591</NoWarn>` — missing-XML-doc warnings are suppressed, so nothing here is compiler-enforced; it's a review convention. `GenerateDocumentationFile=true` is set on all four non-test projects, but **Swagger (`AddCustomSwagger` in `API/Utils/StartupExtensions.cs`) only calls `IncludeXmlComments` for the API project's own generated XML file** — Domain/Application/Infrastructure XML comments never reach Swagger UI, they're IDE-only (IntelliSense). Rule XML-005 below addresses this gap directly.

---

## Why this exists

The codebase already has a real, working comment convention — establshed independently of this document, in files like `ErrorMessages.cs`, `StageService.cs`, and `PlayerSanctionService.cs`: comments explain *why* a rule exists or *why* a workaround is needed, never restate what the code already says via its own name. This document writes that convention down for XML comments specifically, so the backend-wide cleanup pass has a fixed target instead of "make it better."

---

## Rule XML-001 — Every public member gets a `<summary>`, but it earns its keep

Public types and members in `Domain`, `Application`, `Infrastructure`, `API` get a `<summary>`. A `<summary>` that only restates the identifier name is worse than no comment — it's noise a reader has to read past.

```csharp
// ❌ VIOLATION — restates the name, adds nothing
/// <summary>Gets or sets the tournament status.</summary>
public TournamentStatus Status { get; set; }

// ✅ CORRECT — no comment needed at all; the name is the whole story
public TournamentStatus Status { get; set; }
```

```csharp
// ✅ CORRECT — the name can't carry this; the comment earns its line
// (Application/Services/PlayerSanctionService.cs, actual codebase example)
/// <summary>
/// Fechas (match-days) still remaining on the sanction's suspension. Short-circuits
/// to 0 when the appeal was accepted — the sanction is lifted immediately rather
/// than continuing to count down, without mutating the original <see cref="PlayerSanction.Duration"/>
/// so the sanction's history stays intact.
/// </summary>
```

**Applies to:** public classes, interfaces, methods, and non-trivial properties (ones with business rules, side effects, or non-obvious units/ranges). Skip auto-properties whose name and type already say everything (`public Guid Id { get; set; }`, `public string Name { get; set; }`).

---

## Rule XML-002 — Document the WHY, never the WHAT

Matches this repo's existing inline-comment convention. A `<summary>` should carry the non-obvious part: a business rule, an invariant, a workaround for a specific bug, a reason a check exists. If removing the comment wouldn't confuse a future reader, don't write it.

```csharp
// ❌ VIOLATION — narrates the code instead of explaining the decision
/// <summary>
/// Loops through pending matches and sets their status to Canceled, then
/// calls UpdateRangeAsync to persist the changes.
/// </summary>
private async Task CancelPendingMatchesAsync(Guid tournamentId)

// ✅ CORRECT — states the rule and the invariant that isn't visible from the code alone
/// <summary>
/// Cascades a tournament's Cancel/Finish transition onto its still-pending matches,
/// marking them <see cref="MatchStatus.Canceled"/> so the tournament and its matches
/// never disagree about whether play is still ongoing. Never touches <c>IsFinished</c> —
/// a canceled match must never read as played.
/// </summary>
```

---

## Rule XML-003 — API-project comments are consumer-facing; other layers are IDE-only

Because of the Swagger wiring described above, `<summary>` text on **Controllers, request DTOs, and response DTOs in the `API` project** is what external API consumers actually see in Swagger UI — it functions as public documentation, not a code comment. Write it for that reader: describe the endpoint's behavior and each field's meaning/constraints, not implementation detail.

`<summary>` text in `Domain`, `Application`, and `Infrastructure` is IntelliSense-only (see XML-005) — it should still follow XML-001/002, but there's no external-consumer bar to clear.

```csharp
// ✅ CORRECT — API project, Swagger-visible, written for a consumer of the endpoint
// (API/Controllers/PlayerController.cs style)
/// <summary>
/// Creates a player as an administrator. Returns the full admin-facing player
/// record (including PII fields not exposed on the public endpoint).
/// </summary>
```

---

## Rule XML-004 — No stale comments

A `<summary>` describing behavior the code no longer has is actively harmful — worse than a missing one, because it's read as true. When editing a method, re-read its XML comment and correct or delete anything the change invalidated. This is the most common real defect to look for in the audit pass: comments left behind after a signature or behavior change.

---

## Rule XML-005 — Fix the Swagger `IncludeXmlComments` gap

`API/Utils/StartupExtensions.cs`'s `AddCustomSwagger` only loads `{ExecutingAssemblyName}.xml` (i.e. `API.xml`). `Domain.xml`, `Application.xml`, and `Infrastructure.xml` are generated (`GenerateDocumentationFile=true` is set on all four projects) but never passed to `IncludeXmlComments`, so DTO/enum XML comments defined outside the API project (e.g. on `Application.DTOs.*` request/response types) don't show up in Swagger's schema view even though they exist and are well-written.

Fix: call `IncludeXmlComments` for each of the four generated `.xml` files (guard each with `File.Exists` — publish/CI output layout can vary), not just the executing assembly's own file.

---

## What this document does not cover (yet)

Naming conventions, code style, nullable reference types, async/await conventions, error handling/logging, file/project organization, and testing standards are out of scope for this pass. If a future pass wants to define those, it should be grounded in this project's actual tooling the same way this document is — not adapted from an unrelated codebase's rulebook.
