using Application.DTOs.Abstract.Response;
using Application.DTOs.Player.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;

using Domain.Constants;
using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

public class PlayerService(IUnitOfWork unitOfWork) : IPlayerService
{
    private readonly IPlayerRepository _playerRepository = unitOfWork.PlayerRepository;
    private readonly IPlayerTeamRegistrationRepository _registrationRepository = unitOfWork.PlayerTeamRegistrationRepository;

    public async Task<Player> CreatePlayerAsync(Player playerEntity, Guid tournamentId)
    {
        await _playerRepository.AddAsync(playerEntity);
        await EnsureRegistrationAsync(playerEntity, tournamentId);

        return playerEntity;
    }

    public async Task<Player?> GetPlayerByIdAsync(Guid playerId)
    {
        return await _playerRepository.GetByIdAsync(playerId);
    }

    public async Task DeletePlayerAsync(Guid id)
    {
        await _playerRepository.RemoveAsync(player => player.Id == id);
    }

    public async Task UpdatePlayerAsync(Player playerEntity, Guid tournamentId)
    {
        await _playerRepository.UpdateAsync(playerEntity);
        await EnsureRegistrationAsync(playerEntity, tournamentId);
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
    /// Ensures the player has exactly one PlayerTeamRegistration for
    /// <paramref name="tournamentId"/>, pointing at the player's current
    /// TeamId. Creates one if none exists for that season yet; moves the
    /// existing one to the new team if the player's team changed within the
    /// same season; no-ops otherwise.
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
            registration.TeamId = playerEntity.TeamId;
            registration.DateUpdated = DateTime.UtcNow;
            registration.UpdatedBy = playerEntity.UpdatedBy ?? playerEntity.CreatedBy;
            await _registrationRepository.UpdateAsync(registration);
        }
    }
}
