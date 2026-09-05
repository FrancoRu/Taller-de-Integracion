using System;

namespace Application.Utils.Helper.Slug;

/// <summary>
/// Composes the display string a Match's slug is generated from, fed through SlugGenerator.GenerateUniqueSlugAsync.
/// </summary>
public static class MatchSlugSourceBuilder
{
    /// <summary>
    /// Placeholder team name used when a match's home or visitor team is not yet assigned.
    /// </summary>
    public const string UnassignedTeamPlaceholder = "TBD";

    /// <summary>
    /// Builds the slug source string for a match.
    /// </summary>
    /// <param name="homeTeamName">The home team's name, or null/empty when not yet assigned.</param>
    /// <param name="visitorTeamName">The visitor team's name, or null/empty when not yet assigned.</param>
    /// <param name="matchDate">The match's date.</param>
    public static string Build(string? homeTeamName, string? visitorTeamName, DateTime matchDate)
    {
        string home = string.IsNullOrWhiteSpace(homeTeamName) ? UnassignedTeamPlaceholder : homeTeamName;
        string visitor = string.IsNullOrWhiteSpace(visitorTeamName) ? UnassignedTeamPlaceholder : visitorTeamName;

        return $"{home} vs {visitor} {matchDate:yyyy-MM-dd}";
    }
}
