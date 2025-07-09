using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGetAllTournamentsProc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_GetAllTournaments
                AS
                BEGIN
                    SELECT 
                        t.Id, t.Name, t.StartDate, t.EndDate, t.Description, 
                        t.MatchesPerOpponent, t.IsCompleted, t.PlayoffGenerated, t.WinnerId, t.CreatedAt,
                        p.Id as ParticipantId, p.Name as ParticipantName, p.Email as ParticipantEmail, 
                        p.Phone as ParticipantPhone, p.CreatedAt as ParticipantCreatedAt,
                        tp.JoinedAt
                    FROM Tournaments t
                    LEFT JOIN TournamentParticipants tp ON t.Id = tp.TournamentId
                    LEFT JOIN Participants p ON tp.ParticipantId = p.Id
                    ORDER BY t.CreatedAt DESC
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Откат не требуется
        }
    }
}
