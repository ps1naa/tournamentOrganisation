using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    /// <inheritdoc />
    public partial class FixPlayoffExclusionAndWinnerLogic : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Обновляем view для турнирной таблицы (исключаем плей-офф и финальные матчи)
            migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW vw_TournamentStandings
                AS
                SELECT 
                    tp.TournamentId,
                    p.Id as ParticipantId,
                    p.Name as ParticipantName,
                    COUNT(m.Id) as MatchesPlayed,
                    SUM(CASE 
                        WHEN (m.HomeParticipantId = p.Id AND m.HomeScore > m.AwayScore) OR 
                             (m.AwayParticipantId = p.Id AND m.AwayScore > m.HomeScore) 
                        THEN 1 ELSE 0 END) as Wins,
                    SUM(CASE WHEN m.HomeScore = m.AwayScore THEN 1 ELSE 0 END) as Draws,
                    SUM(CASE 
                        WHEN (m.HomeParticipantId = p.Id AND m.HomeScore < m.AwayScore) OR 
                             (m.AwayParticipantId = p.Id AND m.AwayScore < m.HomeScore) 
                        THEN 1 ELSE 0 END) as Losses,
                    SUM(CASE WHEN m.HomeParticipantId = p.Id THEN ISNULL(m.HomeScore, 0) 
                             ELSE ISNULL(m.AwayScore, 0) END) as GoalsFor,
                    SUM(CASE WHEN m.HomeParticipantId = p.Id THEN ISNULL(m.AwayScore, 0) 
                             ELSE ISNULL(m.HomeScore, 0) END) as GoalsAgainst,
                    SUM(CASE 
                        WHEN (m.HomeParticipantId = p.Id AND m.HomeScore > m.AwayScore) OR 
                             (m.AwayParticipantId = p.Id AND m.AwayScore > m.HomeScore) 
                        THEN 3 
                        WHEN m.HomeScore = m.AwayScore THEN 1 
                        ELSE 0 END) as Points,
                    (SUM(CASE WHEN m.HomeParticipantId = p.Id THEN ISNULL(m.HomeScore, 0) 
                              ELSE ISNULL(m.AwayScore, 0) END) - 
                     SUM(CASE WHEN m.HomeParticipantId = p.Id THEN ISNULL(m.AwayScore, 0) 
                              ELSE ISNULL(m.HomeScore, 0) END)) as GoalDifference
                FROM TournamentParticipants tp
                INNER JOIN Participants p ON tp.ParticipantId = p.Id
                LEFT JOIN Matches m ON m.TournamentId = tp.TournamentId 
                    AND (m.HomeParticipantId = p.Id OR m.AwayParticipantId = p.Id)
                    AND m.IsCompleted = 1
                    AND m.Type = 0
                GROUP BY tp.TournamentId, p.Id, p.Name
            ");

            // Обновляем процедуру для получения всех турниров с победителями  
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_GetAllTournaments
                AS
                BEGIN
                    SELECT 
                        t.Id, t.Name, t.StartDate, t.EndDate, t.Description, 
                        t.MatchesPerOpponent, t.IsCompleted, t.PlayoffGenerated, t.WinnerId, t.CreatedAt,
                        p.Id as ParticipantId, p.Name as ParticipantName, p.Email as ParticipantEmail, 
                        p.Phone as ParticipantPhone, p.CreatedAt as ParticipantCreatedAt,
                        tp.JoinedAt,
                        pw.Name as WinnerName
                    FROM Tournaments t
                    LEFT JOIN TournamentParticipants tp ON t.Id = tp.TournamentId
                    LEFT JOIN Participants p ON tp.ParticipantId = p.Id
                    LEFT JOIN Participants pw ON t.WinnerId = pw.Id
                    ORDER BY t.CreatedAt DESC
                END
            ");

            // Обновляем процедуру для получения турнира по ID с победителем
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_GetTournamentById
                    @TournamentId INT
                AS
                BEGIN
                    SELECT 
                        t.Id, t.Name, t.StartDate, t.EndDate, t.Description, 
                        t.MatchesPerOpponent, t.IsCompleted, t.PlayoffGenerated, t.WinnerId, t.CreatedAt,
                        pw.Name as WinnerName
                    FROM Tournaments t
                    LEFT JOIN Participants pw ON t.WinnerId = pw.Id
                    WHERE t.Id = @TournamentId
                    
                    SELECT DISTINCT
                        p.Id, p.Name, p.Email, p.Phone, p.CreatedAt, tp.JoinedAt
                    FROM TournamentParticipants tp
                    INNER JOIN Participants p ON tp.ParticipantId = p.Id
                    WHERE tp.TournamentId = @TournamentId
                    
                    SELECT 
                        MatchId as Id, TournamentId, HomeParticipantId, AwayParticipantId,
                        HomeScore, AwayScore, PlayedAt, IsCompleted, Type, MatchCreatedAt as CreatedAt,
                        HomeParticipantName, AwayParticipantName
                    FROM vw_MatchDetails
                    WHERE TournamentId = @TournamentId
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Откат не требуется, так как изменения совместимы
        }
    }
}
