using Application.DTOs.Abstract.Response;
using Application.DTOs.MatchSeries.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Extensions;
using Application.Utils.Helper.Series;

using Domain.Constants;
using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

using MatchType = Domain.Enums.MatchType;

namespace Application.Services;

/// <summary>
/// Service responsible for managing best-of-N playoff series between two
/// teams at a single bracket round.
/// </summary>
public class MatchSeriesService(IUnitOfWork unitOfWork) : IMatchSeriesService
{
    private readonly IMatchSeriesRepository matchSeriesRepository = unitOfWork.MatchSeriesRepository;
    private readonly IMatchRepository matchRepository = unitOfWork.MatchRepository;
    private readonly IStageRepository stageRepository = unitOfWork.StageRepository;
    private readonly IStageTeamMatchRepository stageTeamMatchRepository = unitOfWork.StageTeamMatchRepository;

    public async Task<MatchSeries?> GetSeriesByIdAsync(Guid seriesId)
    {
        return await matchSeriesRepository.GetByIdAsync(seriesId,
                includes: [s => s.HomeTeam!, s => s.VisitorTeam!, s => s.WinningTeam!, s => s.Matches]);
    }

    public async Task<PaginatedResponse<MatchSeries>> GetAllSeriesAsync(GetMatchSeriesFilteredRequest filter)
    {
        Expression<Func<MatchSeries, bool>> expression = QueryableExtensions.ConstructFilterExpression<MatchSeries, GetMatchSeriesFilteredRequest>(filter);

        IEnumerable<MatchSeries> filteredSeries = await matchSeriesRepository.FindAsync(expression, filter: filter,
            includes: [s => s.HomeTeam!, s => s.VisitorTeam!, s => s.WinningTeam!, s => s.Matches]);

        int totalCount = await matchSeriesRepository.CountAsync(expression);

        return new PaginatedResponse<MatchSeries>
        {
            Page = filter.PageNumber,
            PageSize = filter.PageSize,
            TotalCount = totalCount,
            Items = filteredSeries
        };
    }

    public async Task<MatchSeries> CreateSeriesAsync(Guid stageId, Guid homeTeamId, Guid visitorTeamId)
    {
        if (homeTeamId == visitorTeamId)
        {
            throw new InvalidOperationException(ErrorMessages.MatchSeries.RequiresTwoDifferentTeams);
        }

        Stage stage = await stageRepository.GetByIdAsync(stageId)
            ?? throw new InvalidOperationException(ErrorMessages.Stage.NotFoundGeneric);

        await EnsureTeamAssignedToStageAsync(stageId, homeTeamId);
        await EnsureTeamAssignedToStageAsync(stageId, visitorTeamId);

        bool alreadyExists = await matchSeriesRepository.ExistsAsync(series =>
            series.StageId == stageId
            && ((series.HomeTeamId == homeTeamId && series.VisitorTeamId == visitorTeamId)
                || (series.HomeTeamId == visitorTeamId && series.VisitorTeamId == homeTeamId)));

        if (alreadyExists)
        {
            throw new InvalidOperationException(ErrorMessages.MatchSeries.AlreadyExistsForStage);
        }

        MatchSeries seriesEntity = new()
        {
            StageId = stageId,
            HomeTeamId = homeTeamId,
            VisitorTeamId = visitorTeamId,
            BestOf = stage.BestOf,
            CreatedBy = AuditConstants.SystemUser,
        };

        await matchSeriesRepository.AddAsync(seriesEntity);
        return seriesEntity;
    }

    public async Task<Match> AddGameToSeriesAsync(Guid seriesId, DateTime matchDate, Guid? venueId)
    {
        MatchSeries series = await matchSeriesRepository.GetByIdAsync(seriesId, includes: [s => s.Matches])
            ?? throw new InvalidOperationException(ErrorMessages.MatchSeries.NotFound);

        if (series.WinningTeamId.HasValue)
        {
            throw new InvalidOperationException(ErrorMessages.MatchSeries.AlreadyDecided);
        }

        if (series.Matches.Count >= series.BestOf)
        {
            throw new InvalidOperationException(ErrorMessages.MatchSeries.MaxGamesReached(series.BestOf));
        }

        Match game = new()
        {
            StageId = series.StageId,
            HomeTeamId = series.HomeTeamId,
            VisitorTeamId = series.VisitorTeamId,
            SeriesId = series.Id,
            GameNumber = series.Matches.Count + 1,
            MatchDate = matchDate,
            VenueId = venueId,
            Type = MatchType.Playoff,
            IsFinished = false,
            CreatedBy = AuditConstants.SystemUser,
        };

        await matchRepository.AddAsync(game);
        return game;
    }

    public async Task RecalculateSeriesWinnerAsync(Guid seriesId)
    {
        MatchSeries? series = await matchSeriesRepository.GetByIdAsync(seriesId, includes: [s => s.Matches]);

        if (series is null || series.WinningTeamId.HasValue)
        {
            return;
        }

        Guid? winningTeamId = SeriesDecisionCalculator.DetermineWinner(series);

        if (winningTeamId.HasValue)
        {
            series.WinningTeamId = winningTeamId;
            await matchSeriesRepository.UpdateAsync(series);
        }
    }

    private async Task EnsureTeamAssignedToStageAsync(Guid stageId, Guid teamId)
    {
        bool isAssigned = await stageTeamMatchRepository.ExistsAsync(stm =>
            stm.StageId == stageId && stm.TeamId == teamId);

        if (!isAssigned)
        {
            throw new InvalidOperationException(ErrorMessages.MatchSeries.TeamNotAssignedToStage(teamId));
        }
    }
}
