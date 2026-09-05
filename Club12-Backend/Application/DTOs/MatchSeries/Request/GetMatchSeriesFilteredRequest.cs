using Application.DTOs.Abstract.Request;

using System;

namespace Application.DTOs.MatchSeries.Request;

/// <summary>
/// Represents a request to filter and paginate playoff series.
/// </summary>
public class GetMatchSeriesFilteredRequest : PaginatedFilterRequest
{
    public Guid? StageId { get; set; }
}
