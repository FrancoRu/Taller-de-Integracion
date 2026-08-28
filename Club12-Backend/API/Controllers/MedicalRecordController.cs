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
/// Manages player medical records and the resulting per-season eligibility
/// (HU-55/56/57/58/59/62). A medical record is scoped to a player + team +
/// tournament (the season registration), never to the player globally, so the
/// same player in another team/tournament keeps a separate record.
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
    public async Task<ActionResult<MedicalRecordResponse>> UploadMedicalRecord([FromForm] UploadMedicalRecordRequest request)
    {
        if (!request.File.IsValidPdfFile())
        {
            return BadRequest(ErrorMessages.MedicalRecord.InvalidPdfFile);
        }

        string fileReference = await medicalRecordStorage.StoreAsync(
            request.TournamentId,
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

    private string GetActor()
    {
        return User?.Identity?.Name
            ?? User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? AuditConstants.SystemUser;
    }
}
