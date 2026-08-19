using Application.DTOs.Abstract.Request;

using System;

namespace Application.DTOs.MatchSeries.Request;

/// <summary>
/// Represents a request to filter and paginate playoff series.
/// </summary>
public class GetMatchSeriesFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// The stage (round) to filter series by.
    /// </summary>
    public Guid? StageId { get; set; }
}
