# Exploration — medical-records-storage-eligibility

> Phase: sdd-explore · Store: hybrid (Engram topic `sdd/medical-records-storage-eligibility/explore` id 180 + this file)
> Read-only investigation. No production code changed.

## The change (3 parts)

1. Medical-record files move from the `public-images` bucket to a dedicated **private** `medical-records` bucket; object key becomes `{teamId}/{playerId}/...`.
2. A player with no medical-record file actually uploaded MUST NOT be "habilitado".
3. New MedicalRecords seed that uploads `C:\Users\Franco\Downloads\ficha-medica-club12.pdf` to the `medical-records` bucket for every approved player with no real file (~320 uploads).

**User decisions already taken:** bucket `medical-records` is PRIVATE (owner set it). Files stay served through the existing authenticated backend streaming endpoint (Option A). Path scheme `/medical-records/{teamId}/{playerId}/`.

## Current state (file:line)

### 1. Medical-record model — on `PlayerTeamRegistration` (per player+team+tournament = per season)

`Domain/Entities/Models/PlayerTeamRegistration.cs:54-78`:
- `MedicalRecordStatus` enum Pending/Approved/Rejected — `Domain/Enums/MedicalRecordStatus.cs`
- `MedicalRecordFileUrl` (string?) — **misnamed; stores a bucket-relative OBJECT PATH, not a URL**
- `MedicalRecordFileName` (string?)
- `MedicalRecordReviewReason` (string?), `MedicalRecordReviewedAt` (DateTime?)
- No reviewer column — actor via `EntityBase.UpdatedBy`.

EF config: `Infrastructure/Persistance/Configurations/PlayerTeamRegistrationEntityConfiguration.cs:23-30` (`MedicalRecordStatus` → `HasConversion<string>()`).
Migration: `Infrastructure/Migrations/20260828020000_AddMedicalRecordToPlayerTeamRegistration.cs` — 5 columns, status `text` NOT NULL default `"Pending"`.

### 2. Upload flow

`API/Controllers/MedicalRecordController.cs:51-79` `UploadMedicalRecord([FromForm] UploadMedicalRecordRequest)`, `[Authorize(AdminOrOwner)]`:
- PDF check → 409 pre-check `current?.Status == Approved` (`:63-66`)
- `medicalRecordStorage.StoreAsync(request.TournamentId, request.PlayerId, fileName, stream)` → `Infrastructure/Storage/SupabaseMedicalRecordStorage.cs:27-36`: builds key `medical-records/{tournamentId}/{playerId}/{Guid.NewGuid()}{ext}`, calls `rawStorage.UploadRawAsync(objectPath, content)`.
- `SupabaseHelper.UploadRawAsync` (`Infrastructure/Storage/SupabaseHelper.cs:115-127`) → `_client.Storage.From(_bucketName).Upload(...)`, `_bucketName` = `SupaBase:BucketName` = **`"public-images"`**, `Upsert=true`.
- Persisted reference = the bucket-relative object path (`Application/Services/MedicalRecordService.cs:27-52`). Upload always forces status back to `Pending`.
- 409-on-reupload-when-approved: controller `:63-66` AND service `MedicalRecordService.cs:35-38` (`ErrorMessages.MedicalRecord.AlreadyApproved`).

### 3. Download / streaming flow

`MedicalRecordController.cs:138-152` `DownloadMedicalRecord`, `[Authorize(AdminOrOwner)]` → `medicalRecordStorage.DownloadAsync(record.FileUrl)` → `SupabaseMedicalRecordStorage.cs:38-41` → `SupabaseHelper.DownloadRawAsync` → `_client.Storage.From(_bucketName).Download(objectPath)`. Uses the **service-role key** (SupabaseHelper ctor `:37-52`). No signed URL, no public URL — bytes streamed through the API as `File(content, "application/pdf", fileName)`. **Option A is already how it works today.**

### 4. SupabaseHelper

`Infrastructure/Storage/SupabaseHelper.cs`, implements `Application/Utils/Helper/SupabaseHelper/ISupabaseRawStorage.cs`:
- `UploadImageAsync<T>(stream, fileName)` — key `{typeof(T).Name}/{guid}{ext}`, `Upsert=true`, returns `GetPublicUrl(...)`.
- `DeleteImageAsync<T>(fileName)`.
- raw (image-agnostic): `UploadRawAsync(objectPath, content)`, `ListRawAsync(prefix)`, `RemoveRawAsync(objectPath)`, `DownloadRawAsync(objectPath)`.
- **Every method is hardcoded to the single `_bucketName`** from `SupaBase:BucketName`. `.Storage.From(bucket)` accepts any bucket string, but the helper never varies it.
- **Never creates buckets** — assumes existence. Registered **singleton** (`API/Utils/StartupExtensions.cs:353,360-361`); ctor does blocking `_client.InitializeAsync().Wait()`.
- Config keys: `Application/Utils/Constants/Configuration/ConfigurationKeys.cs:53-58` (`Section="SupaBase"`, `ProjectUrl`, `ServiceRole`, `BucketName`).
- `Infrastructure/Backup/SupabaseBackupStorage.cs` reuses the same boundary with a `backups/` prefix in the same bucket.

