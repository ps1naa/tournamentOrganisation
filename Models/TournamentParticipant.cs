namespace TournamentApp.Models
{
    public class TournamentParticipant
    {
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;
        
        public int ParticipantId { get; set; }
        public Participant Participant { get; set; } = null!;
        
        public DateTime JoinedAt { get; set; } = DateTime.Now;
    }
} 