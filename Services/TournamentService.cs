using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TournamentApp.Data;
using TournamentApp.Models;

namespace TournamentApp.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly TournamentDbContext _context;
        private readonly string _connectionString;

        public TournamentService(TournamentDbContext context, IConfiguration configuration)
        {
            _context = context;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                               throw new ArgumentException("Connection string not found");
        }

        #region Tournament Operations

        public async Task<List<Tournament>> GetAllTournamentsAsync()
        {
            var tournaments = new List<Tournament>();
            var tournamentDict = new Dictionary<int, Tournament>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetAllTournaments", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var tournamentId = reader.GetInt32("Id");

                if (!tournamentDict.ContainsKey(tournamentId))
                {
                    var tournament = new Tournament
                    {
                        Id = tournamentId,
                        Name = reader.GetString("Name"),
                        StartDate = reader.GetDateTime("StartDate"),
                        EndDate = reader.IsDBNull("EndDate") ? null : reader.GetDateTime("EndDate"),
                        Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                        MatchesPerOpponent = reader.GetInt32("MatchesPerOpponent"),
                        IsCompleted = reader.GetBoolean("IsCompleted"),
                        PlayoffGenerated = reader.GetBoolean("PlayoffGenerated"),
                        WinnerId = reader.IsDBNull("WinnerId") ? null : reader.GetInt32("WinnerId"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        TournamentParticipants = new List<TournamentParticipant>()
                    };

                    tournamentDict[tournamentId] = tournament;
                    tournaments.Add(tournament);
                }


                if (!reader.IsDBNull("ParticipantId"))
                {
                    var participant = new Participant
                    {
                        Id = reader.GetInt32("ParticipantId"),
                        Name = reader.GetString("ParticipantName"),
                        Email = reader.IsDBNull("ParticipantEmail") ? null : reader.GetString("ParticipantEmail"),
                        Phone = reader.IsDBNull("ParticipantPhone") ? null : reader.GetString("ParticipantPhone"),
                        CreatedAt = reader.GetDateTime("ParticipantCreatedAt")
                    };

                    var tournamentParticipant = new TournamentParticipant
                    {
                        TournamentId = tournamentId,
                        ParticipantId = participant.Id,
                        Participant = participant,
                        JoinedAt = reader.GetDateTime("JoinedAt")
                    };

                    tournamentDict[tournamentId].TournamentParticipants.Add(tournamentParticipant);
                }

                var currentTournament = tournamentDict[tournamentId];
                if (!reader.IsDBNull("WinnerId") && currentTournament.Winner == null)
                {
                    var winnerId = reader.GetInt32("WinnerId");
                    var winnerParticipant = currentTournament.TournamentParticipants.FirstOrDefault(tp => tp.ParticipantId == winnerId);
                    if (winnerParticipant != null)
                    {
                        currentTournament.Winner = winnerParticipant.Participant;
                    }
                    else
                    {
                        try 
                        {
                            if (!reader.IsDBNull("WinnerName"))
                            {
                                currentTournament.Winner = new Participant
                                {
                                    Id = winnerId,
                                    Name = reader.GetString("WinnerName")
                                };
                            }
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }
            }

            return tournaments;
        }

        public async Task<Tournament?> GetTournamentByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetTournamentById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@TournamentId", id);

            using var reader = await command.ExecuteReaderAsync();

            Tournament? tournament = null;


            if (await reader.ReadAsync())
            {
                tournament = new Tournament
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    StartDate = reader.GetDateTime("StartDate"),
                    EndDate = reader.IsDBNull("EndDate") ? null : reader.GetDateTime("EndDate"),
                    Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                    MatchesPerOpponent = reader.GetInt32("MatchesPerOpponent"),
                    IsCompleted = reader.GetBoolean("IsCompleted"),
                    PlayoffGenerated = reader.GetBoolean("PlayoffGenerated"),
                    WinnerId = reader.IsDBNull("WinnerId") ? null : reader.GetInt32("WinnerId"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    TournamentParticipants = new List<TournamentParticipant>(),
                    Matches = new List<Match>()
                };

                if (!reader.IsDBNull("WinnerId"))
                {
                    try
                    {
                        if (!reader.IsDBNull("WinnerName"))
                        {
                            tournament.Winner = new Participant
                            {
                                Id = reader.GetInt32("WinnerId"),
                                Name = reader.GetString("WinnerName")
                            };
                        }
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }

            if (tournament == null) return null;


            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    var participant = new Participant
                    {
                        Id = reader.GetInt32("Id"),
                        Name = reader.GetString("Name"),
                        Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                        Phone = reader.IsDBNull("Phone") ? null : reader.GetString("Phone"),
                        CreatedAt = reader.GetDateTime("CreatedAt")
                    };

                    var tournamentParticipant = new TournamentParticipant
                    {
                        TournamentId = tournament.Id,
                        ParticipantId = participant.Id,
                        Participant = participant,
                        Tournament = tournament,
                        JoinedAt = reader.GetDateTime("JoinedAt")
                    };

                    tournament.TournamentParticipants.Add(tournamentParticipant);
                }
            }

            if (tournament.WinnerId.HasValue && tournament.Winner == null)
            {
                var winner = tournament.TournamentParticipants
                    .FirstOrDefault(tp => tp.ParticipantId == tournament.WinnerId.Value);
                if (winner != null)
                {
                    tournament.Winner = winner.Participant;
                }
            }

            if (await reader.NextResultAsync())
            {
                while (await reader.ReadAsync())
                {
                    var homeParticipant = new Participant 
                    { 
                        Id = reader.GetInt32("HomeParticipantId"), 
                        Name = reader.GetString("HomeParticipantName") 
                    };
                    var awayParticipant = new Participant 
                    { 
                        Id = reader.GetInt32("AwayParticipantId"), 
                        Name = reader.GetString("AwayParticipantName") 
                    };

                    var match = new Match
                    {
                        Id = reader.GetInt32("Id"),
                        TournamentId = reader.GetInt32("TournamentId"),
                        HomeParticipantId = reader.GetInt32("HomeParticipantId"),
                        AwayParticipantId = reader.GetInt32("AwayParticipantId"),
                        HomeScore = reader.IsDBNull("HomeScore") ? null : reader.GetInt32("HomeScore"),
                        AwayScore = reader.IsDBNull("AwayScore") ? null : reader.GetInt32("AwayScore"),
                        PlayedAt = reader.IsDBNull("PlayedAt") ? null : reader.GetDateTime("PlayedAt"),
                        IsCompleted = reader.GetBoolean("IsCompleted"),
                        Type = (TournamentApp.Models.MatchType)reader.GetInt32("Type"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        Tournament = tournament,
                        HomeParticipant = homeParticipant,
                        AwayParticipant = awayParticipant
                    };

                    tournament.Matches.Add(match);
                }
            }

            return tournament;
        }

        public async Task<Tournament> CreateTournamentAsync(Tournament tournament, List<int> participantIds)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {

                using var createCommand = new SqlCommand("sp_CreateTournament", connection, transaction)
                {
                    CommandType = CommandType.StoredProcedure
                };
                createCommand.Parameters.AddWithValue("@Name", tournament.Name);
                createCommand.Parameters.AddWithValue("@StartDate", tournament.StartDate);
                createCommand.Parameters.AddWithValue("@EndDate", (object?)tournament.EndDate ?? DBNull.Value);
                createCommand.Parameters.AddWithValue("@Description", (object?)tournament.Description ?? DBNull.Value);
                createCommand.Parameters.AddWithValue("@MatchesPerOpponent", tournament.MatchesPerOpponent);

                using var reader = await createCommand.ExecuteReaderAsync();
                await reader.ReadAsync();
                tournament.Id = reader.GetInt32("TournamentId");
                reader.Close();


                using var participantsCommand = new SqlCommand("sp_AddTournamentParticipants", connection, transaction)
                {
                    CommandType = CommandType.StoredProcedure
                };
                participantsCommand.Parameters.AddWithValue("@TournamentId", tournament.Id);
                participantsCommand.Parameters.AddWithValue("@ParticipantIds", string.Join(",", participantIds));
                await participantsCommand.ExecuteNonQueryAsync();


                using var matchesCommand = new SqlCommand("sp_CreateTournamentMatches", connection, transaction)
                {
                    CommandType = CommandType.StoredProcedure
                };
                matchesCommand.Parameters.AddWithValue("@TournamentId", tournament.Id);
                matchesCommand.Parameters.AddWithValue("@MatchesPerOpponent", tournament.MatchesPerOpponent);
                await matchesCommand.ExecuteNonQueryAsync();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return tournament;
        }

        public async Task<bool> UpdateTournamentAsync(int id, Tournament tournament)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_UpdateTournament", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@TournamentId", id);
            command.Parameters.AddWithValue("@Name", tournament.Name);
            command.Parameters.AddWithValue("@StartDate", tournament.StartDate);
            command.Parameters.AddWithValue("@EndDate", (object?)tournament.EndDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@Description", (object?)tournament.Description ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return reader.GetInt32("RowsAffected") > 0;
        }

        public async Task<bool> DeleteTournamentAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_DeleteTournament", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@TournamentId", id);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return reader.GetInt32("RowsAffected") > 0;
        }

        #endregion

        #region Match Operations

        public async Task<List<Match>> GetTournamentMatchesAsync(int tournamentId)
        {
            var matches = new List<Match>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetTournamentMatches", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@TournamentId", tournamentId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var homeParticipant = new Participant 
                { 
                    Id = reader.GetInt32("HomeParticipantId"), 
                    Name = reader.GetString("HomeParticipantName") 
                };
                var awayParticipant = new Participant 
                { 
                    Id = reader.GetInt32("AwayParticipantId"), 
                    Name = reader.GetString("AwayParticipantName") 
                };

                var match = new Match
                {
                    Id = reader.GetInt32("Id"),
                    TournamentId = reader.GetInt32("TournamentId"),
                    HomeParticipantId = reader.GetInt32("HomeParticipantId"),
                    AwayParticipantId = reader.GetInt32("AwayParticipantId"),
                    HomeScore = reader.IsDBNull("HomeScore") ? null : reader.GetInt32("HomeScore"),
                    AwayScore = reader.IsDBNull("AwayScore") ? null : reader.GetInt32("AwayScore"),
                    PlayedAt = reader.IsDBNull("PlayedAt") ? null : reader.GetDateTime("PlayedAt"),
                    IsCompleted = reader.GetBoolean("IsCompleted"),
                    Type = (TournamentApp.Models.MatchType)reader.GetInt32("Type"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    HomeParticipant = homeParticipant,
                    AwayParticipant = awayParticipant
                };

                matches.Add(match);
            }

            return matches;
        }

        public async Task<Match?> GetMatchByIdAsync(int matchId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetMatchById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@MatchId", matchId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var homeParticipant = new Participant 
                { 
                    Id = reader.GetInt32("HomeParticipantId"), 
                    Name = reader.GetString("HomeParticipantName") 
                };
                var awayParticipant = new Participant 
                { 
                    Id = reader.GetInt32("AwayParticipantId"), 
                    Name = reader.GetString("AwayParticipantName") 
                };
                var tournament = new Tournament 
                { 
                    Id = reader.GetInt32("TournamentId"), 
                    Name = reader.GetString("TournamentName") 
                };

                return new Match
                {
                    Id = reader.GetInt32("Id"),
                    TournamentId = reader.GetInt32("TournamentId"),
                    HomeParticipantId = reader.GetInt32("HomeParticipantId"),
                    AwayParticipantId = reader.GetInt32("AwayParticipantId"),
                    HomeScore = reader.IsDBNull("HomeScore") ? null : reader.GetInt32("HomeScore"),
                    AwayScore = reader.IsDBNull("AwayScore") ? null : reader.GetInt32("AwayScore"),
                    PlayedAt = reader.IsDBNull("PlayedAt") ? null : reader.GetDateTime("PlayedAt"),
                    IsCompleted = reader.GetBoolean("IsCompleted"),
                    Type = (TournamentApp.Models.MatchType)reader.GetInt32("Type"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    HomeParticipant = homeParticipant,
                    AwayParticipant = awayParticipant,
                    Tournament = tournament
                };
            }

            return null;
        }

        public async Task<bool> UpdateMatchResultAsync(int matchId, int? homeScore, int? awayScore, bool isCompleted)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var match = await GetMatchByIdAsync(matchId);
            if (match == null) return false;

            using var command = new SqlCommand("sp_UpdateMatchResult", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@MatchId", matchId);
            command.Parameters.AddWithValue("@HomeScore", (object?)homeScore ?? DBNull.Value);
            command.Parameters.AddWithValue("@AwayScore", (object?)awayScore ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsCompleted", isCompleted);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var success = reader.GetInt32("RowsAffected") > 0;

            if (success && isCompleted)
            {
                if (match.Type == TournamentApp.Models.MatchType.Playoff)
                {
                    try
                    {
                        await GenerateFinalAsync(match.TournamentId);
                    }
                    catch
                    {
                    }
                }
                else if (match.Type == TournamentApp.Models.MatchType.Final)
                {
                    await CompleteTournamentAsync(match.TournamentId);
                }
            }

            return success;
        }

        private async Task CompleteTournamentAsync(int tournamentId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            int? winnerId = null;

            using var finalCommand = new SqlCommand(@"
                SELECT HomeParticipantId, AwayParticipantId, HomeScore, AwayScore
                FROM Matches 
                WHERE TournamentId = @TournamentId 
                    AND Type = 2 
                    AND IsCompleted = 1 
                    AND HomeScore IS NOT NULL 
                    AND AwayScore IS NOT NULL", connection);
            finalCommand.Parameters.AddWithValue("@TournamentId", tournamentId);

            using var finalReader = await finalCommand.ExecuteReaderAsync();
            if (await finalReader.ReadAsync())
            {
                var homeScore = finalReader.GetInt32("HomeScore");
                var awayScore = finalReader.GetInt32("AwayScore");
                var homeParticipantId = finalReader.GetInt32("HomeParticipantId");
                var awayParticipantId = finalReader.GetInt32("AwayParticipantId");

                if (homeScore > awayScore)
                {
                    winnerId = homeParticipantId;
                }
                else if (awayScore > homeScore)
                {
                    winnerId = awayParticipantId;
                }
            }
            finalReader.Close();

            if (winnerId == null)
            {
                using var standingsCommand = new SqlCommand(@"
                    SELECT TOP 1 ParticipantId 
                    FROM vw_TournamentStandings 
                    WHERE TournamentId = @TournamentId 
                    ORDER BY Points DESC, GoalDifference DESC, GoalsFor DESC", connection);
                standingsCommand.Parameters.AddWithValue("@TournamentId", tournamentId);

                var result = await standingsCommand.ExecuteScalarAsync();
                if (result != null)
                {
                    winnerId = (int)result;
                }
            }

            if (winnerId.HasValue)
            {
                using var updateCommand = new SqlCommand(@"
                    UPDATE Tournaments 
                    SET IsCompleted = 1, WinnerId = @WinnerId 
                    WHERE Id = @TournamentId", connection);
                updateCommand.Parameters.AddWithValue("@TournamentId", tournamentId);
                updateCommand.Parameters.AddWithValue("@WinnerId", winnerId.Value);

                await updateCommand.ExecuteNonQueryAsync();
            }
        }

        #endregion

        #region Participant Operations

        public async Task<List<Participant>> GetAllParticipantsAsync()
        {
            var participants = new List<Participant>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetAllParticipants", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var participant = new Participant
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                    Phone = reader.IsDBNull("Phone") ? null : reader.GetString("Phone"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                };

                participants.Add(participant);
            }

            return participants;
        }

        public async Task<Participant> CreateParticipantAsync(Participant participant)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_CreateParticipant", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Name", participant.Name);
            command.Parameters.AddWithValue("@Email", (object?)participant.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object?)participant.Phone ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            participant.Id = reader.GetInt32("ParticipantId");
            participant.CreatedAt = DateTime.Now;

            return participant;
        }

        public async Task<Participant?> GetParticipantByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetParticipantById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ParticipantId", id);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Participant
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                    Phone = reader.IsDBNull("Phone") ? null : reader.GetString("Phone"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                };
            }

            return null;
        }

        public async Task<bool> UpdateParticipantAsync(int id, Participant participant)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_UpdateParticipant", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ParticipantId", id);
            command.Parameters.AddWithValue("@Name", participant.Name);
            command.Parameters.AddWithValue("@Email", (object?)participant.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@Phone", (object?)participant.Phone ?? DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            return reader.GetInt32("RowsAffected") > 0;
        }

        public async Task<bool> DeleteParticipantAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_DeleteParticipant", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ParticipantId", id);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var rowsAffected = reader.GetInt32("RowsAffected");
            var errorMessage = reader.GetString("ErrorMessage");

            if (!string.IsNullOrEmpty(errorMessage))
                return false;

            return rowsAffected > 0;
        }

        #endregion

        #region Statistics Operations

        public async Task<List<ParticipantStanding>> GetTournamentStandingsAsync(int tournamentId)
        {
            var standings = new List<ParticipantStanding>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetTournamentStandings", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@TournamentId", tournamentId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var standing = new ParticipantStanding
                {
                    ParticipantId = reader.GetInt32("ParticipantId"),
                    ParticipantName = reader.GetString("ParticipantName"),
                    MatchesPlayed = reader.GetInt32("MatchesPlayed"),
                    Wins = reader.GetInt32("Wins"),
                    Draws = reader.GetInt32("Draws"),
                    Losses = reader.GetInt32("Losses"),
                    GoalsFor = reader.GetInt32("GoalsFor"),
                    GoalsAgainst = reader.GetInt32("GoalsAgainst"),
                    Points = reader.GetInt32("Points"),
                    GoalDifference = reader.GetInt32("GoalDifference")
                };

                standings.Add(standing);
            }

            return standings;
        }

        public async Task<ParticipantStatistics> GetParticipantStatisticsAsync(int participantId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetParticipantStatistics", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@ParticipantId", participantId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ParticipantStatistics
                {
                    ParticipantId = reader.GetInt32("ParticipantId"),
                    ParticipantName = reader.GetString("ParticipantName"),
                    TotalTournaments = reader.GetInt32("TotalTournaments"),
                    TournamentsWon = reader.GetInt32("TournamentsWon"),
                    TotalMatches = reader.GetInt32("TotalMatches"),
                    TotalWins = reader.GetInt32("TotalWins"),
                    TotalDraws = reader.GetInt32("TotalDraws"),
                    TotalLosses = reader.GetInt32("TotalLosses"),
                    TotalGoalsScored = reader.GetInt32("TotalGoalsScored"),
                    TotalGoalsConceded = reader.GetInt32("TotalGoalsConceded")
                };
            }

            return new ParticipantStatistics();
        }

        public async Task<List<HeadToHeadStatistics>> GetHeadToHeadStatisticsAsync()
        {
            var headToHeadStats = new List<HeadToHeadStatistics>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GetHeadToHeadStatistics", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var stats = new HeadToHeadStatistics
                {
                    Participant1Id = reader.GetInt32("Participant1Id"),
                    Participant1Name = reader.GetString("Participant1Name"),
                    Participant2Id = reader.GetInt32("Participant2Id"),
                    Participant2Name = reader.GetString("Participant2Name"),
                    TotalMatches = reader.GetInt32("TotalMatches"),
                    Participant1Wins = reader.GetInt32("Participant1Wins"),
                    Participant2Wins = reader.GetInt32("Participant2Wins"),
                    Draws = reader.GetInt32("Draws"),
                    Participant1Goals = reader.GetInt32("Participant1Goals"),
                    Participant2Goals = reader.GetInt32("Participant2Goals")
                };

                headToHeadStats.Add(stats);
            }

            return headToHeadStats;
        }

        public async Task<bool> GeneratePlayoffAsync(int tournamentId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GeneratePlayoff", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@TournamentId", tournamentId);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var success = reader.GetInt32("Success");
            var errorMessage = reader.GetString("ErrorMessage");

            if (!string.IsNullOrEmpty(errorMessage))
                return false;

            return success == 1;
        }

        public async Task<bool> GenerateFinalAsync(int tournamentId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GenerateFinal", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@TournamentId", tournamentId);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var success = reader.GetInt32("Success");
            var errorMessage = reader.GetString("ErrorMessage");

            if (!string.IsNullOrEmpty(errorMessage))
                return false;

            return success == 1;
        }



        public async Task<bool> SetTournamentWinnerAsync(int tournamentId, int winnerId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(@"
                UPDATE Tournaments 
                SET WinnerId = @WinnerId, IsCompleted = 1 
                WHERE Id = @TournamentId", connection);
            command.Parameters.AddWithValue("@TournamentId", tournamentId);
            command.Parameters.AddWithValue("@WinnerId", winnerId);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }

        public async Task<bool> GenerateRandomGroupResultsAsync(int tournamentId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand("sp_GenerateRandomGroupResults", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@TournamentId", tournamentId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var updatedMatches = reader.GetInt32("UpdatedMatches");
                return updatedMatches > 0;
            }

            return false;
        }

        #endregion
    }
} 