### 5. Eligibility reads — rule `status == Approved` duplicated in

1. `Domain/Entities/Models/Player.cs:91` `IsHabilitado => MedicalRecordStatus == Approved` (`[NotMapped]` transient; transient `MedicalRecordStatus` `:82`)
2. `Application/Services/TeamService.cs:267` `AttachSeasonRostersAsync` surfaces `r.MedicalRecordStatus` onto `r.Player.MedicalRecordStatus`
3. `Application/DTOs/MedicalRecord/Response/MedicalRecordResponse.cs:49` `IsHabilitado = status == Approved`
4. `Application/Services/PlayerStatisticService.cs:185` — match-sheet enforcement: `if (sanctioned || registration.MedicalRecordStatus != Approved) throw PlayerNotEligible`
5. `Application/DTOs/Player/Response/PublicPlayerResponse.cs:55` `IsHabilitado` — AutoMapper name-convention from `Player.IsHabilitado`; exposed on `[AllowAnonymous]` `GET api/players/{idOrSlug}` and `/public`
6. Frontend: `Club12-WebClient/src/views/medicalRecord/HabilitacionBadge.tsx`, `PlayerMedicalRecordDialog.tsx:69` — mirror backend values only

### 6. Approve flow — the core gap for part 2

`MedicalRecordService.ReviewAsync` (`Application/Services/MedicalRecordService.cs:54-67`) sets `Approved` with **no check that `MedicalRecordFileUrl` is set**. Controller `ReviewMedicalRecord` (`:92-99`) has no validation. So a record can be `Approved` with no file. Existing tests approve with no file: `API.Tests/MedicalRecordEligibilityTests.cs:117-133, 177-196, 200-244`.

### 7. Seed

`Infrastructure/Persistance/SampleTournamentBuilder.cs:317-365`: `bool isHabilitado = p < 7` (7/8 per team). Approved rows get `MedicalRecordFileUrl = SampleMedicalRecordFileUrl` = `"medical-records/sample/ficha-medica.pdf"` (`:47`) — a **fake path pointing at no real object** (download fails).
`Infrastructure/Persistance/DataSeeder.cs`: ctor `(ApplicationDBContext, ILogger<DataSeeder>, SupabaseHelper)` `:42-45`; `SeedAsync(bool reset=false, string? logosPath=null)` `:167`; skip-if-teams-exist unless reset `:173-177`. Escudo upload `UploadTeamLogosAsync` `~:365-420`: `Directory.GetFiles(logosPath,"*.png")`, per team `supabaseHelper.UploadImageAsync<Team>(stream, name)` → sets `LogoUrl`; per-file try/catch → warn + keep placeholder, **never fails the seed**; missing folder → warn + skip.
Wiring: `API/Utils/StartupExtensions.cs:144-173` — `if GetValue<bool>(Seed:Enabled)` → reads `Seed:Reset`, `Seed:LogosPath` → `dataSeeder.SeedAsync(reset, logosPath)`. Config keys `API/Utils/ConfigurationKeys.cs:44-61`; `DataSeeder.DefaultLogosPath` const `:52`.
`Application/Services/RosterCopyService.cs:54-56` deliberately does NOT copy medical fields (HU-59).

### 8. Data-migration concern

Existing `MedicalRecordFileUrl` values: seeded rows = fake `medical-records/sample/ficha-medica.pdf`; manual-test uploads = `medical-records/{tournamentId}/{playerId}/{guid}.pdf` inside `public-images`. After the helper repoints to a new bucket these old refs break.
Options: (a) **go-forward + dev reseed only** — the new seed rewrites refs; prod real volume ≈ 0, `main` untouched; (b) **full migration tool** — copy objects + rewrite refs (path also changes `tournamentId`→`teamId`). Recommend (a); document (b).

### 9. Bucket provisioning

The Supabase .NET client `Upload` does **not** create buckets, and `.From(missingBucket)` errors. `medical-records` must be **created manually in the Supabase dashboard as a PRIVATE bucket** before this ships. API keeps working because it uses the service-role key. Required manual pre-deploy step. **(Owner has already created it private.)**

### 10. Tests

`API.Tests/MedicalRecordEligibilityTests.cs` (SQLite integration host), `API.Tests/MedicalRecordDownloadTests.cs` (direct controller + fakes; asserts `objectPath.StartsWith("medical-records/")` `:49`; `InMemoryRawStorage` fake), `API.Tests/SupabaseDependentControllerNotFoundTests.cs`, `API.Tests/Backup/Fakes/FakeSupabaseRawStorage.cs`. `CustomWebApplicationFactory.cs` replaces only the two DbContexts — **does NOT stub Supabase**, so the seed / SupabaseHelper cannot be integration-tested through the host. strict_tdd active → new guard tests plus fixing the 3 approve-without-file tests must land in the same change.

