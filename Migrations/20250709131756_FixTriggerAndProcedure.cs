using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    /// <inheritdoc />
    public partial class FixTriggerAndProcedure : Migration
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
                CREATE OR ALTER PROCEDURE sp_AutoCompleteTournament
                    @TournamentId INT
                AS
                BEGIN
                    DECLARE @WinnerId INT = NULL;
                    DECLARE @IsCompleted BIT = 0;
                    
                    SELECT @IsCompleted = IsCompleted FROM Tournaments WHERE Id = @TournamentId;
                    IF @IsCompleted = 1 RETURN;
                    
                    SELECT @WinnerId = CASE 
                        WHEN HomeScore > AwayScore THEN HomeParticipantId
                        WHEN AwayScore > HomeScore THEN AwayParticipantId
                        ELSE NULL
                    END
                    FROM Matches 
                    WHERE TournamentId = @TournamentId 
                        AND Type = 2 
                        AND IsCompleted = 1 
                        AND HomeScore IS NOT NULL 
                        AND AwayScore IS NOT NULL
                        AND HomeScore <> AwayScore;
                    
                    IF @WinnerId IS NULL
                    BEGIN
                        SELECT TOP 1 @WinnerId = ParticipantId 
                        FROM vw_TournamentStandings 
                        WHERE TournamentId = @TournamentId 
                        ORDER BY Points DESC, GoalDifference DESC, GoalsFor DESC;
                    END
                    
                    IF @WinnerId IS NOT NULL
                    BEGIN
                        UPDATE Tournaments 
                        SET IsCompleted = 1, WinnerId = @WinnerId 
                        WHERE Id = @TournamentId;
                    END
                END
            ");
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

            migrationBuilder.Sql(@"
                CREATE TRIGGER tr_DebugTournamentCompletion
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
                    )
                    BEGIN
                        PRINT 'Final match completed - trigger fired';
                    END
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS tr_AutoCompleteTournament;
                DROP TRIGGER IF EXISTS tr_DebugTournamentCompletion;
            ");
        }
    }
}
