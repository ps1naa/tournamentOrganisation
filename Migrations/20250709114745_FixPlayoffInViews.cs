using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    /// <inheritdoc />
    public partial class FixPlayoffInViews : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Откат не требуется, view будет пересоздан при следующей миграции
        }
    }
}
