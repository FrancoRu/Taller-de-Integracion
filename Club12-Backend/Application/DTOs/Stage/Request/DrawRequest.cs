using Domain.Enums;

using System;
using System.Collections.Generic;

namespace Application.DTOs.Stage.Request;

/// <summary>
/// A preview-draw or commit-draw request for a playoffs-only bracket, carrying either a random-draw token or an explicit manual order.
/// </summary>
public class DrawRequest
{
    public required DrawMode Mode { get; set; }

    public string? DrawToken { get; set; }

    public List<Guid>? ManualOrder { get; set; }
}
