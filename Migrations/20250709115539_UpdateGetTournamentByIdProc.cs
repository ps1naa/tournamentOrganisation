using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGetTournamentByIdProc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_GetTournamentById
                    @TournamentId INT
                AS
                BEGIN
                    SELECT 
                        Id, Name, StartDate, EndDate, Description, 
                        MatchesPerOpponent, IsCompleted, PlayoffGenerated, WinnerId, CreatedAt
                    FROM Tournaments
                    WHERE Id = @TournamentId
                    
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
            // Откат не требуется
        }
    }
}
