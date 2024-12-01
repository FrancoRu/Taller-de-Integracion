using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Player.Validation;

/// <summary>
/// Custom validation attribute to ensure Club and Category are provided when IsFederated is true.
/// </summary>
public class FederatedValidationAttribute : ValidationAttribute
{
    /// <summary>
    /// Override the IsValid method to check if Club and Category are provided when IsFederated is true.
    /// </summary>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        dynamic playerRequest = validationContext.ObjectInstance;

        if (playerRequest.IsFederated == true)
        {
            if (string.IsNullOrWhiteSpace(playerRequest.Club) || string.IsNullOrWhiteSpace(playerRequest.Category))
            {
                return new ValidationResult("Both Club and Category are required if the player is federated.");
            }
        }

        return ValidationResult.Success;
    }
}
