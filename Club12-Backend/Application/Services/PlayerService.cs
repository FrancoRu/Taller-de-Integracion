using Application.DTOs.Abstract.Response;
using Application.DTOs.Player.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Extensions;
using Application.Utils.Helper.Slug;
using Application.Utils.Options;

using Domain.Constants;
using Domain.Entities.Models;

using Microsoft.Extensions.Options;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Manages players and keeps Player.TeamId in sync with their season-scoped PlayerTeamRegistration.
/// </summary>
public class PlayerService(
    IUnitOfWork unitOfWork,
    IScorerRepository scorerRepository,
    IOptions<RosterOptions> rosterOptions) : IPlayerService
{
    private readonly IPlayerRepository _playerRepository = unitOfWork.PlayerRepository;
    private readonly IPlayerTeamRegistrationRepository _registrationRepository = unitOfWork.PlayerTeamRegistrationRepository;
    private readonly IPlayerStatisticRepository _statisticRepository = unitOfWork.PlayerStatisticRepository;
    private readonly IPlayerSanctionRepository _sanctionRepository = unitOfWork.PlayerSanctionRepository;
    private readonly IScorerRepository _scorerRepository = scorerRepository;
    private readonly int _maxPlayersPerTeam = rosterOptions.Value.MaxPlayersPerTeam;

    public async Task<Player> CreatePlayerAsync(Player playerEntity, Guid tournamentId)
    {
        await EnsureDocumentNumberIsUniqueAsync(playerEntity.DocumentNumber, excludingPlayerId: null);

        playerEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            playerEntity.SlugSource,
            candidate => _playerRepository.ExistsAsync(player => player.Slug == candidate));

        await _playerRepository.AddAsync(playerEntity);
        await EnsureRegistrationAsync(playerEntity, tournamentId);

        return playerEntity;
    }

    /// <summary>
    /// Guards the DB's IX_Players_DocumentNumber unique index with a friendly 409 instead of a raw 500.
    /// </summary>
    private async Task EnsureDocumentNumberIsUniqueAsync(string documentNumber, Guid? excludingPlayerId)
    {
        bool taken = await _playerRepository.ExistsAsync(player =>
            player.DocumentNumber == documentNumber
            && (excludingPlayerId == null || player.Id != excludingPlayerId));

        if (taken)
        {
            throw new InvalidOperationException(
                ErrorMessages.Player.DuplicateDocumentNumber(documentNumber));
        }
    }

    public async Task<Player?> GetPlayerByIdAsync(Guid playerId)
    {
        return await _playerRepository.GetByIdAsync(playerId);
    }

    /// <summary>
    /// Retrieves a player by id or slug, auto-detecting which one was passed via GUID parsing.
    /// </summary>
    /// <param name="idOrSlug">The player's GUID id or its slug.</param>
    /// <returns>The matching player, or null if not found.</returns>
    public async Task<Player?> GetPlayerByIdOrSlugAsync(string idOrSlug)
    {
        if (Guid.TryParse(idOrSlug, out Guid playerId))
        {
            return await GetPlayerByIdAsync(playerId);
        }

        IEnumerable<Player> matches = await _playerRepository.FindAsync(player => player.Slug == idOrSlug);
        return matches.FirstOrDefault();
    }

    public async Task DeletePlayerAsync(Guid id)
    {
        // Integrity: a player who already has match statistics, scorer records
        // or sanctions is part of the tournament history — deleting them would
        // orphan those records, so the deletion is blocked. Otherwise the
        // player's season registrations are cleaned up and the player removed.
        bool hasStatistics = await _statisticRepository.ExistsAsync(statistic => statistic.PlayerId == id);
        bool hasScorers = await _scorerRepository.ExistsAsync(scorer => scorer.PlayerId == id);
        bool hasSanctions = await _sanctionRepository.ExistsAsync(sanction => sanction.PlayerId == id);

        if (hasStatistics || hasScorers || hasSanctions)
        {
            throw new InvalidOperationException(ErrorMessages.Player.HasHistoryCannotDelete);
        }

        await _registrationRepository.RemoveAsync(registration => registration.PlayerId == id);
        await _playerRepository.RemoveAsync(player => player.Id == id);
    }

    /// <inheritdoc />
    public async Task<PlayerTeamRegistration> RegisterPlayerToTeamAsync(
        Guid playerId, Guid teamId, Guid tournamentId, int? jerseyNumber = null)
    {
        IEnumerable<PlayerTeamRegistration> existing = await _registrationRepository.FindAsync(
            registration => registration.PlayerId == playerId && registration.TournamentId == tournamentId);
        PlayerTeamRegistration? registration = existing.FirstOrDefault();

        // A player cannot be registered to two teams in the same tournament.
        if (registration is not null && registration.TeamId != teamId)
        {
            throw new InvalidOperationException(
                ErrorMessages.Roster.PlayerAlreadyInAnotherTeam(playerId, tournamentId));
        }

        // Dorsal must be unique within the same team + tournament
        // (ignoring this same player's own current registration).
        if (jerseyNumber is not null)
        {
            bool dorsalTaken = await _registrationRepository.ExistsAsync(candidate =>
                candidate.TeamId == teamId
                && candidate.TournamentId == tournamentId
                && candidate.PlayerId != playerId
                && candidate.JerseyNumber == jerseyNumber);

            if (dorsalTaken)
            {
                throw new InvalidOperationException(
                    ErrorMessages.Roster.DuplicateJerseyNumber(jerseyNumber.Value, teamId, tournamentId));
            }
        }

        if (registration is null)
        {
            // Enforce the configurable roster-size cap when adding a
            // brand-new member (re-registering an existing member does not grow
            // the roster, so it skips this check).
            int currentRosterSize = await _registrationRepository.CountAsync(
                candidate => candidate.TeamId == teamId && candidate.TournamentId == tournamentId);

            if (currentRosterSize >= _maxPlayersPerTeam)
            {
                throw new InvalidOperationException(
                    ErrorMessages.Roster.RosterFull(teamId, _maxPlayersPerTeam));
            }

            PlayerTeamRegistration created = new()
            {
                Id = Guid.Empty,
                PlayerId = playerId,
                TeamId = teamId,
                TournamentId = tournamentId,
                JerseyNumber = jerseyNumber,
                DateCreated = DateTime.UtcNow,
                CreatedBy = AuditConstants.SystemUser,
            };

            await _registrationRepository.AddAsync(created);
            return created;
        }

        // Same player, same team: keep the dorsal in sync (idempotent add/edit).
        registration.JerseyNumber = jerseyNumber;
        registration.DateUpdated = DateTime.UtcNow;
        await _registrationRepository.UpdateAsync(registration);
        return registration;
    }

    public async Task UpdatePlayerAsync(Player playerEntity, Guid tournamentId)
    {
        // Validate the roster move BEFORE persisting the Player row: this
        // repository commits immediately (no shared transaction), so if the
        // Player.TeamId write landed first and the registration move then
        // threw (e.g. destination roster full), Player.TeamId would point at
        // a team the player was never actually validly registered to for
        // this season — a silent inconsistency between the "current team"
        // pointer and the season-scoped source of truth.
        await EnsureDocumentNumberIsUniqueAsync(playerEntity.DocumentNumber, excludingPlayerId: playerEntity.Id);
        await ValidateRegistrationMoveAsync(playerEntity, tournamentId);
        await _playerRepository.UpdateAsync(playerEntity);
        await EnsureRegistrationAsync(playerEntity, tournamentId);
    }

    /// <summary>
    /// Mirrors RegisterPlayerToTeamAsync's roster-size cap for a mid-season team change.
    /// </summary>
    private async Task ValidateRegistrationMoveAsync(Player playerEntity, Guid tournamentId)
    {
        PlayerTeamRegistration? registration = (await _registrationRepository.FindAsync(
            r => r.PlayerId == playerEntity.Id && r.TournamentId == tournamentId)).FirstOrDefault();

        if (registration is null || registration.TeamId == playerEntity.TeamId)
        {
            return;
        }

        bool hasStatistics = await _statisticRepository.ExistsAsync(statistic => statistic.PlayerId == playerEntity.Id);
        bool hasScorers = await _scorerRepository.ExistsAsync(scorer => scorer.PlayerId == playerEntity.Id);
        bool hasSanctions = await _sanctionRepository.ExistsAsync(sanction => sanction.PlayerId == playerEntity.Id);

        if (hasStatistics || hasScorers || hasSanctions)
        {
            throw new InvalidOperationException(ErrorMessages.Player.CannotMoveTeamWithHistory);
        }

        int destinationRosterSize = await _registrationRepository.CountAsync(
            candidate => candidate.TeamId == playerEntity.TeamId
                && candidate.TournamentId == tournamentId
                && candidate.PlayerId != playerEntity.Id);

        if (destinationRosterSize >= _maxPlayersPerTeam)
        {
            throw new InvalidOperationException(
                ErrorMessages.Roster.RosterFull(playerEntity.TeamId, _maxPlayersPerTeam));
        }
    }

    public async Task<PaginatedResponse<Player>> GetAllPlayersAsync(PlayerFilterRequestBase filter)
    {
        Expression<Func<Player, bool>> expression = QueryableExtensions.ConstructFilterExpression<Player, PlayerFilterRequestBase>(filter);
        IEnumerable<Player> filteredPlayers = await _playerRepository.FindAsync(expression, filter: filter);

        int totalCount = await _playerRepository.CountAsync(expression);

        return new PaginatedResponse<Player>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredPlayers
        };
    }

    /// <summary>
    /// Ensures the player has exactly one PlayerTeamRegistration for tournamentId pointing at their current TeamId.
    /// </summary>
    private async Task EnsureRegistrationAsync(Player playerEntity, Guid tournamentId)
    {
        IEnumerable<PlayerTeamRegistration> existing = await _registrationRepository.FindAsync(
            registration => registration.PlayerId == playerEntity.Id && registration.TournamentId == tournamentId);

        PlayerTeamRegistration? registration = existing.FirstOrDefault();

        if (registration is null)
        {
            await _registrationRepository.AddAsync(new PlayerTeamRegistration
            {
                Id = Guid.Empty,
                PlayerId = playerEntity.Id,
                TeamId = playerEntity.TeamId,
                TournamentId = tournamentId,
                DateCreated = DateTime.UtcNow,
                CreatedBy = playerEntity.UpdatedBy ?? playerEntity.CreatedBy ?? AuditConstants.SystemUser,
            });

            return;
        }

        if (registration.TeamId != playerEntity.TeamId)
        {
            // Capacity was already validated by ValidateRegistrationMoveAsync
            // before the Player row was persisted.
            registration.TeamId = playerEntity.TeamId;

            // The dorsal is unique within (TeamId, TournamentId, JerseyNumber) —
            // carrying it over to the new team could collide with a player
            // already wearing it there and throw a raw DB constraint violation
            // instead of a friendly error. Reset it; the admin re-assigns a
            // dorsal on the new team explicitly after the move.
            registration.JerseyNumber = null;

            registration.DateUpdated = DateTime.UtcNow;
            registration.UpdatedBy = playerEntity.UpdatedBy ?? playerEntity.CreatedBy;
            await _registrationRepository.UpdateAsync(registration);
        }
    }
}
