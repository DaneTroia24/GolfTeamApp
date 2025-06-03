using System.ComponentModel.DataAnnotations;

namespace GolfTeamApp.Models
{
    public class EventScore
    {
        [Key]
        public int ScoreId { get; set; }

        // Foreign Keys
        public int AthleteId { get; set; }
        public Athlete? Athlete { get; set; }

        public int EventId { get; set; }
        public GolfEvent? Event { get; set; }

        public int EnteredByPartnerId { get; set; }
        public Partner? EnteredByPartner { get; set; }

        // Score Data
        [Required]
        public int GolfScore { get; set; }

        [Range(1, 18)]
        public int HolesCompleted { get; set; }

        public DateTime Timestamp { get; set; }
    }
}