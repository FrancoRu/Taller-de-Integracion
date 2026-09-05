using Application.DTOs.Abstract.Response;
using Application.DTOs.Scorer.Response;
using Application.Interfaces.Mappers;
using Application.Utils.Constants.Scorer;

using Domain.Entities.Models;

using Riok.Mapperly.Abstractions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Utils.Mappers;


[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public partial class ScorerMapper : IScorerMapper
{
    /// <summary>
    /// Collapses one row per scoring event into a single row per player, summing their points across the paginated page.
    /// </summary>
    public PaginatedResponse<ScorerByPlayerResponse> FromPaginatedScorerToPaginatedScorerByPlayerResponse(PaginatedResponse<Scorer> paginatedScorers)
    {
        List<ScorerByPlayerResponse> groupedItems = [.. paginatedScorers.Items
            .GroupBy(scorer => scorer.PlayerId)
            .Select(group => new ScorerByPlayerResponse
            {
                PlayerId = group.Key,
                Points = group.Sum(scorer => scorer.Points),
                FullName = group
                    .Select(scorer => scorer.Player?.FullName)
                    .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? string.Empty
            })
            .OrderByDescending(item => item.Points)];

        return new PaginatedResponse<ScorerByPlayerResponse>
        {
            Items = groupedItems,
            Page = paginatedScorers.Page,
            PageSize = paginatedScorers.PageSize,
            TotalCount = groupedItems.Count
        };
    }

    /// <summary>
    /// Computes each team's league points across the given matches using ScoreConstants for win, draw and loss, skipping matches that aren't finished yet or don't have both teams assigned.
    /// </summary>
    public PaginatedResponse<ScorerByTeamResponse> FromPaginatedMatchToPaginatedScorerByTeamResponse(PaginatedResponse<Match> paginatedMatches)
    {
        Dictionary<Guid, ScorerByTeamResponse> scoreboard = [];

        foreach (Match match in paginatedMatches.Items)
        {
            if (match.HomeTeamId is null || match.VisitorTeamId is null)
            {
                continue;
            }

            Guid homeTeamId = match.HomeTeamId.Value;
            Guid visitorTeamId = match.VisitorTeamId.Value;

            string homeTeamName = match.HomeTeam?.Name ?? string.Empty;
            string visitorTeamName = match.VisitorTeam?.Name ?? string.Empty;

            if (!scoreboard.TryGetValue(homeTeamId, out ScorerByTeamResponse? homeEntry))
            {
                homeEntry = new ScorerByTeamResponse
                {
                    TeamId = homeTeamId,
                    Name = homeTeamName,
                    Points = 0
                };
                scoreboard[homeTeamId] = homeEntry;
            }

            if (!scoreboard.TryGetValue(visitorTeamId, out ScorerByTeamResponse? visitorEntry))
            {
                visitorEntry = new ScorerByTeamResponse
                {
                    TeamId = visitorTeamId,
                    Name = visitorTeamName,
                    Points = 0
                };
                scoreboard[visitorTeamId] = visitorEntry;
            }

            if (!match.IsFinished)
            {
                continue;
            }

            bool isDraw = match.HomeScore == match.VisitorScore;
            bool homeWon = match.HomeScore > match.VisitorScore;

            if (isDraw)
            {
                homeEntry.Points += ScoreConstants.PointsForDraw;
                visitorEntry.Points += ScoreConstants.PointsForDraw;
                continue;
            }

            if (homeWon)
            {
                homeEntry.Points += ScoreConstants.PointsForWin;
                visitorEntry.Points += ScoreConstants.PointsForLoss;
            }
            else
            {
                homeEntry.Points += ScoreConstants.PointsForLoss;
                visitorEntry.Points += ScoreConstants.PointsForWin;
            }
        }

        List<ScorerByTeamResponse> items = [.. scoreboard.Values.OrderByDescending(x => x.Points)];

        return new PaginatedResponse<ScorerByTeamResponse>
        {
            Items = items,
            Page = paginatedMatches.Page,
            PageSize = paginatedMatches.PageSize,
            TotalCount = items.Count
        };
    }

    public ScorerByPlayerResponse FromScorerToScorerByPlayerResponse(Scorer scorer)
    {
        throw new System.NotImplementedException();
    }

    public ScorerByTeamResponse FromScorerToScorerByTeamResponse(Scorer scorer)
    {
        throw new System.NotImplementedException();
    }
}
