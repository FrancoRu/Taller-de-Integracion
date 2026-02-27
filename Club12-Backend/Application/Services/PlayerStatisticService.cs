using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.Models;
using System;
using System.Threading.Tasks;

namespace Application.Services;

public class PlayerStatisticService(IPlayerStatisticRepository playerStatisticRepository) : IPlayerStatisticService
{
    public async Task<PlayerStatistic> CreatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity)
    {
        await playerStatisticRepository.AddAsync(playerStatisticEntity);
        return playerStatisticEntity;
    }

    public async Task<PlayerStatistic?> GetPlayerStatisticByIdAsync(Guid playerStatisticId) 
        => await playerStatisticRepository.GetByIdAsync(playerStatisticId);

    public async Task DeletePlayerStatisticAsync(Guid id)
        => await playerStatisticRepository.RemoveAsync(playerStatistic => playerStatistic.Id == id);

    public async Task UpdatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity)
        => await playerStatisticRepository.UpdateAsync(playerStatisticEntity); 
}
