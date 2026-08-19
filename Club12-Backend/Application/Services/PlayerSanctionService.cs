using Application.DTOs.Abstract.Response;
using Application.DTOs.PlayerSanction.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Extensions;
using Application.Utils.Helper.Slug;

using Domain.Entities.Models;

using LinqKit;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Application.Services;

public class PlayerSanctionService(IUnitOfWork unitOfWork) : IPlayerSanctionService
{
    private readonly IPlayerSanctionRepository _playerSanctionRepository = unitOfWork.PlayerSanctionRepository;
    private readonly IPlayerRepository _playerRepository = unitOfWork.PlayerRepository;

    public async Task<PlayerSanction> CreatePlayerSanctionAsync(PlayerSanction playerSanctionEntity)
    {
        Player? player = await _playerRepository.GetByIdAsync(playerSanctionEntity.PlayerId);
        string playerName = player?.FullName ?? "jugador";

        playerSanctionEntity.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            $"{playerName} {playerSanctionEntity.IssuedDate:yyyy-MM-dd}",
            candidate => _playerSanctionRepository.ExistsAsync(sanction => sanction.Slug == candidate));

        await _playerSanctionRepository.AddAsync(playerSanctionEntity);
        return playerSanctionEntity;
    }

    public async Task<PlayerSanction?> GetPlayerSanctionByIdAsync(Guid playerSanctionId)
    {
        return await _playerSanctionRepository.GetByIdAsync(playerSanctionId);
    }

    /// <summary>
    /// Retrieves a player sanction by its id or its slug. The value is
    /// treated as an id when it parses as a GUID, otherwise it is looked up
    /// as a slug.
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
                includes: [playerSanction => playerSanction.Player]);
    }

    public async Task<PaginatedResponse<PlayerSanction>> GetPlayerSanctionsAsync(GetPlayerSanctionsFilteredRequest filter)
    {
        Expression<Func<PlayerSanction, bool>> expression = QueryableExtensions.ConstructFilterExpression<PlayerSanction, GetPlayerSanctionsFilteredRequest>(filter);

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
            expression = expression.And(playerSanction => playerSanction.Player.TeamId == filter.TeamId.Value);
        }

        IEnumerable<PlayerSanction> filteredSanctions = await _playerSanctionRepository.FindAsync(expression,
            filter: filter,
            includes: [playerSanction => playerSanction.Player, playerSanction => playerSanction.Match]);

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
