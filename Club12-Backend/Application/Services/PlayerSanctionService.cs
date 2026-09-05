using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerSanction.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;
using Application.Utils.Helper.Slug;

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
/// Manages sanctions issued to a player, team, or staff member, identified by SanctionSubjectType.
/// </summary>
public class PlayerSanctionService(IUnitOfWork unitOfWork) : IPlayerSanctionService
{
    private readonly IPlayerSanctionRepository _playerSanctionRepository = unitOfWork.PlayerSanctionRepository;
    private readonly IPlayerRepository _playerRepository = unitOfWork.PlayerRepository;
    private readonly IMatchRepository _matchRepository = unitOfWork.MatchRepository;
    private readonly ITeamRepository _teamRepository = unitOfWork.TeamRepository;

    /// <summary>
    /// Creates a sanction and assigns it a unique slug derived from its subject's resolved name and issue date.
    /// </summary>
    public async Task<PlayerSanction> CreatePlayerSanctionAsync(PlayerSanction playerSanctionEntity)
    {
        string subjectName = await ResolveSubjectNameAsync(playerSanctionEntity);

        playerSanctionEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            $"{subjectName} {playerSanctionEntity.IssuedDate:yyyy-MM-dd}",
            candidate => _playerSanctionRepository.ExistsAsync(sanction => sanction.Slug == candidate));

