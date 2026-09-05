using Application.DTOs.Roster.Response;

using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Clones a roster from a previous season's team into a new season's team so admins don't re-enter every player each year.
/// </summary>
public interface IRosterCopyService
{
    /// <summary>
    /// Creates a new season registration on targetTeamId for targetTournamentId for every player registered to the source team in the source season.
    /// </summary>
    /// <param name="sourceTeamId">The past-season team to copy from.</param>
    /// <param name="sourceTournamentId">The season the source roster belongs to.</param>
    /// <param name="targetTeamId">The new-season team to copy into.</param>
    /// <param name="targetTournamentId">The new season the roster is cloned into.</param>
    /// <returns>How many registrations were created and how many were skipped.</returns>
    Task<RosterCopyResult> CopyRosterAsync(
        Guid sourceTeamId, Guid sourceTournamentId, Guid targetTeamId, Guid targetTournamentId);
}
