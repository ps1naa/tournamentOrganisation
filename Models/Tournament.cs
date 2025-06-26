using System.ComponentModel.DataAnnotations;

namespace TournamentApp.Models
{
    public class Tournament
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Название турнира обязательно")]
        [StringLength(100, ErrorMessage = "Название не может быть длиннее 100 символов")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Дата начала обязательна")]
        public DateTime StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        public string? Description { get; set; }
        
        public bool IsCompleted { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public ICollection<TournamentParticipant> TournamentParticipants { get; set; } = new List<TournamentParticipant>();
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }
} 