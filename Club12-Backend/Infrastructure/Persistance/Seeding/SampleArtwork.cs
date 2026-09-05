using System;
using System.Text;

namespace Infrastructure.Persistance;

/// <summary>
/// Generates the sample dataset's artwork as self-contained SVG data URIs.
/// </summary>
internal static class SampleArtwork
{
    /// <summary>
    /// Widest a generated data URI may get, matching the 2048 character cap on BlogPost.PhotoUrl in its EF configuration.
    /// </summary>
    public const int MaxDataUriLength = 2048;

    // Court floors: plausible hardwood tones, picked by the venue's name.
    private static readonly string[] Floors = ["#B4763A", "#C08A4A", "#A9682F", "#CE9856"];

    // Painted key colours, likewise per venue.
    private static readonly string[] Keys = ["#1D4ED8", "#B91C1C", "#0F766E", "#7C3AED", "#C2410C"];

    // Cover gradients: pairs dark enough for white text in both stops.
    private static readonly (string From, string To)[] CoverGradients =
    [
        ("#0F172A", "#1D4ED8"), ("#7C2D12", "#C2410C"), ("#134E4A", "#0F766E"),
        ("#3B0764", "#7C3AED"), ("#450A0A", "#B91C1C"), ("#082F49", "#0284C7"),
    ];

    /// <summary>
    /// A stylised plan of the venue's court, with hardwood floor, sidelines, centre circle, and both painted keys.
    /// </summary>
    public static string VenuePhotoDataUri(string venueName)
    {
        // Floor and key are read from different slices of the hash: taking the
        // second from the same low bits as the first correlated them, and the
        // 14 seeded gyms then landed on only 9 of the 20 possible combinations.
        int hash = StableHash(venueName);
        string floor = Floors[hash % Floors.Length];
        string key = Keys[(hash >> 8) % Keys.Length];

        string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 640 360' width='640' height='360'>"
            + $"<rect width='640' height='360' fill='{floor}'/>"
            + $"<rect x='24' y='106' width='108' height='148' fill='{key}' fill-opacity='0.85'/>"
            + $"<rect x='508' y='106' width='108' height='148' fill='{key}' fill-opacity='0.85'/>"
            + "<g fill='none' stroke='#FFFFFF' stroke-width='4'>"
            + "<rect x='24' y='24' width='592' height='312'/>"
            + "<path d='M320 24v312'/>"
            + "<circle cx='320' cy='180' r='46'/>"
            + "<rect x='24' y='106' width='108' height='148'/>"
            + "<rect x='508' y='106' width='108' height='148'/>"
            + "<path d='M132 106a86 74 0 0 1 0 148'/>"
            + "<path d='M508 106a86 74 0 0 0 0 148'/>"
            + "</g>"
            + "</svg>";

        return ToDataUri(svg);
    }

    /// <summary>
    /// A 1200 by 630 social-card cover banner for a post, with a gradient chosen from postSlug, a ball outline, and the league's name.
    /// </summary>
    public static string BlogCoverDataUri(string postSlug)
    {
        (string from, string to) = CoverGradients[StableHash(postSlug) % CoverGradients.Length];

        string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1200 630' width='1200' height='630'>"
            + "<defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'>"
            + $"<stop offset='0' stop-color='{from}'/><stop offset='1' stop-color='{to}'/>"
            + "</linearGradient></defs>"
            + "<rect width='1200' height='630' fill='url(#g)'/>"
            + "<g fill='none' stroke='#FFFFFF' stroke-opacity='0.34' stroke-width='10'>"
            + "<circle cx='980' cy='430' r='190'/>"
            + "<path d='M790 430h380M980 240v380M846 296q134 134 0 268M1114 296q-134 134 0 268'/>"
            + "</g>"
            + "<text x='72' y='128' font-family='Helvetica,Arial,sans-serif' font-size='46' "
            + "font-weight='700' fill='#FFFFFF' fill-opacity='0.88'>LIGA CLUB 12</text>"
            + "</svg>";

        return ToDataUri(svg);
    }

    /// <summary>
    /// Base64 data URI for an SVG document.
    /// </summary>
    private static string ToDataUri(string svg) =>
        "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(svg));

    /// <summary>
    /// Small, stable, non-negative hash of a string.
    /// </summary>
    private static int StableHash(string value)
    {
        int hash = 17;
        foreach (char character in value)
        {
            hash = (hash * 31) + character;
        }

        return hash & 0x7FFFFFFF;
    }
}
