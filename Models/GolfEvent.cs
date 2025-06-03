using System.ComponentModel.DataAnnotations;

namespace GolfTeamApp.Models
{
    public class GolfEvent
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Event Date")]
        [DataType(DataType.Date)]
        public DateTime EventDate { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime { get; set; }

        [Required]
        [StringLength(300)]
        public string Location { get; set; } = string.Empty;

        // Foreign Key
        [Display(Name = "Created By Coach")]
        public int CreatedByCoachId { get; set; }
        public Coach? CreatedByCoach { get; set; }

        // Navigation property
        public ICollection<EventScore> EventScores { get; set; } = new List<EventScore>();
    }
}