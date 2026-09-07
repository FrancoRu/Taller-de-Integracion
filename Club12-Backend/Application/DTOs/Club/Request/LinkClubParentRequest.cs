using System;

namespace Application.DTOs.Club.Request;

/// <summary>
/// Links a club as a squad of the given parent institution club.
/// </summary>
public class LinkClubParentRequest
{
    public required Guid ParentClubId { get; set; }
}
