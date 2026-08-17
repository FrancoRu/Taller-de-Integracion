using System;

namespace Application.Utils.Helper.Slug;

/// <summary>
/// Composes the display string a Match's slug is generated from —
/// "{homeTeamName} vs {visitorTeamName} {date:yyyy-MM-dd}" — fed through
/// <see cref="SlugGenerator.GenerateUniqueSlugAsync"/>. Shared between
/// MatchService (manual and bulk/automated match creation) and
/// MatchSeriesService (series game creation), so both retire their own copy
/// of this format and the unassigned-team placeholder.
/// </summary>
public static class MatchSlugSourceBuilder
{
    /// <summary>
    /// Placeholder team name used when a match's home or visitor team is not
    /// yet assigned (e.g. an unseeded group match, or an empty knockout
    /// slot). Collisions between multiple such matches are resolved by
    /// SlugGenerator's -2/-3 suffixing, same as a real repeated pairing.
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
