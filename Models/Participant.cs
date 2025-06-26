using System.ComponentModel.DataAnnotations;

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
        public string? Phone { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public ICollection<TournamentParticipant> TournamentParticipants { get; set; } = new List<TournamentParticipant>();
        public ICollection<Match> HomeMatches { get; set; } = new List<Match>();
        public ICollection<Match> AwayMatches { get; set; } = new List<Match>();
    }
} 