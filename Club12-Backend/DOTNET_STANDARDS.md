# Club12 Backend — XML Documentation Standard

> **Scope:** This document covers **XML doc comments** (`/// <summary>` and friends) and **one rule of file organization** (one type per file — see FILE-001) across `Domain`, `Application`, `Infrastructure`, and `API`. It intentionally does not cover naming, code style, nullable reference types, async conventions, or testing — those aren't in scope for this pass. `API.Tests` is excluded (test methods are self-documenting via their names; see the project's existing `MethodName_Scenario_ExpectedResult` convention).
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
/// than continuing to count down, without mutating the original Duration so the
/// sanction's history stays intact.
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
/// marking them Canceled so the tournament and its matches never disagree about
/// whether play is still ongoing. Never marks a canceled match as finished — a
/// canceled match must never read as played.
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

## Rule XML-006 — Plain prose in `<summary>`, no inline markup, no parentheses, exactly 3 lines

**Added 2026-09-05 after the first cleanup pass shipped comments full of `<see cref>` and `<c>` — that's exactly the clutter this rule now forbids. Tightened five times the same day, in order: after later passes still left parenthetical asides and summaries wrapped across too many `///` lines; after a follow-up correction clarified the target shape is 3 lines, not 1; after a correction dropped `<remarks>` as an overflow valve entirely — there is no escape hatch, everything must fit in the one summary line; after a correction banned user-story/ticket references (HU-XX) from doc comments outright; and after a final correction banned "for example" / "such as" example-introducing phrases too — no example call-outs of any kind, worded any way.**

`<summary>` text is plain English prose, held to five things at once:

1. **No inline markup.** Do not use `<see cref="...">`, `<c>...</c>`, `<paramref name="...">`, or `<typeparamref name="...">` inside it — write the member/parameter name as plain text instead. These tags read as noise in the source (which is where developers actually read comments — IntelliSense tooltips are secondary) and `<see cref>` targets silently go stale on a rename with no compiler warning, which is worse than plain text that can't "break."
2. **No parentheses at all**, not just "e.g." asides. A parenthetical is a second thought bolted onto the sentence — fold it into the main clause or cut it.
3. **No example call-outs at all** — not "e.g.", not "for example", not "such as", not "like X". State the general rule; if a concrete example is genuinely necessary to make the rule clear, it must read as a plain fact in the sentence, not flagged as an example.
4. **No user-story/ticket references** (`HU-99`, `HU-63/HU-65`, and the like). A ticket ID is external tracking metadata, not something a reader of the code needs — describe the actual rule or behavior instead of pointing at a ticket number.
5. **Exactly 3 lines: the opening tag alone, one line of concise text, the closing tag alone.** Never collapse it to one line (`<summary>text</summary>`), and never let the text wrap onto a second or third `///` line — one fact, one line, tags on their own lines around it. If the fact doesn't fit on one line, it's saying too much — cut it down to the single most important thing (per XML-001/002) and drop the rest. **No `<remarks>` tag either** — there is no overflow container; a second fact that doesn't fit gets cut, not relocated.

The structural, block-level tags — `<param>`, `<returns>`, `<exception>`, `<typeparam>` — are kept: they're not inline reference clutter, they're separate documented fields that Swagger and IntelliSense render distinctly. Their content follows the same plain-prose/no-parens rule but isn't held to the 3-line shape if a parameter genuinely needs more explaining.

```csharp
// ❌ VIOLATION — inline markup, a parenthetical, and text wrapped across 3 lines of prose
/// <summary>
/// Short-circuits to 0 when the appeal was accepted, without mutating the
/// original <see cref="PlayerSanction.Duration"/> (a 3-fecha sanction
/// stays <c>Duration = 3</c> in the record).
/// </summary>

// ❌ ALSO A VIOLATION — collapsed to one line instead of the 3-line shape
/// <summary>Short-circuits remaining fechas to 0 when the appeal was accepted, leaving Duration untouched.</summary>

// ✅ CORRECT — 3 lines, one line of plain-prose text, no parens
/// <summary>
/// Short-circuits remaining fechas to 0 when the appeal is accepted, leaving Duration untouched.
/// </summary>
```

---

## Rule FILE-001 — One type per file

Each `.cs` file declares exactly one top-level type: one class, interface, enum, record, or struct. A file with two top-level `public`/`internal` types (a common pattern in this codebase for small request/response DTO pairs, or an enum declared alongside the class that uses it) gets split into one file per type, named after the type it contains.

**Not a violation:** a private/nested type declared inside its owning class (it's not top-level), and `partial class` declarations of the same type split across multiple files (each file still declares exactly one type).

```csharp
// ❌ VIOLATION — two top-level types in one file (CreatePlayerRequest.cs)
public class CreatePlayerRequest { /* ... */ }
public class CreatePlayerAddressRequest { /* ... */ }

// ✅ CORRECT — split into CreatePlayerRequest.cs and CreatePlayerAddressRequest.cs
```

---

## What this document does not cover (yet)

Naming conventions, code style, nullable reference types, async/await conventions, error handling/logging, file/project organization, and testing standards are out of scope for this pass. If a future pass wants to define those, it should be grounded in this project's actual tooling the same way this document is — not adapted from an unrelated codebase's rulebook.
