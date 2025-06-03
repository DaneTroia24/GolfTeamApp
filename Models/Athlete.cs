using System.ComponentModel.DataAnnotations;

namespace GolfTeamApp.Models
{
    public class Athlete
    {
        public int AthleteId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? PictureUrl { get; set; }

        [Range(0, 5)]
        public int SwingRating { get; set; }

        [Range(0, 5)]
        public int PowerRating { get; set; }

        [Range(0, 5)]
        public int UnderstandingRating { get; set; }

        // Foreign Key
        public int PartnerId { get; set; }
        public Partner? Partner { get; set; }

        // Link to Identity User (optional - athletes might not have login accounts)
        public string? UserId { get; set; }  // Links to AspNetUsers table

        // Navigation property
        public ICollection<EventScore> EventScores { get; set; } = new List<EventScore>();
    }
}