using System.ComponentModel.DataAnnotations;

namespace GolfTeamApp.Models
{
    public class Partner
    {
        public int PartnerId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        // Link to Identity User instead of storing password hash
        public string? UserId { get; set; }  // Links to AspNetUsers table

        // Navigation properties
        public ICollection<Athlete> Athletes { get; set; } = new List<Athlete>();
        public ICollection<EventScore> EnteredScores { get; set; } = new List<EventScore>();
    }
}