using Entities.Models.MatchEntity;
using Entities.Models.PlayerEntity;
using Entities.Models.PlayerStatisticEntity;
using Entities.Models.TopScorerModel;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

using Services.DataAccessLayer.GenericEntity;
using Services.Services.PlayerStatisticService;

namespace Club12.Services.Services.PlayerStatisticService.Implementation;

public class PlayerStatisticService(IGenericService<PlayerStatistic> genericPlayerStatisticService) : IPlayerStatisticService
{
    public PlayerStatistic CreatePlayerStatistic(PlayerStatistic playerStatisticEntity)
    {
        genericPlayerStatisticService.Insert(playerStatisticEntity);
        return playerStatisticEntity;
    }

    public PlayerStatistic? GetPlayerStatisticById(Guid playerStatisticId)
    {
        return genericPlayerStatisticService.TryGet(playerStatisticId);
    }

    public void DeletePlayerStatistic(PlayerStatistic playerStatisticEntity)
    {
        genericPlayerStatisticService.Delete(playerStatisticEntity);
    }

    public async Task<bool> UpdatePlayerStatisticAsync(PlayerStatistic playerStatisticEntity)
    {
        try
        {
            await genericPlayerStatisticService.UpdateAsync(playerStatisticEntity);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public List<TopScorer> GetTopScorersByDivision(IEnumerable<Guid> matchIds, int totalMatches)
    {
        IIncludableQueryable<PlayerStatistic, Match> playerStatistics = genericPlayerStatisticService.FilterByExpression(stat => matchIds.Contains(stat.MatchId))
                                                                                                     .Include(playerStatistic => playerStatistic.Player)
                                                                                                     .Include(playerStatistic => playerStatistic.Match);

        List<PlayerStatistic> playerStatsList = [.. playerStatistics];

        List<TopScorer> topScorers = playerStatsList
            .GroupBy(stat => stat.PlayerId)
            .Select(group =>
            {
                PlayerStatistic? firstStat = group.FirstOrDefault();
                Player? player = firstStat?.Player;

                Dictionary<int, int> weeklyScores = Enumerable.Range(1, totalMatches)
                                             .ToDictionary(week => week, week => 0);

                foreach (PlayerStatistic? stat in group)
                {
                    if (stat.Match?.IsFinished == true)
                    {
                        int matchWeek = stat.Match?.MatchWeek ?? 0;
                        if (matchWeek > 0 && matchWeek <= totalMatches)
                        {
                            weeklyScores[matchWeek] = stat.Value;
                        }
                    }
                }

                return new TopScorer
                {
                    PlayerId = group.Key,
                    FirstName = player?.Names ?? "Unknown",
                    LastName = player?.LastName ?? "Unknown",
                    WeeklyScores = weeklyScores
                };
            })
            .OrderByDescending(x => x.TotalPoints)
            .Take(10)
            .ToList();

        return topScorers;
    }
}
