using Microsoft.EntityFrameworkCore;
using TournamentApp.Models;

namespace TournamentApp.Data
{
    public class TournamentDbContext : DbContext
    {
        public TournamentDbContext(DbContextOptions<TournamentDbContext> options) : base(options)
        {
        }
        
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<TournamentParticipant> TournamentParticipants { get; set; }
        public DbSet<Match> Matches { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<TournamentParticipant>()
                .HasKey(tp => new { tp.TournamentId, tp.ParticipantId });
            
            modelBuilder.Entity<TournamentParticipant>()
                .HasOne(tp => tp.Tournament)
                .WithMany(t => t.TournamentParticipants)
                .HasForeignKey(tp => tp.TournamentId);
                
            modelBuilder.Entity<TournamentParticipant>()
                .HasOne(tp => tp.Participant)
                .WithMany(p => p.TournamentParticipants)
                .HasForeignKey(tp => tp.ParticipantId);
            
            modelBuilder.Entity<Match>()
                .HasOne(m => m.Tournament)
                .WithMany(t => t.Matches)
                .HasForeignKey(m => m.TournamentId);
                
            modelBuilder.Entity<Match>()
                .HasOne(m => m.HomeParticipant)
                .WithMany(p => p.HomeMatches)
                .HasForeignKey(m => m.HomeParticipantId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Match>()
                .HasOne(m => m.AwayParticipant)
                .WithMany(p => p.AwayMatches)
                .HasForeignKey(m => m.AwayParticipantId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Tournament>()
                .HasOne(t => t.Winner)
                .WithMany()
                .HasForeignKey(t => t.WinnerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
} 