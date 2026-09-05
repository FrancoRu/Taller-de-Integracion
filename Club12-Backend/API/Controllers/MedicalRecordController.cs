using API.Utils;

using Application.DTOs.MedicalRecord.Request;
using Application.DTOs.MedicalRecord.Response;
using Application.Interfaces.Services;
using Application.Interfaces.Storage;
using Application.Utils.Constants;

using Domain.Constants;
using Domain.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers;

/// <summary>
/// Manages player medical records and per-season eligibility, scoped to a player, team, and tournament rather than to the player globally.
/// </summary>
/// <param name="medicalRecordService">The medical-record service.</param>
/// <param name="medicalRecordStorage">The medical-record file storage boundary.</param>
[Route("api/medical-records/")]
[ApiController]
[Authorize(Roles = Roles.AdminOrOwner)]
public class MedicalRecordController(
    IMedicalRecordService medicalRecordService,
    IMedicalRecordStorage medicalRecordStorage) : ControllerBase
{
    /// <summary>
    /// Uploads a player's medical-record PDF for a team and tournament, starting the record in Pending status until it is approved.
    /// </summary>
    /// <param name="request">The player, team, tournament, and PDF file.</param>
    /// <returns>The resulting medical-record state, with status Pending.</returns>
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

        // Rejects the upload before touching storage when the ficha is already Approved, since an approved record is view or download only.
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
    /// Approves or rejects a player's medical record for a team and tournament, where approval grants eligibility and rejection can include an optional reason.
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
    /// Returns the current medical-record and eligibility state of a player's season registration.
    /// </summary>
    /// <param name="playerId">The player.</param>
    /// <param name="teamId">The team the player is registered to.</param>
    /// <param name="tournamentId">The tournament, i.e. the season.</param>
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
    /// Downloads a player's medical-record PDF by streaming it through the API rather than via a public URL, since the storage area is private.
    /// </summary>
    /// <param name="playerId">The player.</param>
    /// <param name="teamId">The team the player is registered to.</param>
    /// <param name="tournamentId">The tournament, i.e. the season.</param>
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
