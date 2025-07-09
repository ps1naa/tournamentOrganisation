using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    /// <inheritdoc />
    public partial class FixGetTournamentStandingsProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Исправляем процедуру sp_GetTournamentStandings
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_GetTournamentStandings
                    @TournamentId INT
                AS
                BEGIN
                    SELECT 
                        ParticipantId,
                        ParticipantName,
                        MatchesPlayed,
                        Wins,
                        Draws,
                        Losses,
                        GoalsFor,
                        GoalsAgainst,
                        Points,
                        GoalDifference
                    FROM vw_TournamentStandings
                    WHERE TournamentId = @TournamentId
                    ORDER BY Points DESC, GoalDifference DESC, GoalsFor DESC
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Возвращаем старую версию процедуры
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_GetTournamentStandings
                    @TournamentId INT
                AS
                BEGIN
                    SELECT 
                        ParticipantId,
                        ParticipantName,
                        MatchesPlayed,
                        Wins,
                        Draws,
                        Losses,
                        GoalsFor,
                        GoalsAgainst
                    FROM vw_TournamentStandings
                    WHERE TournamentId = @TournamentId
                    ORDER BY Points DESC, GoalDifference DESC, GoalsFor DESC
                END
            ");
        }
    }
}
