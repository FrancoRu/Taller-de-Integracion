using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

namespace Services.Utils.Excel.Implementation;

public class ExcelService : IExcelService
{
    public async Task<(string TeamName, string ThreeLetterCode, List<(string FirstName, string LastName, string DocumentNumber)> Players)> ReadTeamAndPlayersAsync(IFormFile file)
    {
        using MemoryStream stream = new();
        await file.CopyToAsync(stream);
        using ExcelPackage package = new(stream);
        ExcelWorksheet worksheet = package.Workbook.Worksheets.First();

        string teamName = worksheet.Cells[1, 1].Text;
        string threeLetterCode = worksheet.Cells[2, 1].Text;
        List<(string FirstName, string LastName, string DocumentNumber)> players = [];

        for (int row = 4; row <= worksheet.Dimension.End.Row; row++)
        {
            string firstName = worksheet.Cells[row, 1].Text;
            string lastName = worksheet.Cells[row, 2].Text;
            string documentNumber = worksheet.Cells[row, 3].Text;

            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName) && !string.IsNullOrWhiteSpace(documentNumber))
            {
                players.Add((firstName, lastName, documentNumber));
            }
        }

        return (teamName, threeLetterCode, players);
    }
}