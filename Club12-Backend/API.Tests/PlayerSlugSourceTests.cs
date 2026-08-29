using Domain.Entities.Models;

namespace API.Tests;

/// <summary>
/// Locks the single canonical slug-source rule shared by create, seed and the
/// re-backfill migration: <see cref="Player.BuildSlugSource"/> joins the raw-case
/// names as <c>apellido nombre[ segundo]</c> with no document number, while
/// <see cref="Player.FullName"/> keeps its historical display output
/// (last name upper-cased) byte-for-byte.
/// </summary>
public class PlayerSlugSourceTests
{
    [Fact]
    public void BuildSlugSource_WithoutSecondName_JoinsLastThenFirst()
    {
        string source = Player.BuildSlugSource("Lopez", "Carlos", null);

        Assert.Equal("Lopez Carlos", source);
    }

    [Fact]
    public void BuildSlugSource_WithSecondName_AppendsSecondName()
    {
        string source = Player.BuildSlugSource("Lopez", "Carlos", "Maria");

        Assert.Equal("Lopez Carlos Maria", source);
    }

    [Fact]
    public void BuildSlugSource_WhitespaceSecondName_TreatedAsAbsent()
    {
        string source = Player.BuildSlugSource("Lopez", "Carlos", "   ");

        Assert.Equal("Lopez Carlos", source);
    }

    [Fact]
    public void BuildSlugSource_PreservesRawCase_NoUpperCasing()
    {
        string source = Player.BuildSlugSource("lopez", "carlos", null);

        Assert.Equal("lopez carlos", source);
    }

    [Fact]
    public void SlugSource_UsesRawCaseNamesWithoutDocumentNumber()
    {
        Player player = BuildPlayer("Lopez", "Carlos", secondName: null, documentNumber: "30000001");

        Assert.Equal("Lopez Carlos", player.SlugSource);
    }

    [Fact]
    public void SlugSource_WithSecondName_IncludesIt()
    {
        Player player = BuildPlayer("Lopez", "Carlos", secondName: "Maria", documentNumber: "30000001");

        Assert.Equal("Lopez Carlos Maria", player.SlugSource);
    }

    [Fact]
    public void FullName_WithoutSecondName_UpperCasesLastNameAndMatchesLegacyFormat()
    {
        Player player = BuildPlayer("Lopez", "Carlos", secondName: null, documentNumber: "30000001");

        string expected = string.Concat("Lopez".ToUpper(), " Carlos");
        Assert.Equal(expected, player.FullName);
        Assert.Equal("LOPEZ Carlos", player.FullName);
    }

    [Fact]
    public void FullName_WithSecondName_MatchesLegacyFormat()
    {
        Player player = BuildPlayer("Lopez", "Carlos", secondName: "Maria", documentNumber: "30000001");

        string expected = string.Concat("Lopez".ToUpper(), " Carlos Maria");
        Assert.Equal(expected, player.FullName);
    }

    [Fact]
    public void FullName_WhitespaceSecondName_TreatedAsAbsentLikeLegacy()
    {
        Player player = BuildPlayer("Lopez", "Carlos", secondName: "  ", documentNumber: "30000001");

        Assert.Equal(string.Concat("Lopez".ToUpper(), " Carlos"), player.FullName);
    }

    private static Player BuildPlayer(string lastName, string firstName, string? secondName, string documentNumber)
    {
        return new Player
        {
            FirstName = firstName,
            SecondName = secondName,
            LastName = lastName,
            Slug = "placeholder",
            DocumentNumber = documentNumber,
            IsSanctioned = false,
            BirthDate = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            SocialSecurity = "OSDE",
            Team = null!,
            CreatedBy = "test",
        };
    }
}
