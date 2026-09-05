using Application.DTOs.Abstract.Request;

using System;
namespace Application.DTOs.Player.Request;

/// <summary>
/// Base class for filtering players with common properties.
/// </summary>
public class PlayerFilterRequestBase : PaginatedFilterRequest
{
    public string? Names { get; set; }

    public string? LastName { get; set; }

    public Guid? TeamId { get; set; }
}

