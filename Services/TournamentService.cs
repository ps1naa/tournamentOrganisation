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
            
            for (int round = 0; round < tournament.MatchesPerOpponent; round++)
            {
                for (int i = 0; i < participantIds.Count; i++)
                {
                    for (int j = i + 1; j < participantIds.Count; j++)
                    {
                        var match = new Match
                        {
                            TournamentId = tournament.Id,
                            HomeParticipantId = participantIds[i],
                            AwayParticipantId = participantIds[j],
                            Type = TournamentApp.Models.MatchType.Group
                        };
                        _context.Matches.Add(match);
                    }
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
        
        public async Task<Participant?> GetParticipantByIdAsync(int id)
        {
            return await _context.Participants.FindAsync(id);
        }
        
        public async Task<bool> UpdateParticipantAsync(int id, Participant participant)
        {
            var existingParticipant = await _context.Participants.FindAsync(id);
            if (existingParticipant == null)
                return false;
            
            existingParticipant.Name = participant.Name;
            existingParticipant.Email = participant.Email;
            existingParticipant.Phone = participant.Phone;
            
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<bool> DeleteParticipantAsync(int id)
        {
            var participant = await _context.Participants
                .Include(p => p.TournamentParticipants)
                .Include(p => p.HomeMatches)
                .Include(p => p.AwayMatches)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (participant == null)
                return false;
            
            var hasCompletedMatches = participant.HomeMatches.Any(m => m.IsCompleted) || 
                                    participant.AwayMatches.Any(m => m.IsCompleted);
            
            if (hasCompletedMatches)
                return false;
            
            _context.Participants.Remove(participant);
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<ParticipantStatistics> GetParticipantStatisticsAsync(int participantId)
        {
            var participant = await _context.Participants
                .Include(p => p.TournamentParticipants)
                .Include(p => p.HomeMatches.Where(m => m.IsCompleted))
                .Include(p => p.AwayMatches.Where(m => m.IsCompleted))
                .FirstOrDefaultAsync(p => p.Id == participantId);
            
            if (participant == null)
                return new ParticipantStatistics();
            
            var allMatches = participant.HomeMatches.Concat(participant.AwayMatches).ToList();
            var completedMatches = allMatches.Where(m => m.IsCompleted).ToList();
            
            var stats = new ParticipantStatistics
            {
                ParticipantId = participant.Id,
                ParticipantName = participant.Name,
                TotalTournaments = participant.TournamentParticipants.Count,
                TotalMatches = completedMatches.Count,
                TotalWins = completedMatches.Count(m => 
                    (m.HomeParticipantId == participantId && m.Result == MatchResult.HomeWin) ||
                    (m.AwayParticipantId == participantId && m.Result == MatchResult.AwayWin)),
                TotalDraws = completedMatches.Count(m => m.Result == MatchResult.Draw),
                TotalLosses = completedMatches.Count(m => 
                    (m.HomeParticipantId == participantId && m.Result == MatchResult.AwayWin) ||
                    (m.AwayParticipantId == participantId && m.Result == MatchResult.HomeWin)),
                TotalGoalsScored = completedMatches.Sum(m => 
                    m.HomeParticipantId == participantId ? m.HomeScore ?? 0 : m.AwayScore ?? 0),
                TotalGoalsConceded = completedMatches.Sum(m => 
                    m.HomeParticipantId == participantId ? m.AwayScore ?? 0 : m.HomeScore ?? 0)
            };
            
            return stats;
        }
        
        public async Task<List<HeadToHeadStatistics>> GetHeadToHeadStatisticsAsync()
        {
            var participants = await _context.Participants.ToListAsync();
            var headToHeadStats = new List<HeadToHeadStatistics>();
            
            for (int i = 0; i < participants.Count; i++)
            {
                for (int j = i + 1; j < participants.Count; j++)
                {
                    var participant1 = participants[i];
                    var participant2 = participants[j];
                    
                    var matches = await _context.Matches
                        .Where(m => m.IsCompleted && 
                                   ((m.HomeParticipantId == participant1.Id && m.AwayParticipantId == participant2.Id) ||
                                    (m.HomeParticipantId == participant2.Id && m.AwayParticipantId == participant1.Id)))
                        .ToListAsync();
                    
                    if (matches.Any())
                    {
                        var stats = new HeadToHeadStatistics
                        {
                            Participant1Id = participant1.Id,
                            Participant1Name = participant1.Name,
                            Participant2Id = participant2.Id,
                            Participant2Name = participant2.Name,
                            TotalMatches = matches.Count,
                            Participant1Wins = matches.Count(m => 
                                (m.HomeParticipantId == participant1.Id && m.Result == MatchResult.HomeWin) ||
                                (m.AwayParticipantId == participant1.Id && m.Result == MatchResult.AwayWin)),
                            Participant2Wins = matches.Count(m => 
                                (m.HomeParticipantId == participant2.Id && m.Result == MatchResult.HomeWin) ||
                                (m.AwayParticipantId == participant2.Id && m.Result == MatchResult.AwayWin)),
                            Draws = matches.Count(m => m.Result == MatchResult.Draw),
                            Participant1Goals = matches.Sum(m => 
                                m.HomeParticipantId == participant1.Id ? m.HomeScore ?? 0 : m.AwayScore ?? 0),
                            Participant2Goals = matches.Sum(m => 
                                m.HomeParticipantId == participant2.Id ? m.HomeScore ?? 0 : m.AwayScore ?? 0)
                        };
                        
                        headToHeadStats.Add(stats);
                    }
                }
            }
            
            return headToHeadStats;
        }
        
        public async Task<bool> GeneratePlayoffAsync(int tournamentId)
        {
            var tournament = await GetTournamentByIdAsync(tournamentId);
            if (tournament == null || tournament.PlayoffGenerated)
                return false;
            
            var standings = await GetTournamentStandingsAsync(tournamentId);
            var participantCount = standings.Count;
            
            List<ParticipantStanding> playoffParticipants;
            
            if (participantCount >= 4)
            {
                playoffParticipants = standings.Take(4).ToList();
                
                var semifinal1 = new Match
                {
                    TournamentId = tournamentId,
                    HomeParticipantId = playoffParticipants[0].ParticipantId,
                    AwayParticipantId = playoffParticipants[3].ParticipantId,
                    Type = TournamentApp.Models.MatchType.Playoff
                };
                
                var semifinal2 = new Match
                {
                    TournamentId = tournamentId,
                    HomeParticipantId = playoffParticipants[1].ParticipantId,
                    AwayParticipantId = playoffParticipants[2].ParticipantId,
                    Type = TournamentApp.Models.MatchType.Playoff
                };
                
                _context.Matches.AddRange(semifinal1, semifinal2);
            }
            else if (participantCount >= 2)
            {
                playoffParticipants = standings.Take(2).ToList();
                
                var final = new Match
                {
                    TournamentId = tournamentId,
                    HomeParticipantId = playoffParticipants[0].ParticipantId,
                    AwayParticipantId = playoffParticipants[1].ParticipantId,
                    Type = TournamentApp.Models.MatchType.Final
                };
                
                _context.Matches.Add(final);
            }
            
            tournament.PlayoffGenerated = true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 