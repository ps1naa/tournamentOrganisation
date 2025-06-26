using Microsoft.EntityFrameworkCore;
using TournamentApp.Data;
using TournamentApp.Models;

namespace TournamentApp.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly TournamentDbContext _context;
        
        public TournamentService(TournamentDbContext context)
        {
            _context = context;
        }
        
        public async Task<List<Tournament>> GetAllTournamentsAsync()
        {
            return await _context.Tournaments
                .Include(t => t.TournamentParticipants)
                .ThenInclude(tp => tp.Participant)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<Tournament?> GetTournamentByIdAsync(int id)
        {
            return await _context.Tournaments
                .Include(t => t.TournamentParticipants)
                .ThenInclude(tp => tp.Participant)
                .Include(t => t.Matches)
                .ThenInclude(m => m.HomeParticipant)
                .Include(t => t.Matches)
                .ThenInclude(m => m.AwayParticipant)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        
        public async Task<Tournament> CreateTournamentAsync(Tournament tournament, List<int> participantIds)
        {
            _context.Tournaments.Add(tournament);
            await _context.SaveChangesAsync();
            
            foreach (var participantId in participantIds)
            {
                var tournamentParticipant = new TournamentParticipant
                {
                    TournamentId = tournament.Id,
                    ParticipantId = participantId
                };
                _context.TournamentParticipants.Add(tournamentParticipant);
            }
            
            for (int i = 0; i < participantIds.Count; i++)
            {
                for (int j = i + 1; j < participantIds.Count; j++)
                {
                    var match = new Match
                    {
                        TournamentId = tournament.Id,
                        HomeParticipantId = participantIds[i],
                        AwayParticipantId = participantIds[j]
                    };
                    _context.Matches.Add(match);
                }
            }
            
            await _context.SaveChangesAsync();
            return tournament;
        }
        
        public async Task<bool> UpdateTournamentAsync(int id, Tournament tournament)
        {
            var existingTournament = await _context.Tournaments.FindAsync(id);
            if (existingTournament == null)
                return false;
            
            existingTournament.Name = tournament.Name;
            existingTournament.StartDate = tournament.StartDate;
            existingTournament.EndDate = tournament.EndDate;
            existingTournament.Description = tournament.Description;
            
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> DeleteTournamentAsync(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.Matches)
                .Include(t => t.TournamentParticipants)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tournament == null)
                return false;

            _context.Matches.RemoveRange(tournament.Matches);
            _context.TournamentParticipants.RemoveRange(tournament.TournamentParticipants);
            _context.Tournaments.Remove(tournament);

            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<List<Match>> GetTournamentMatchesAsync(int tournamentId)
        {
            return await _context.Matches
                .Include(m => m.HomeParticipant)
                .Include(m => m.AwayParticipant)
                .Where(m => m.TournamentId == tournamentId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }
        
        public async Task<Match?> GetMatchByIdAsync(int matchId)
        {
            return await _context.Matches
                .Include(m => m.HomeParticipant)
                .Include(m => m.AwayParticipant)
                .Include(m => m.Tournament)
                .FirstOrDefaultAsync(m => m.Id == matchId);
        }
        
        public async Task<bool> UpdateMatchResultAsync(int matchId, int? homeScore, int? awayScore, bool isCompleted)
        {
            var match = await _context.Matches.FindAsync(matchId);
            if (match == null)
                return false;
            
            match.HomeScore = homeScore;
            match.AwayScore = awayScore;
            match.IsCompleted = isCompleted;
            match.PlayedAt = isCompleted ? DateTime.Now : null;
            
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<List<ParticipantStanding>> GetTournamentStandingsAsync(int tournamentId)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentParticipants)
                .ThenInclude(tp => tp.Participant)
                .Include(t => t.Matches)
                .FirstOrDefaultAsync(t => t.Id == tournamentId);
            
            if (tournament == null)
                return new List<ParticipantStanding>();
            
            var standings = new List<ParticipantStanding>();
            
            foreach (var tp in tournament.TournamentParticipants)
            {
                var participant = tp.Participant;
                var participantMatches = tournament.Matches
                    .Where(m => m.HomeParticipantId == participant.Id || m.AwayParticipantId == participant.Id)
                    .Where(m => m.IsCompleted)
                    .ToList();
                
                var standing = new ParticipantStanding
                {
                    ParticipantId = participant.Id,
                    ParticipantName = participant.Name,
                    MatchesPlayed = participantMatches.Count,
                    Wins = participantMatches.Count(m => 
                        (m.HomeParticipantId == participant.Id && m.Result == MatchResult.HomeWin) ||
                        (m.AwayParticipantId == participant.Id && m.Result == MatchResult.AwayWin)),
                    Draws = participantMatches.Count(m => m.Result == MatchResult.Draw),
                    Losses = participantMatches.Count(m => 
                        (m.HomeParticipantId == participant.Id && m.Result == MatchResult.AwayWin) ||
                        (m.AwayParticipantId == participant.Id && m.Result == MatchResult.HomeWin)),
                    GoalsFor = participantMatches.Sum(m => 
                        m.HomeParticipantId == participant.Id ? m.HomeScore ?? 0 : m.AwayScore ?? 0),
                    GoalsAgainst = participantMatches.Sum(m => 
                        m.HomeParticipantId == participant.Id ? m.AwayScore ?? 0 : m.HomeScore ?? 0)
                };
                
                standings.Add(standing);
            }
            
            return standings
                .OrderByDescending(s => s.Points)
                .ThenByDescending(s => s.GoalDifference)
                .ThenByDescending(s => s.GoalsFor)
                .ToList();
        }
        
        public async Task<List<Participant>> GetAllParticipantsAsync()
        {
            return await _context.Participants
                .OrderBy(p => p.Name)
                .ToListAsync();
        }
        
        public async Task<Participant> CreateParticipantAsync(Participant participant)
        {
            _context.Participants.Add(participant);
            await _context.SaveChangesAsync();
            return participant;
        }
    }
} 