using API.Utils;

using Application.DTOs.MedicalRecord.Request;
using Application.DTOs.MedicalRecord.Response;
using Application.Interfaces.Services;
using Application.Interfaces.Storage;
using Application.Utils.Constants;

using Domain.Constants;
using Domain.Enums;

using Infrastructure.Persistance;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Manages player medical records and the resulting per-season eligibility
/// (HU-55/56/57/58/59/62). A medical record is scoped to a player + team +
/// tournament (the season registration), never to the player globally, so the
/// same player in another team/tournament keeps a separate record.
/// </summary>
/// <param name="medicalRecordService">The medical-record service.</param>
/// <param name="medicalRecordStorage">The medical-record file storage boundary.</param>
/// <param name="medicalRecordSeedBackfiller">
/// The one-off admin backfill used only by <see cref="BackfillMedicalRecords"/>
/// — kept out of <c>DataMaintenanceService</c>'s constructor on purpose, since
/// that class is resolved through the shared DI container by ~20 tests that
/// have no override for the live-Supabase-constructor testability gap this
/// dependency drags in (see <see cref="MedicalRecordSeedBackfiller"/>).
/// </param>
[Route("api/medical-records/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class MedicalRecordController(
    IMedicalRecordService medicalRecordService,
    IMedicalRecordStorage medicalRecordStorage,
    MedicalRecordSeedBackfiller medicalRecordSeedBackfiller) : ControllerBase
{
    /// <summary>
    /// One-off admin action: runs the same idempotent, resumable
    /// medical-record backfill the startup seeder runs
    /// (medical-records-storage-eligibility, Part 3) against whatever data is
    /// already in the database right now, without requiring a reseed or a
    /// restart. Safe to call repeatedly — a registration that already has a
    /// real stored file is skipped.
    /// </summary>
    [HttpPost("backfill")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> BackfillMedicalRecords()
    {
        // approveNonApproved: true — this live dataset was seeded before the
        // Approved-by-default seed logic existed, so every registration is
        // still Pending; the normal (seed self-heal) candidate set would find
        // nothing to fix here.
        await medicalRecordSeedBackfiller.BackfillMedicalRecordsAsync(medicalRecordPath: null, approveNonApproved: true);
        return NoContent();
    }

    /// <summary>
    /// Uploads a player's medical-record file (PDF) for a specific team and
    /// tournament (HU-55). Stores the file in the dedicated medical-records
    /// area (HU-56) and records its reference on the season registration. The
    /// player is NOT habilitado yet — the record starts Pending and still has
    /// to be approved (HU-57/HU-58).
    /// </summary>
    /// <param name="request">The player, team, tournament, and PDF file.</param>
    /// <returns>The resulting medical-record state (status Pending).</returns>
    [HttpPost()]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(MedicalRecordResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MedicalRecordResponse>> UploadMedicalRecord([FromForm] UploadMedicalRecordRequest request)
    {
        if (!request.File.IsValidPdfFile())
        {
            return BadRequest(ErrorMessages.MedicalRecord.InvalidPdfFile);
        }

        // HU-57: reject the upload up front — before touching storage — when the
        // ficha is already Approved. An habilitado record is view/download only.
        MedicalRecordResponse? current = await medicalRecordService.GetAsync(
            request.PlayerId, request.TeamId, request.TournamentId);

        if (current?.Status == MedicalRecordStatus.Approved)
        {
            return Conflict(ErrorMessages.MedicalRecord.AlreadyApproved);
        }

        string fileReference = await medicalRecordStorage.StoreAsync(
            request.TeamId,
            request.PlayerId,
            request.File.FileName,
            request.File.OpenReadStream());

        MedicalRecordResponse response = await medicalRecordService.RecordUploadAsync(
            request.PlayerId, request.TeamId, request.TournamentId,
            fileReference, request.File.FileName, GetActor());

        return new ObjectResult(response) { StatusCode = StatusCodes.Status201Created };
    }

    /// <summary>
    /// Approves or rejects a player's medical record for a team and tournament
    /// (HU-58). Approve → habilitado (HU-57); reject → not-habilitado with the
    /// optional reason.
    /// </summary>
    /// <param name="request">The player, team, tournament, decision, and reason.</param>
    /// <returns>The resulting medical-record state.</returns>
    [HttpPut("review")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MedicalRecordResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MedicalRecordResponse>> ReviewMedicalRecord(ReviewMedicalRecordRequest request)
    {
        MedicalRecordResponse response = await medicalRecordService.ReviewAsync(
            request.PlayerId, request.TeamId, request.TournamentId,
            request.Approve, request.Reason, GetActor());

        return Ok(response);
    }

    /// <summary>
    /// Returns the current medical-record / eligibility state of a player's
    /// season registration (HU-62), so the frontend can show a habilitado /
    /// not-habilitado badge.
    /// </summary>
    /// <param name="playerId">The player.</param>
    /// <param name="teamId">The team the player is registered to.</param>
    /// <param name="tournamentId">The tournament (season).</param>
    /// <returns>The medical-record state, or 404 when none exists.</returns>
    [HttpGet()]
    [Authorize(Roles = Roles.AdminOrOwner)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MedicalRecordResponse))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicalRecordResponse>> GetMedicalRecord(
        [FromQuery] Guid playerId, [FromQuery] Guid teamId, [FromQuery] Guid tournamentId)
    {
        MedicalRecordResponse? response = await medicalRecordService.GetAsync(playerId, teamId, tournamentId);

        return response is null
            ? this.NotFoundProblem(nameof(MedicalRecordResponse), $"{playerId}/{teamId}/{tournamentId}")
            : Ok(response);
    }

    /// <summary>
    /// Downloads the PDF file of a player's uploaded medical record for a team
    /// and tournament (HU-55/HU-56). The medical-records storage area is
    /// private, so the file is streamed back through the API rather than via a
    /// public URL.
    /// </summary>
    /// <param name="playerId">The player.</param>
    /// <param name="teamId">The team the player is registered to.</param>
    /// <param name="tournamentId">The tournament (season).</param>
    /// <returns>The stored PDF, or 404 when no record/file exists.</returns>
    [HttpGet("download")]
    [Authorize(Roles = Roles.AdminOrOwner)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadMedicalRecord(
        [FromQuery] Guid playerId, [FromQuery] Guid teamId, [FromQuery] Guid tournamentId)
    {
        MedicalRecordResponse? record = await medicalRecordService.GetAsync(playerId, teamId, tournamentId);

        if (record?.FileUrl is null)
        {
            return this.NotFoundProblem(nameof(MedicalRecordResponse), $"{playerId}/{teamId}/{tournamentId}");
        }

        byte[] content = await medicalRecordStorage.DownloadAsync(record.FileUrl);
        string fileName = string.IsNullOrWhiteSpace(record.FileName) ? "medical-record.pdf" : record.FileName;

        return File(content, "application/pdf", fileName);
    }

    private string GetActor()
    {
        return User?.Identity?.Name
            ?? User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? AuditConstants.SystemUser;
    }
}