        await _playerSanctionRepository.AddAsync(playerSanctionEntity);
        return playerSanctionEntity;
    }

    /// <summary>
    /// Resolves the human-readable name of a sanction's subject, for a sensible slug.
    /// </summary>
    private async Task<string> ResolveSubjectNameAsync(PlayerSanction sanction)
    {
        switch (sanction.SubjectType)
        {
            case SanctionSubjectType.Team:
                if (sanction.TeamId.HasValue)
                {
                    Team? team = await _teamRepository.GetByIdAsync(sanction.TeamId.Value);
                    return team?.Name ?? "equipo";
                }
                return "equipo";

            case SanctionSubjectType.Staff:
                return string.IsNullOrWhiteSpace(sanction.StaffName) ? "staff" : sanction.StaffName;

            // SanctionSubjectType.Player and any future/default value.
            default:
                if (sanction.PlayerId.HasValue)
                {
                    Player? player = await _playerRepository.GetByIdAsync(sanction.PlayerId.Value);
                    return player?.FullName ?? "jugador";
                }
                return "jugador";
        }
    }

    /// <summary>
    /// Computes how many fechas of the sanction are still to be served.
    /// </summary>
    /// <returns>
    /// The number of fechas remaining, or null when it cannot be computed by rounds: a staff
    /// sanction, an unknown team, or a match with no round.
    /// </returns>
    public async Task<int?> GetFechasRemainingAsync(PlayerSanction sanction)
    {
        if (sanction.AppealStatus == SanctionAppealStatus.Accepted)
        {
            return 0;
        }

        Guid? teamId = await ResolveSubjectTeamIdAsync(sanction);
        if (!teamId.HasValue)
        {
            return null;
        }

        Match? sanctionMatch = await _matchRepository.GetByIdAsync(sanction.MatchId);
        if (sanctionMatch?.Round is not int sanctionRound)
        {
            return null;
        }

        Guid teamValue = teamId.Value;
        Guid stageId = sanctionMatch.StageId;

        int roundsServed = await _matchRepository.CountAsync(match =>
            match.StageId == stageId
            && match.IsFinished
            && match.Round != null
            && match.Round > sanctionRound
            && (match.HomeTeamId == teamValue || match.VisitorTeamId == teamValue));

        return Math.Max(0, sanction.Duration - roundsServed);
    }

    /// <summary>
    /// Determines whether a player currently has any active sanction with fechas still to be served.
    /// </summary>
    public async Task<bool> HasActiveSanctionAsync(Guid playerId)
    {
        IEnumerable<PlayerSanction> sanctions = await _playerSanctionRepository.FindAsync(
            sanction => sanction.PlayerId == playerId
                && sanction.SubjectType == SanctionSubjectType.Player,
            includes: [sanction => sanction.Match]);

        foreach (PlayerSanction sanction in sanctions)
        {
            int? remaining = await GetFechasRemainingAsync(sanction);

            // A sanction whose fechas cannot be computed by rounds (e.g. the
            // originating match has no round) still counts as active while its
            // duration is positive; the day-based sweep cleans it up later.
            bool active = remaining.HasValue ? remaining.Value > 0 : sanction.Duration > 0;
            if (active)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves the team whose rounds a sanction is served against, or none for a staff sanction.
    /// </summary>
    private async Task<Guid?> ResolveSubjectTeamIdAsync(PlayerSanction sanction)
    {
        switch (sanction.SubjectType)
        {
            case SanctionSubjectType.Team:
                return sanction.TeamId;

            case SanctionSubjectType.Player:
                if (sanction.Player is not null)
                {
                    return sanction.Player.TeamId;
                }
                if (sanction.PlayerId.HasValue)
                {
                    Player? player = await _playerRepository.GetByIdAsync(sanction.PlayerId.Value);
                    return player?.TeamId;
                }
                return null;

            case SanctionSubjectType.Staff:
            default:
                return null;
        }
    }

    /// <inheritdoc />
    public async Task<(string? PlayerFullName, string? TeamName, string? StaffName)> ResolveSubjectAsync(PlayerSanction sanction)
    {
        switch (sanction.SubjectType)
        {
            case SanctionSubjectType.Team:
                string? teamName = sanction.Team?.Name;
                if (teamName is null && sanction.TeamId.HasValue)
                {
                    Team? team = await _teamRepository.GetByIdAsync(sanction.TeamId.Value);
                    teamName = team?.Name;
                }
                return (null, teamName, null);

            case SanctionSubjectType.Staff:
                return (null, null, sanction.StaffName);

            // SanctionSubjectType.Player and any future/default value.
            default:
                string? playerFullName = sanction.Player?.FullName;
                if (playerFullName is null && sanction.PlayerId.HasValue)
                {
                    Player? player = await _playerRepository.GetByIdAsync(sanction.PlayerId.Value);
                    playerFullName = player?.FullName;
                }
                return (playerFullName, null, null);
        }
    }

    public async Task<PlayerSanction?> GetPlayerSanctionByIdAsync(Guid playerSanctionId)
    {
        return await _playerSanctionRepository.GetByIdAsync(playerSanctionId);
    }

    /// <summary>
    /// Retrieves a player sanction by id or slug, auto-detecting which one was passed via GUID parsing.
    /// </summary>
    /// <param name="idOrSlug">The sanction's GUID id or its slug.</param>
    /// <returns>The matching player sanction, or null if not found.</returns>
    public async Task<PlayerSanction?> GetPlayerSanctionByIdOrSlugAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out Guid playerSanctionId))
        {
            return await GetPlayerSanctionByIdAsync(playerSanctionId);
        }

        IEnumerable<PlayerSanction> sanctions = await _playerSanctionRepository.FindAsync(
            sanction => sanction.Slug == idOrSlug);

        return sanctions.FirstOrDefault();
    }

    public async Task DeletePlayerSanctionAsync(Guid id)
    {
        await _playerSanctionRepository.RemoveAsync(playerSanction => playerSanction.Id == id);
    }

    public async Task UpdatePlayerSanctionAsync(PlayerSanction playerSanctionEntity)
    {
        await _playerSanctionRepository.UpdateAsync(playerSanctionEntity);
    }

    public async Task<IEnumerable<PlayerSanction>> GetExpiredSanctionsAsync(DateTime date)
    {
        return await _playerSanctionRepository.FindAsync(
                playerSanction => playerSanction.IssuedDate.AddDays(playerSanction.Duration) <= date,
                includes: [playerSanction => playerSanction.Player!]);
    }

    /// <summary>
    /// Retrieves a filtered, paginated list of sanctions with free-text and team matching.
    /// </summary>
    public async Task<PaginatedResponse<PlayerSanction>> GetPlayerSanctionsAsync(GetPlayerSanctionsFilteredRequest filter)
    {
        // Description is suppressed from the auto-generated single-field
        // predicate so the free-text search box can match EITHER the sanction
        // reason OR the sanctioned player's name (Names + LastName). All
        // clauses are case-insensitive partial (Contains) matches, mirroring
        // the auto-generator's own ToLower().Contains() string handling.
        // Accent-insensitivity is intentionally NOT applied: it would require
        // a Postgres extension (e.g. unaccent/citext) that is not enabled in
        // this project, and the SQLite test provider could not translate it.
        Expression<Func<PlayerSanction, bool>> expression = QueryableExtensions.ConstructFilterExpression<PlayerSanction, GetPlayerSanctionsFilteredRequest>(
            filter, nameof(GetPlayerSanctionsFilteredRequest.Description));

        if (!string.IsNullOrWhiteSpace(filter.Description))
        {
            string searchTerm = filter.Description.ToLower();
            expression = expression.And(playerSanction =>
                playerSanction.Description.ToLower().Contains(searchTerm)
                || (playerSanction.Player != null
                    && (playerSanction.Player.FirstName.ToLower().Contains(searchTerm)
                        || (playerSanction.Player.SecondName != null
                            && playerSanction.Player.SecondName.ToLower().Contains(searchTerm))
                        || playerSanction.Player.LastName.ToLower().Contains(searchTerm))));
        }

        if (filter.TournamentId.HasValue)
        {
            expression = expression.And(playerSanction => playerSanction.Match.Stage != null
                && playerSanction.Match.Stage.Division != null
                && playerSanction.Match.Stage.Division.TournamentId == filter.TournamentId.Value);
        }

        if (filter.DivisionId.HasValue)
        {
            expression = expression.And(playerSanction => playerSanction.Match.Stage != null
               && playerSanction.Match.Stage.DivisionId == filter.DivisionId.Value);
        }

        if (filter.StageId.HasValue)
        {
            expression = expression.And(playerSanction => playerSanction.Match.Stage != null
               && playerSanction.Match.Stage.Id == filter.StageId.Value);
        }

        if (filter.TeamId.HasValue)
        {
            // Matches both a player sanction whose player belongs to the team
            // and a team-subject sanction targeting the team directly.
            expression = expression.And(playerSanction =>
                (playerSanction.Player != null && playerSanction.Player.TeamId == filter.TeamId.Value)
                || playerSanction.TeamId == filter.TeamId.Value);
        }

        IEnumerable<PlayerSanction> filteredSanctions = await _playerSanctionRepository.FindAsync(expression,
            filter: filter,
            includes: [playerSanction => playerSanction.Player!, playerSanction => playerSanction.Match]);

        int totalCount = await _playerSanctionRepository.CountAsync(expression);

        return new PaginatedResponse<PlayerSanction>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredSanctions
        };
    }
}
