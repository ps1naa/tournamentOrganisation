using System.ComponentModel.DataAnnotations;

namespace TournamentApp.Models
{
    public class Match
    {
        public int Id { get; set; }
        
        public int TournamentId { get; set; }
        public Tournament Tournament { get; set; } = null!;
        
        public int HomeParticipantId { get; set; }
        public Participant HomeParticipant { get; set; } = null!;
        
        public int AwayParticipantId { get; set; }
        public Participant AwayParticipant { get; set; } = null!;
        
        public int? HomeScore { get; set; }
        public int? AwayScore { get; set; }
        
        public DateTime? PlayedAt { get; set; }
        
        public bool IsCompleted { get; set; } = false;
        
        public MatchType Type { get; set; } = MatchType.Group;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public MatchResult Result
        {
            get
            {
                if (!IsCompleted || HomeScore == null || AwayScore == null)
                    return MatchResult.NotPlayed;
                
                if (HomeScore > AwayScore)
                    return MatchResult.HomeWin;
                else if (AwayScore > HomeScore)
                    return MatchResult.AwayWin;
                else
                    return MatchResult.Draw;
            }
        }
    }
    
    public enum MatchResult
    {
        NotPlayed,
        HomeWin,
        AwayWin,
        Draw
    }
    
    public enum MatchType
    {
        Group,
        Playoff,
        Final
    }
} 