using Microsoft.AspNetCore.Http;

namespace Services.Utils.Excel;

/// <summary>
/// Interface for Excel services.
/// </summary>
public interface IExcelService
{
    /// <summary>
    /// Reads the Excel file and extracts the team and player data.
    /// </summary>
    /// <param name="file">The Excel file containing team and player information.</param>
    /// <returns>A tuple containing team data as strings and a list of player data as strings.</returns>
    Task<(string TeamName, string ThreeLetterCode, List<(string FirstName, string LastName, string DocumentNumber)> Players)> ReadTeamAndPlayersAsync(IFormFile file);
}
