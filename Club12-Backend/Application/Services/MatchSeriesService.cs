using Application.DTOs.Abstract.Response;
using Application.DTOs.MatchSeries.Request;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Utils.Constants;
using Application.Utils.Extensions;
using Application.Utils.Helper.Series;
using Application.Utils.Helper.Slug;

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
    private readonly IMatchSeriesRepository _matchSeriesRepository = unitOfWork.MatchSeriesRepository;
    private readonly IMatchRepository _matchRepository = unitOfWork.MatchRepository;
    private readonly IStageRepository _stageRepository = unitOfWork.StageRepository;
    private readonly IStageTeamMatchRepository _stageTeamMatchRepository = unitOfWork.StageTeamMatchRepository;
    private readonly ITeamRepository _teamRepository = unitOfWork.TeamRepository;

    public async Task<MatchSeries?> GetSeriesByIdAsync(Guid seriesId)
    {
        return await _matchSeriesRepository.GetByIdAsync(seriesId,
                includes: [s => s.HomeTeam!, s => s.VisitorTeam!, s => s.WinningTeam!, s => s.Matches]);
    }

    public async Task<PaginatedResponse<MatchSeries>> GetAllSeriesAsync(GetMatchSeriesFilteredRequest filter)
    {
        Expression<Func<MatchSeries, bool>> expression = QueryableExtensions.ConstructFilterExpression<MatchSeries, GetMatchSeriesFilteredRequest>(filter);

        IEnumerable<MatchSeries> filteredSeries = await _matchSeriesRepository.FindAsync(expression, filter: filter,
            includes: [s => s.HomeTeam!, s => s.VisitorTeam!, s => s.WinningTeam!, s => s.Matches]);

        int totalCount = await _matchSeriesRepository.CountAsync(expression);

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

        Stage stage = await _stageRepository.GetByIdAsync(stageId)
            ?? throw new InvalidOperationException(ErrorMessages.Stage.NotFoundGeneric);

        await EnsureTeamAssignedToStageAsync(stageId, homeTeamId);
        await EnsureTeamAssignedToStageAsync(stageId, visitorTeamId);

        bool alreadyExists = await _matchSeriesRepository.ExistsAsync(series =>
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

        await _matchSeriesRepository.AddAsync(seriesEntity);
        return seriesEntity;
    }

    public async Task<Match> AddGameToSeriesAsync(Guid seriesId, DateTime matchDate, Guid? venueId)
    {
        MatchSeries series = await _matchSeriesRepository.GetByIdAsync(seriesId, includes: [s => s.Matches])
            ?? throw new InvalidOperationException(ErrorMessages.MatchSeries.NotFound);

        if (series.WinningTeamId.HasValue)
        {
            throw new InvalidOperationException(ErrorMessages.MatchSeries.AlreadyDecided);
        }

        if (series.Matches.Count >= series.BestOf)
        {
            throw new InvalidOperationException(ErrorMessages.MatchSeries.MaxGamesReached(series.BestOf));
        }

        Team? homeTeam = await _teamRepository.GetByIdAsync(series.HomeTeamId);
        Team? visitorTeam = await _teamRepository.GetByIdAsync(series.VisitorTeamId);

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
            Slug = string.Empty,
            IsFinished = false,
            CreatedBy = AuditConstants.SystemUser,
        };

        game.Slug = await SlugGenerator.GenerateUniqueSlugAsync(
            MatchSlugSourceBuilder.Build(homeTeam?.Name, visitorTeam?.Name, game.MatchDate),
            candidate => _matchRepository.ExistsAsync(match => match.Slug == candidate));

        await _matchRepository.AddAsync(game);
        return game;
    }

    public async Task RecalculateSeriesWinnerAsync(Guid seriesId)
    {
        MatchSeries? series = await _matchSeriesRepository.GetByIdAsync(seriesId, includes: [s => s.Matches]);

        if (series is null || series.WinningTeamId.HasValue)
        {
            return;
        }

        Guid? winningTeamId = SeriesDecisionCalculator.DetermineWinner(series);

        if (winningTeamId.HasValue)
        {
            series.WinningTeamId = winningTeamId;
            await _matchSeriesRepository.UpdateAsync(series);
        }
    }

    private async Task EnsureTeamAssignedToStageAsync(Guid stageId, Guid teamId)
    {
        bool isAssigned = await _stageTeamMatchRepository.ExistsAsync(stm =>
            stm.StageId == stageId && stm.TeamId == teamId);

        if (!isAssigned)
        {
            throw new InvalidOperationException(ErrorMessages.MatchSeries.TeamNotAssignedToStage(teamId));
        }
    }
}
