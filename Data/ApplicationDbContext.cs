using GolfTeamApp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GolfTeamApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Athlete> Athletes { get; set; }
        public DbSet<Partner> Partners { get; set; }
        public DbSet<Coach> Coaches { get; set; }
        public DbSet<GolfEvent> GolfEvents { get; set; }
        public DbSet<EventScore> EventScores { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Define primary key for EventScore
            builder.Entity<EventScore>()
                .HasKey(es => es.ScoreId);

            builder.Entity<Athlete>()
                .HasOne(a => a.Partner)
                .WithMany(p => p.Athletes)
                .HasForeignKey(a => a.PartnerId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            builder.Entity<EventScore>()
                .HasOne(es => es.Athlete)
                .WithMany(a => a.EventScores)
                .HasForeignKey(es => es.AthleteId)
                .OnDelete(DeleteBehavior.Cascade); // Allow cascade delete

            builder.Entity<EventScore>()
                .HasOne(es => es.Event)
                .WithMany(e => e.EventScores)
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade); // Allow cascade delete

            builder.Entity<EventScore>()
                .HasOne(es => es.EnteredByPartner)
                .WithMany(p => p.EnteredScores)
                .HasForeignKey(es => es.EnteredByPartnerId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            builder.Entity<GolfEvent>()
                .HasOne(e => e.CreatedByCoach)
                .WithMany(c => c.CreatedEvents)
                .HasForeignKey(e => e.CreatedByCoachId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        }
    }
}
