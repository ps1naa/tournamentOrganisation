using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    public partial class AddStoredProcedures : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            var projectPath = Directory.GetCurrentDirectory();
            var scriptPath = Path.Combine(projectPath, "Scripts", "StoredProcedures.sql");
            var sqlScript = File.ReadAllText(scriptPath);
            

            var batches = sqlScript.Split(new[] { "\nGO\n", "\nGO\r\n", "\rGO\r", "GO" }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var batch in batches)
            {
                if (!string.IsNullOrWhiteSpace(batch))
                {
                    migrationBuilder.Sql(batch.Trim());
                }
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

            var procedureNames = new[]
            {
                "sp_GetAllTournaments",
                "sp_GetTournamentById", 
                "sp_CreateTournament",
                "sp_AddTournamentParticipants",
                "sp_CreateTournamentMatches",
                "sp_UpdateTournament",
                "sp_DeleteTournament",
                "sp_GetTournamentMatches",
                "sp_GetMatchById",
                "sp_UpdateMatchResult",
                "sp_GetAllParticipants",
                "sp_CreateParticipant",
                "sp_GetParticipantById",
                "sp_UpdateParticipant",
                "sp_DeleteParticipant",
                "sp_GetTournamentStandings",
                "sp_GetParticipantStatistics",
                "sp_GetHeadToHeadStatistics",
                "sp_GeneratePlayoff"
            };

            foreach (var procedureName in procedureNames)
            {
                migrationBuilder.Sql($"DROP PROCEDURE IF EXISTS {procedureName}");
            }
        }
    }
}
