using System.ComponentModel.DataAnnotations;

namespace GolfTeamApp.Models
{
    public class Coach
    {
        public int CoachId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        public string? UserId { get; set; }  // Links to AspNetUsers table

        // Navigation property
        public ICollection<GolfEvent> CreatedEvents { get; set; } = new List<GolfEvent>();
    }
}