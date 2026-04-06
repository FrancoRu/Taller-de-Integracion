using Application.DTOs.Abstract.Request;
using Domain.Enums;
using System;

namespace Application.DTOs.Stage.Request;

/// <summary>
/// Represents the filter and pagination parameters to retrieve a filtered list of stages.
/// </summary>
public class GetStagesFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// Optional filter by Division unique identifier.
    /// </summary>
    public Guid? DivisionId { get; set; }

    public Guid? TournamentId { get; set; }

    /// <summary>
    /// Optional filter by stage type.
    /// </summary>
    public StageType? StageType { get; set; }

    /// <summary>
    /// Optional filter by active status.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Optional filter to specify if the stage is elimination or not.
    /// </summary>
    public bool? IsElimination { get; set; }

    /// <summary>
    /// Optional filter by stage name (supports partial matching).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional filter to get stages starting on or after this date.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Optional filter to get stages ending on or before this date.
    /// </summary>
    public DateTime? EndDate { get; set; }
}
