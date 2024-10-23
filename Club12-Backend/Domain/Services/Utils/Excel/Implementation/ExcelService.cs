using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;

namespace Services.Utils.Excel.Implementation;

/// <summary>
/// Provides methods for reading team and player data from Excel files.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExcelService"/> class.
/// </remarks>
/// <param name="logger">The logger to log errors.</param>
public class ExcelService(ILogger<ExcelService> logger) : IExcelService
{
    /// <summary>
    /// Reads the team name, three-letter code, and players from the provided Excel file.
    /// </summary>
    /// <param name="file">The Excel file to read.</param>
    /// <returns>A tuple containing the team name, three-letter code, and a list of players.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an error occurs while processing the Excel file.</exception>
    public async Task<(string TeamName, string ThreeLetterCode, List<(string FirstName, string SecondName, string LastName, string DocumentNumber)> Players)> ReadTeamAndPlayersAsync(IFormFile file)
    {
        try
        {
            using MemoryStream stream = new();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using ExcelPackage package = new(stream);
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

            string teamName = worksheet.Cells[1, 2].Text;
            string threeLetterCode = worksheet.Cells[2, 2].Text;
            List<(string FirstName, string SecondName, string LastName, string DocumentNumber)> players = [];

            for (int row = 5; row <= worksheet.Dimension.End.Row; row++)
            {
                string firstName = worksheet.Cells[row, 1].Text;
                string secondName = worksheet.Cells[row, 2].Text;
                string lastName = worksheet.Cells[row, 3].Text;
                string documentNumber = worksheet.Cells[row, 4].Text;

                if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(secondName) && !string.IsNullOrWhiteSpace(lastName) && !string.IsNullOrWhiteSpace(documentNumber))
                {
                    players.Add((firstName, secondName, lastName, documentNumber));
                }
            }

            return (teamName, threeLetterCode, players);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while reading team and players from the Excel file");
            throw new InvalidOperationException("An error occurred while processing the Excel file.", ex);
        }
    }
}