using TournamentApp.Models;

namespace TournamentApp.Services
{
    public interface ITournamentService
    {
        Task<List<Tournament>> GetAllTournamentsAsync();
        Task<Tournament?> GetTournamentByIdAsync(int id);
        Task<Tournament> CreateTournamentAsync(Tournament tournament, List<int> participantIds);
        Task<bool> UpdateTournamentAsync(int id, Tournament tournament);
        Task<bool> DeleteTournamentAsync(int id);
        Task<List<Match>> GetTournamentMatchesAsync(int tournamentId);
        Task<Match?> GetMatchByIdAsync(int matchId);
        Task<bool> UpdateMatchResultAsync(int matchId, int? homeScore, int? awayScore, bool isCompleted);
        Task<List<ParticipantStanding>> GetTournamentStandingsAsync(int tournamentId);
        Task<List<Participant>> GetAllParticipantsAsync();
        Task<Participant> CreateParticipantAsync(Participant participant);
        Task<Participant?> GetParticipantByIdAsync(int id);
        Task<bool> UpdateParticipantAsync(int id, Participant participant);
        Task<bool> DeleteParticipantAsync(int id);
        Task<ParticipantStatistics> GetParticipantStatisticsAsync(int participantId);
        Task<List<HeadToHeadStatistics>> GetHeadToHeadStatisticsAsync();
        Task<bool> GeneratePlayoffAsync(int tournamentId);
        Task<bool> GenerateFinalAsync(int tournamentId);
    }
    
    public class ParticipantStanding
    {
        public int ParticipantId { get; set; }
        public string ParticipantName { get; set; } = string.Empty;
        public int MatchesPlayed { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int GoalDifference => GoalsFor - GoalsAgainst;
        public int Points => (Wins * 3) + Draws;
    }
} 