### 11. Frontend impact

None required. `fileUrl` is opaque (`Club12-WebClient/src/modules/medicalRecord/service/medicalRecord.service.ts:73`), download goes through the streaming `/download` endpoint (`:91`), the badge only mirrors backend `isHabilitado`. Optional UX: disable "Aprobar" when `!hasStoredFile` (`PlayerMedicalRecordDialog.tsx:73,294-327`).

## Scope

Backend (primary) + one manual Supabase step (create private `medical-records` bucket — done) + optional tiny frontend UX. **EF migration NOT required** (no schema change); optional only if a DB CHECK invariant "Approved ⇒ file" is wanted.

## Approaches

### Storage relocation
| # | Approach | Pros | Cons | Effort |
|---|----------|------|------|--------|
| 1 | Bucket-parameterized raw boundary: optional `bucket` arg/overloads on `ISupabaseRawStorage` (default = configured bucket) + new `SupaBase:MedicalRecordsBucketName` (default `"medical-records"`); `SupabaseMedicalRecordStorage` passes it. Go-forward only + dev reseed. | Minimal; private data isolated; backup path untouched; fake-testable | Interface ripple to fake + backup adapter (mitigate with overload); prefix redundancy to resolve | M |
| 2 | Second `SupabaseHelper`/client keyed to the medical bucket | Strong separation | Extra DI wiring; second blocking `InitializeAsync().Wait()` at startup | M-H |
| 3 | Keep `public-images`, only change path prefix | Trivial | Fails "PRIVATE bucket" — can't privatise `public-images` without breaking logos/blog | S (rejected) |

**Recommendation: 1**, go-forward only for data + reseed for dev.

### Eligibility enforcement
| # | Approach | Pros | Cons | Effort |
|---|----------|------|------|--------|
| A | Write-side guard in `ReviewAsync`: reject approve when `MedicalRecordFileUrl` null/whitespace | Keeps invariant "Approved ⟺ file present"; read sites untouched | Fix 3 tests + seed; pre-existing fake-ref rows still pass (non-null) → reseed/backfill needed | S |
| B | Read-side: `IsHabilitado = status == Approved && fileUrl != null` in canonical spots | Robust vs bad data | 3+ sites; must surface `fileUrl` onto `Player` transient + into match-sheet check | M |
| C | A + B + DB CHECK constraint (migration) | Most defensive | Adds a migration | M-H |

**Recommendation: A** as the rule + light **B** in `MedicalRecordResponse.FromRegistration` and `Player.IsHabilitado` as defense-in-depth; DB constraint optional.

### Seed
| # | Approach | Pros | Cons | Effort |
|---|----------|------|------|--------|
| 1 | One-shot inside reseed, degrade-on-failure like logos, inject `IMedicalRecordStorage`, gate on `Seed:MedicalRecordPath` presence | Mirrors escudo pattern exactly | ~320 uploads every `--Seed:Reset` | M |
| 2 | Idempotent — skip registrations whose `MedicalRecordFileUrl` already resolves in the new bucket scheme; resumable | Cheap re-runs, survives interruption | Slightly more logic | M |
| 3 | Separate gate `Seed:MedicalRecords=true`, runnable against an already-seeded DB | Can backfill without a full wipe | One more flag | M |

**Recommendation: 2 + 3** — piggyback the seed flow, gate on `Seed:MedicalRecordPath` (skip whole step if unset/missing, like `LogosPath`), make each upload idempotent, never fail the seed on upload error. Use `IMedicalRecordStorage.StoreAsync` (same boundary as the controller), not `UploadImageAsync<T>`.

## Open decisions for `sdd-propose`

a) bucket-param overload vs second client
b) keep `medical-records/` prefix inside a bucket also named `medical-records`, or drop it (user path spec is `/medical-records/{teamId}/{playerId}/`)
c) `StoreAsync` partition key = `teamId` only (user spec) or `teamId`+`tournamentId` — dropping `tournamentId` drops the season dimension from the object path (still unique: a team belongs to one tournament)
d) go-forward-only vs full object migration for prod
e) DB CHECK constraint (→ migration) or domain-only
f) whether `SampleTournamentBuilder` should leave the approved rows' file ref null for the new seed to fill

## Risks

- `medical-records` bucket must exist and be private before deploy or every upload/download fails (client never creates buckets). — **done by owner**
- 3 existing tests approve with no file and break under the write guard — must fix in the same change (strict TDD).
- `MedicalRecordDownloadTests` path assertion + `StoreAsync(tournamentId→teamId)` signature ripple through `IMedicalRecordStorage`, controller, fakes.
- Seed of ~320 real network uploads is slow/flaky — needs idempotency + resumability.
- Existing rows reference `public-images`; their download breaks after the repoint unless migrated or reseeded.
- `SupabaseHelper` is a startup-blocking singleton; approach 2 would add a second blocking init.
- Changing the path key from `tournamentId` to `teamId` drops the season dimension from the object path — acceptable, note it.
