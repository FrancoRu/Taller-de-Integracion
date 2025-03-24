using Entities.Models.PlayoffSeriesEntity;
using Entities.Models.RoundNameEnum;

using Microsoft.EntityFrameworkCore;

using Services.DataAccessLayer.GenericEntity;

namespace Services.Services.PlayoffService.Implementation;

/// <summary>
/// Implementation of the <see cref="IPlayoffSeriesService"/> interface.
/// </summary>
/// <param name="_genericPlayoffSeriesService">The generic service to handle playoff series data.</param>
public class PlayoffSeriesService(IGenericService<PlayoffSeries> _genericPlayoffSeriesService) : IPlayoffSeriesService
{
    /// <inheritdoc />
    public async Task<List<PlayoffSeries>> CreatePlayoffSeriesAsync()
    {
        List<PlayoffSeries> playoffSeries =
        [
            new() {
                RoundName = RoundName.Quarterfinal,
                IsFinished = false,
                GamesRequiredToWin = 2,
                HomeTeamWins = 0,
                VisitorTeamWins = 0
            },
            new PlayoffSeries
            {
                RoundName = RoundName.Semifinal,
                IsFinished = false,
                GamesRequiredToWin = 2,
                HomeTeamWins = 0,
                VisitorTeamWins = 0
            },
            new PlayoffSeries
            {
                RoundName = RoundName.Final,
                IsFinished = false,
                GamesRequiredToWin = 2,
                HomeTeamWins = 0,
                VisitorTeamWins = 0
            }
        ];

        // Link the series (Quarterfinal -> Semifinal -> Final)
        playoffSeries[0].NextSeries = playoffSeries[1]; // Quarterfinal -> Semifinal
        playoffSeries[1].NextSeries = playoffSeries[2]; // Semifinal -> Final

        await _genericPlayoffSeriesService.InsertRangeAsync(playoffSeries);

        return playoffSeries;
    }

    /// <inheritdoc />
    public async Task<PlayoffSeries?> GetSeriesByIdAsync(Guid id) => await _genericPlayoffSeriesService
        .FilterByExpression(series => series.Id == id)
        .Include(series => series.Matches)
            .ThenInclude(match => match.HomeTeam)
        .Include(series => series.Matches)
            .ThenInclude(match => match.VisitorTeam)
        .FirstOrDefaultAsync();

    /// <inheritdoc />
    public async Task<bool> UpdateSeriesAsync(PlayoffSeries series)
    {
        try
        {
            await _genericPlayoffSeriesService.UpdateAsync(series);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSeriesAsync(PlayoffSeries series)
    {
        try
        {
            await _genericPlayoffSeriesService.DeleteAsync(series);
            return true;
        }
        catch
        {
            return false;
        }
    }
}