using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace TournamentApp.Models
{
    public class Participant
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Имя участника обязательно")]
        [StringLength(50, ErrorMessage = "Имя не может быть длиннее 50 символов")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(100, ErrorMessage = "Email не может быть длиннее 100 символов")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public string? Email { get; set; }
        
        [StringLength(20, ErrorMessage = "Телефон не может быть длиннее 20 символов")]
        [RegularExpression(@"^\+375\s?\(?\d{2}\)?\s?\d{3}-?\d{2}-?\d{2}$", 
            ErrorMessage = "Неверный формат телефона. Используйте белорусский формат: +375 (XX) XXX-XX-XX")]
        public string? Phone { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public ICollection<TournamentParticipant> TournamentParticipants { get; set; } = new List<TournamentParticipant>();
        public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
        public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
    }
    
    public class ParticipantStatistics
    {
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public int TotalTournaments { get; set; }
        public int TotalMatches { get; set; }
        public int TotalWins { get; set; }
        public int TotalDraws { get; set; }
        public int TotalLosses { get; set; }
        public int TotalGoalsScored { get; set; }
        public int TotalGoalsConceded { get; set; }
        public double WinPercentage => TotalMatches > 0 ? (double)TotalWins / TotalMatches * 100 : 0;
        public int GoalDifference => TotalGoalsScored - TotalGoalsConceded;
    }
    
    public class HeadToHeadStatistics
    {
        public int Participant1Id { get; set; }
        public string Participant1Name { get; set; } = string.Empty;
        public int Participant2Id { get; set; }
        public string Participant2Name { get; set; } = string.Empty;
        public int TotalMatches { get; set; }
        public int Participant1Wins { get; set; }
        public int Participant2Wins { get; set; }
        public int Draws { get; set; }
        public int Participant1Goals { get; set; }
        public int Participant2Goals { get; set; }
    }
} 