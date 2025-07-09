using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    /// <inheritdoc />
    public partial class AddRandomGroupResultsProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_GenerateRandomGroupResults
                    @TournamentId INT
                AS
                BEGIN
                    DECLARE @MatchId INT;
                    DECLARE @HomeScore INT;
                    DECLARE @AwayScore INT;
                    DECLARE @UpdatedCount INT = 0;
                    
                    DECLARE match_cursor CURSOR FOR
                    SELECT Id
                    FROM Matches
                    WHERE TournamentId = @TournamentId 
                        AND Type = 0 
                        AND IsCompleted = 0;
                    
                    OPEN match_cursor;
                    FETCH NEXT FROM match_cursor INTO @MatchId;
                    
                    WHILE @@FETCH_STATUS = 0
                    BEGIN
                        SET @HomeScore = ABS(CHECKSUM(NEWID()) % 6);
                        SET @AwayScore = ABS(CHECKSUM(NEWID()) % 6);
                        
                        UPDATE Matches 
                        SET HomeScore = @HomeScore,
                            AwayScore = @AwayScore,
                            IsCompleted = 1,
                            PlayedAt = GETDATE()
                        WHERE Id = @MatchId;
                        
                        SET @UpdatedCount = @UpdatedCount + 1;
                        
                        FETCH NEXT FROM match_cursor INTO @MatchId;
                    END
                    
                    CLOSE match_cursor;
                    DEALLOCATE match_cursor;
                    
                    SELECT @UpdatedCount as UpdatedMatches;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP PROCEDURE IF EXISTS sp_GenerateRandomGroupResults;
            ");
        }
    }
}
