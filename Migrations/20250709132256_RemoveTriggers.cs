using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'tr_AutoCompleteTournament')
                BEGIN
                    DROP TRIGGER tr_AutoCompleteTournament;
                END
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'tr_DebugTournamentCompletion')
                BEGIN
                    DROP TRIGGER tr_DebugTournamentCompletion;
                END
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_AutoCompleteTournament
                    @TournamentId INT
                AS
                BEGIN
                    PRINT 'sp_AutoCompleteTournament is deprecated. Use C# CompleteTournamentAsync instead.'
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TRIGGER tr_AutoCompleteTournament
                ON Matches
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    IF EXISTS (
                        SELECT 1 
                        FROM inserted i
                        INNER JOIN deleted d ON i.Id = d.Id
                        WHERE i.Type = 2 
                            AND i.IsCompleted = 1 
                            AND d.IsCompleted = 0
                            AND i.HomeScore IS NOT NULL 
                            AND i.AwayScore IS NOT NULL
                    )
                    BEGIN
                        DECLARE @TournamentId INT;
                        DECLARE tournament_cursor CURSOR FOR
                        SELECT DISTINCT i.TournamentId
                        FROM inserted i
                        INNER JOIN deleted d ON i.Id = d.Id
                        WHERE i.Type = 2 
                            AND i.IsCompleted = 1 
                            AND d.IsCompleted = 0;
                        
                        OPEN tournament_cursor;
                        FETCH NEXT FROM tournament_cursor INTO @TournamentId;
                        
                        WHILE @@FETCH_STATUS = 0
                        BEGIN
                            EXEC sp_AutoCompleteTournament @TournamentId;
                            FETCH NEXT FROM tournament_cursor INTO @TournamentId;
                        END
                        
                        CLOSE tournament_cursor;
                        DEALLOCATE tournament_cursor;
                    END
                END
            ");
        }
    }
}
