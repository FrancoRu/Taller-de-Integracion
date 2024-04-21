using System.ComponentModel.DataAnnotations;

namespace Club12.Viewmodels.Team
{
    /// <summary>
    /// Represents a request to create a team.
    /// </summary>
    public class TeamRequest
    {
        /// <summary>
        /// The name of the team.
        /// </summary>
        [Required(ErrorMessage = "The Name field is required.")]
        public required string Name { get; set; }

        /// <summary>
        /// The three-letter code of the team.
        /// </summary>
        [Required(ErrorMessage = "The Three-letter code field is required.")]
        [MaxLength(3)]
        public required string ThreeLetterCode { get; set; }

        /// <summary>
        /// The unique identifier of the division to which the team belongs.
        /// </summary>
        [Required(ErrorMessage = "The DivisionId field is required.")]
        public required Guid DivisionId { get; set; }
    }
}
