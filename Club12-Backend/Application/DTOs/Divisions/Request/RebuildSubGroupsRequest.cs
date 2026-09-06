using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Divisions.Request;

public class RebuildSubGroupsRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "SubGroupCount must be at least 1.")]
    public required int SubGroupCount { get; set; }
}
