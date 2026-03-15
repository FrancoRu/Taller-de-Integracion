using System;

namespace Domain.Entities.Models;

public class Player : EntityBase
{
    public required string FirstName { get; set; }
    public string? SecondName { get; set; }
    public required string LastName { get; set; }

    /// <summary>Computed — not persisted to the database.</summary>
    public string FullName => string.Concat(
        LastName.ToUpper(),
        string.IsNullOrWhiteSpace(SecondName) ? $" {FirstName}" : $" {FirstName} {SecondName}"
    );

    public required string DocumentNumber { get; set; }
    public required bool IsSanctioned { get; set; } = false;
    public string? PhoneNumber { get; set; }
    public required DateTime BirthDate { get; set; }
    public required string SocialSecurity { get; set; }
    public required Team Team { get; set; }
    public Guid TeamId { get; set; }
}