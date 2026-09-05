using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerStatistic.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Extensions;
using Application.Utils.Helper.MatchResult;
using Application.Utils.Helper.Tournament;

using Domain.Constants;
using Domain.Entities.Models;
using Domain.Enums;

using LinqKit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Records player statistics, most notably match scoring sheets: a team's
/// final score is always derived as the sum of the per-player points loaded
/// here, never entered independently (see <see cref="LoadTeamMatchSheetAsync"/>
/// and <see cref="LoadMatchResultFromSheetsAsync"/>).
/// </summary>
public class PlayerStatisticService(IUnitOfWork unitOfWork, IStageService stageService) : IPlayerStatisticService
{
    private readonly IPlayerStatisticRepository _playerStatisticRepository = unitOfWork.PlayerStatisticRepository;
    private readonly IMatchRepository _matchRepository = unitOfWork.MatchRepository;
    private readonly IPlayerRepository _playerRepository = unitOfWork.PlayerRepository;
    private readonly IPlayerTeamRegistrationRepository _playerTeamRegistrationRepository = unitOfWork.PlayerTeamRegistrationRepository;

    public async Task<PlayerStatistic> CreatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity)
    {
        await _playerStatisticRepository.AddAsync(playerStatisticEntity);
        return playerStatisticEntity;
    }

    public async Task<PlayerStatistic?> GetPlayerStatisticByIdAsync(Guid playerStatisticId)
    {
        return await _playerStatisticRepository.GetByIdAsync(playerStatisticId);
    }

    public async Task DeletePlayerStatisticAsync(Guid id)
    {
        await _playerStatisticRepository.RemoveAsync(playerStatistic => playerStatistic.Id == id);
    }

    public async Task UpdatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity)
    {
        await _playerStatisticRepository.UpdateAsync(playerStatisticEntity);
    }

    public async Task<PaginatedResponse<PlayerStatistic>> GetPlayerStatisticsAsync(GetPlayerStatisticsFilteredRequest filter)
    {
        Expression<Func<PlayerStatistic, bool>> expression =
            QueryableExtensions.ConstructFilterExpression<PlayerStatistic, GetPlayerStatisticsFilteredRequest>(filter);

        if (filter.TeamId.HasValue)
        {
            Expression<Func<PlayerStatistic, bool>> teamExpression =
                playerStatistic => playerStatistic.Player != null && playerStatistic.Player.TeamId == filter.TeamId.Value;
            expression = expression.And(teamExpression);
        }

        IEnumerable<PlayerStatistic> filteredStatistics = await _playerStatisticRepository.FindAsync(
            expression,
            includes: [playerStatistic => playerStatistic.Match!, playerStatistic => playerStatistic.Player!],
            filter: filter);

        int totalCount = await _playerStatisticRepository.CountAsync(expression);

        return new PaginatedResponse<PlayerStatistic>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredStatistics
        };
    }

    /// <summary>
    /// Loads a whole team's scoring sheet for a match in one coherent operation
    /// (HU-71). Validates that the match has a final score, that the team played
    /// in it, that every listed player is on the team's roster for that season
    /// and eligible (no active sanction), and that the players' points add up to
    /// the team's final score. Only if every check passes are the team's Points
    /// statistics for that match replaced with the new set (so corrections
    /// recalculate the ranking). Any failure throws and persists nothing.
    /// </summary>
    /// <param name="request">The match, team, and per-player points.</param>
    /// <returns>The persisted Points statistics for the team in this match.</returns>
    /// <exception cref="InvalidOperationException">Thrown when any coherence or eligibility check fails.</exception>
    public async Task<List<PlayerStatistic>> LoadTeamMatchSheetAsync(LoadMatchSheetRequest request)
    {
        Match? match = await _matchRepository.GetByIdAsync(request.MatchId,
            includes: [m => m.HomeTeam!, m => m.VisitorTeam!]);

        if (match is null)
        {
            throw new InvalidOperationException(ErrorMessages.MatchSheet.MatchNotFinished(request.MatchId));
        }

        bool teamIsHome = match.HomeTeamId == request.TeamId;
        bool teamIsVisitor = match.VisitorTeamId == request.TeamId;

        if (!teamIsHome && !teamIsVisitor)
        {
            throw new InvalidOperationException(ErrorMessages.MatchSheet.TeamNotInMatch(request.TeamId));
        }

        int? teamScore = teamIsHome ? match.HomeScore : match.VisitorScore;
        if (!match.IsFinished || teamScore is null)
        {
            throw new InvalidOperationException(ErrorMessages.MatchSheet.MatchNotFinished(request.MatchId));
        }

        int playersSum = request.Scores.Sum(entry => entry.Points);
        if (playersSum != teamScore.Value)
        {
            throw new InvalidOperationException(ErrorMessages.MatchSheet.ScoreMismatch(teamScore.Value, playersSum));
        }

        Team teamEntity = (teamIsHome ? match.HomeTeam : match.VisitorTeam)
            ?? throw new InvalidOperationException(ErrorMessages.MatchSheet.TeamNotInMatch(request.TeamId));

        await EnsureTeamMeetsHabilitadoMinimumAsync(teamEntity);

        List<string> issues = await FindRosterEligibilityIssuesAsync(request.Scores, request.TeamId, teamEntity);
        if (issues.Count > 0)
        {
            throw new InvalidOperationException(
                ErrorMessages.MatchSheet.PlayersNotEligible([(teamEntity.Name, issues)]));
        }

        await ReplaceTeamPointsForMatchAsync(request.MatchId, request.TeamId);

        List<PlayerStatistic> created = [.. request.Scores.Select(entry => new PlayerStatistic
        {
            MatchId = request.MatchId,
            PlayerId = entry.PlayerId,
            Value = entry.Points,
            Type = StatisticType.Points,
            CreatedBy = teamEntity.UpdatedBy ?? teamEntity.CreatedBy ?? AuditConstants.SystemUser,
        })];

        if (created.Count > 0)
        {
            await _playerStatisticRepository.AddRangeAsync(created);
        }

        return created;
    }

    /// <summary>
    /// Finishes a match by loading BOTH teams' scoring sheets in one coherent
    /// operation (HU-72): the final score is DERIVED as the sum of each
    /// team's listed player points — not typed in separately and checked
    /// against a sheet afterward, the way <see cref="LoadTeamMatchSheetAsync"/>
    /// requires. Validates every listed player is on their team's roster and
    /// eligible, then finalizes the match (rejecting a tie, HU-70) and
    /// replaces both teams' Points statistics with the new sheets. Any
    /// failure throws before anything is persisted.
    /// </summary>
    /// <param name="request">The match and both teams' per-player points.</param>
    /// <returns>The finalized match (score, winner, IsFinished all set), or null if no match with that id exists.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a player is ineligible/off the roster or the resulting
    /// score is tied.
    /// </exception>
    public async Task<Match?> LoadMatchResultFromSheetsAsync(LoadMatchResultFromSheetsRequest request)
    {
        Match? match = await _matchRepository.GetByIdAsync(request.MatchId,
            includes: [m => m.HomeTeam!, m => m.VisitorTeam!, m => m.Stage]);

        if (match is null)
        {
            return null;
        }

        Team homeTeam = match.HomeTeam
            ?? throw new InvalidOperationException(ErrorMessages.MatchSheet.MatchMissingTeams(request.MatchId));
        Team visitorTeam = match.VisitorTeam
            ?? throw new InvalidOperationException(ErrorMessages.MatchSheet.MatchMissingTeams(request.MatchId));

        await EnsureTeamMeetsHabilitadoMinimumAsync(homeTeam);
        await EnsureTeamMeetsHabilitadoMinimumAsync(visitorTeam);

        List<string> homeIssues = await FindRosterEligibilityIssuesAsync(request.HomeScores, homeTeam.Id, homeTeam);
        List<string> visitorIssues = await FindRosterEligibilityIssuesAsync(request.VisitorScores, visitorTeam.Id, visitorTeam);
        if (homeIssues.Count > 0 || visitorIssues.Count > 0)
        {
            List<(string TeamName, List<string> Issues)> issuesByTeam = [];
            if (homeIssues.Count > 0) issuesByTeam.Add((homeTeam.Name, homeIssues));
            if (visitorIssues.Count > 0) issuesByTeam.Add((visitorTeam.Name, visitorIssues));

            throw new InvalidOperationException(ErrorMessages.MatchSheet.PlayersNotEligible(issuesByTeam));
        }

        int homeScore = request.HomeScores.Sum(entry => entry.Points);
        int visitorScore = request.VisitorScores.Sum(entry => entry.Points);

        // Validated above and the tie-check below both throw before this line
        // touches the database — nothing is written on a rejected sheet.
        MatchResultFinalizer.ApplyResult(match, homeScore, visitorScore);
        match.WentToOvertime = request.WentToOvertime;
        await _matchRepository.UpdateAsync(match);
        await stageService.TryAutoSeedPlayoffPhaseAsync(match.StageId);

        await ReplaceTeamPointsForMatchAsync(match.Id, homeTeam.Id);
        await ReplaceTeamPointsForMatchAsync(match.Id, visitorTeam.Id);

        List<PlayerStatistic> created = [
            .. BuildPointsStatistics(match.Id, request.HomeScores, homeTeam),
            .. BuildPointsStatistics(match.Id, request.VisitorScores, visitorTeam),
        ];

        if (created.Count > 0)
        {
            await _playerStatisticRepository.AddRangeAsync(created);
        }

        return match;
    }

    private static List<PlayerStatistic> BuildPointsStatistics(Guid matchId, List<PlayerScoreEntry> scores, Team teamEntity) =>
        [.. scores.Select(entry => new PlayerStatistic
        {
            MatchId = matchId,
            PlayerId = entry.PlayerId,
            Value = entry.Points,
            Type = StatisticType.Points,
            CreatedBy = teamEntity.UpdatedBy ?? teamEntity.CreatedBy ?? AuditConstants.SystemUser,
        })];

    /// <summary>
    /// Enforces the walkover threshold: a team fielding fewer than
    /// <see cref="TournamentCompletabilityValidator.MinPlayersPerTeam"/>
    /// HABILITADO players for this season can never legally field a lineup,
    /// so its match can't be recorded as a normal result — it must be loaded
    /// as a walkover instead (<see cref="IMatchService.LoadWalkOverAsync"/>).
    /// Checked independently of which players the sheet actually lists — a
    /// team already below the threshold fails this even with an empty sheet.
    /// </summary>
    private async Task EnsureTeamMeetsHabilitadoMinimumAsync(Team teamEntity)
    {
        if (teamEntity.TournamentId is null)
        {
            return;
        }

        Guid tournamentId = teamEntity.TournamentId.Value;

        int habilitadoCount = (await _playerTeamRegistrationRepository.FindAsync(
                registration => registration.TeamId == teamEntity.Id && registration.TournamentId == tournamentId))
            .Count(registration => registration.IsHabilitado);

        if (habilitadoCount < TournamentCompletabilityValidator.MinPlayersPerTeam)
        {
            throw new InvalidOperationException(
                ErrorMessages.MatchSheet.TeamRequiresWalkOver(teamEntity.Name, habilitadoCount));
        }
    }

    /// <summary>
    /// Checks every listed player is registered to the team for the match's
    /// season (HU-98) and is eligible — an approved registration and no active
    /// sanction (HU-60/HU-61) — and returns one readable reason per violation
    /// instead of throwing on the first one found, so a caller can report
    /// every offending player across both teams in a single error.
    /// </summary>
    private async Task<List<string>> FindRosterEligibilityIssuesAsync(
        List<PlayerScoreEntry> scores, Guid teamId, Team teamEntity)
    {
        List<string> issues = [];

        List<Guid> playerIds = [.. scores.Select(entry => entry.PlayerId).Distinct()];
        if (playerIds.Count == 0)
        {
            return issues;
        }

        Dictionary<Guid, Player> playersById = (await _playerRepository.FindAsync(player => playerIds.Contains(player.Id)))
            .ToDictionary(player => player.Id);

        if (teamEntity.TournamentId is null)
        {
            foreach (Guid playerId in playerIds)
            {
                issues.Add(ErrorMessages.MatchSheet.PlayerNotOnRosterReason(PlayerLabel(playersById, playerId)));
            }

            return issues;
        }

        Guid tournamentId = teamEntity.TournamentId.Value;

        Dictionary<Guid, PlayerTeamRegistration> registrationsByPlayer = (await _playerTeamRegistrationRepository.FindAsync(
            registration => registration.TeamId == teamId
                && registration.TournamentId == tournamentId
                && playerIds.Contains(registration.PlayerId)))
            .ToDictionary(registration => registration.PlayerId);

        foreach (Guid playerId in playerIds)
        {
            string label = PlayerLabel(playersById, playerId);

            if (!registrationsByPlayer.TryGetValue(playerId, out PlayerTeamRegistration? registration))
            {
                issues.Add(ErrorMessages.MatchSheet.PlayerNotOnRosterReason(label));
                continue;
            }

            // Eligible only when NOT sanctioned (HU-61) AND habilitado for this
            // team+tournament (HU-57/HU-60): the medical record must be Approved
            // AND carry a real stored file (medical-records-storage-eligibility)
            // — Approved alone is not enough. A Pending or Rejected registration
            // — including a brand-new season's, which never inherits a prior
            // approval (HU-59) — is not habilitado either.
            bool sanctioned = !playersById.TryGetValue(playerId, out Player? player) || player.IsSanctioned;
            if (sanctioned || !registration.IsHabilitado)
            {
                issues.Add(ErrorMessages.MatchSheet.PlayerNotEligibleReason(label));
            }
        }

        return issues;
    }

    /// <summary>
    /// A human-readable label for a player in an error message — the same
    /// "APELLIDO Nombre" the admin sees on the sheet — falling back to the id
    /// only for the (should-never-happen) case of a score entry naming a
    /// player that doesn't exist at all.
    /// </summary>
    private static string PlayerLabel(Dictionary<Guid, Player> playersById, Guid playerId) =>
        playersById.TryGetValue(playerId, out Player? player) ? player.FullName : playerId.ToString();

    /// <summary>
    /// Removes the team's existing Points statistics for a match so a corrected
    /// sheet fully replaces the previous one (HU-71 editability).
    /// </summary>
    private async Task ReplaceTeamPointsForMatchAsync(Guid matchId, Guid teamId)
    {
        List<Guid> existingIds = [.. (await _playerStatisticRepository.FindAsync(
            statistic => statistic.MatchId == matchId && statistic.Type == StatisticType.Points,
            includes: [statistic => statistic.Player!]))
            .Where(statistic => statistic.Player != null && statistic.Player.TeamId == teamId)
            .Select(statistic => statistic.Id)];

        if (existingIds.Count > 0)
        {
            await _playerStatisticRepository.RemoveAsync(statistic => existingIds.Contains(statistic.Id));
        }
    }
}
