using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentsWonStatistics : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW vw_ParticipantStatistics
                AS
                SELECT 
                    p.Id as ParticipantId,
                    p.Name as ParticipantName,
                    p.Email,
                    p.Phone,
                    p.CreatedAt,
                    COUNT(DISTINCT tp.TournamentId) as TotalTournaments,
                    COUNT(DISTINCT CASE WHEN t.WinnerId = p.Id THEN t.Id END) as TournamentsWon,
                    COUNT(m.Id) as TotalMatches,
                    SUM(CASE 
                        WHEN (m.HomeParticipantId = p.Id AND m.HomeScore > m.AwayScore) OR 
                             (m.AwayParticipantId = p.Id AND m.AwayScore > m.HomeScore) 
                        THEN 1 ELSE 0 END) as TotalWins,
                    SUM(CASE WHEN m.HomeScore = m.AwayScore THEN 1 ELSE 0 END) as TotalDraws,
                    SUM(CASE 
                        WHEN (m.HomeParticipantId = p.Id AND m.HomeScore < m.AwayScore) OR 
                             (m.AwayParticipantId = p.Id AND m.AwayScore < m.HomeScore) 
                        THEN 1 ELSE 0 END) as TotalLosses,
                    SUM(CASE WHEN m.HomeParticipantId = p.Id THEN ISNULL(m.HomeScore, 0) 
                             ELSE ISNULL(m.AwayScore, 0) END) as TotalGoalsScored,
                    SUM(CASE WHEN m.HomeParticipantId = p.Id THEN ISNULL(m.AwayScore, 0) 
                             ELSE ISNULL(m.HomeScore, 0) END) as TotalGoalsConceded,
                    SUM(CASE 
                        WHEN (m.HomeParticipantId = p.Id AND m.HomeScore > m.AwayScore) OR 
                             (m.AwayParticipantId = p.Id AND m.AwayScore > m.HomeScore) 
                        THEN 3 
                        WHEN m.HomeScore = m.AwayScore THEN 1 
                        ELSE 0 END) as TotalPoints
                FROM Participants p
                LEFT JOIN TournamentParticipants tp ON p.Id = tp.ParticipantId
                LEFT JOIN Tournaments t ON tp.TournamentId = t.Id
                LEFT JOIN Matches m ON (m.HomeParticipantId = p.Id OR m.AwayParticipantId = p.Id)
                    AND m.IsCompleted = 1
                GROUP BY p.Id, p.Name, p.Email, p.Phone, p.CreatedAt
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_GetParticipantStatistics
                    @ParticipantId INT
                AS
                BEGIN
                    SELECT 
                        ParticipantId,
                        ParticipantName,
                        TotalTournaments,
                        TournamentsWon,
                        TotalMatches,
                        TotalWins,
                        TotalDraws,
                        TotalLosses,
                        TotalGoalsScored,
                        TotalGoalsConceded
                    FROM vw_ParticipantStatistics
                    WHERE ParticipantId = @ParticipantId
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Откат не требуется
        }
    }
}
