using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    public partial class AddGenerateFinalProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Создать финал между победителями полуфиналов
                CREATE OR ALTER PROCEDURE sp_GenerateFinal
                    @TournamentId INT
                AS
                BEGIN
                    -- Проверяем, есть ли уже финал в турнире
                    IF EXISTS (SELECT 1 FROM Matches WHERE TournamentId = @TournamentId AND Type = 2) -- Final
                    BEGIN
                        SELECT 0 as Success, 'Final already exists' as ErrorMessage
                        RETURN
                    END
                    
                    -- Получаем всех полуфинальных матчей
                    CREATE TABLE #PlayoffMatches (
                        MatchId INT,
                        HomeParticipantId INT,
                        AwayParticipantId INT,
                        HomeScore INT,
                        AwayScore INT,
                        IsCompleted BIT
                    )
                    
                    INSERT INTO #PlayoffMatches
                    SELECT Id, HomeParticipantId, AwayParticipantId, HomeScore, AwayScore, IsCompleted
                    FROM Matches 
                    WHERE TournamentId = @TournamentId 
                    AND Type = 1 -- Playoff
                    
                    -- Проверяем, что все полуфинальные матчи завершены
                    IF EXISTS (SELECT 1 FROM #PlayoffMatches WHERE IsCompleted = 0)
                    BEGIN
                        SELECT 0 as Success, 'Not all playoff matches are completed' as ErrorMessage
                        DROP TABLE #PlayoffMatches
                        RETURN
                    END
                    
                    -- Получаем количество полуфинальных матчей
                    DECLARE @PlayoffCount INT = (SELECT COUNT(*) FROM #PlayoffMatches)
                    
                    IF @PlayoffCount = 2
                    BEGIN
                        -- Получаем победителей каждого полуфинала
                        CREATE TABLE #Winners (
                            ParticipantId INT
                        )
                        
                        INSERT INTO #Winners
                        SELECT 
                            CASE 
                                WHEN HomeScore > AwayScore THEN HomeParticipantId
                                WHEN AwayScore > HomeScore THEN AwayParticipantId
                                ELSE NULL -- В случае ничьи не добавляем
                            END
                        FROM #PlayoffMatches
                        WHERE HomeScore IS NOT NULL AND AwayScore IS NOT NULL
                        AND HomeScore <> AwayScore -- Исключаем ничьи
                        
                        -- Проверяем, что у нас есть ровно 2 победителя
                        DECLARE @WinnerCount INT = (SELECT COUNT(*) FROM #Winners WHERE ParticipantId IS NOT NULL)
                        
                        IF @WinnerCount = 2
                        BEGIN
                            -- Создаем финальный матч
                            DECLARE @Winner1 INT, @Winner2 INT
                            
                            SELECT @Winner1 = MIN(ParticipantId), @Winner2 = MAX(ParticipantId) 
                            FROM #Winners
                            
                            INSERT INTO Matches (TournamentId, HomeParticipantId, AwayParticipantId, Type, IsCompleted, CreatedAt)
                            VALUES (@TournamentId, @Winner1, @Winner2, 2, 0, GETDATE()) -- Final
                            
                            SELECT 1 as Success, '' as ErrorMessage
                        END
                        ELSE
                        BEGIN
                            SELECT 0 as Success, 'Cannot determine winners from playoffs' as ErrorMessage
                        END
                        
                        DROP TABLE #Winners
                    END
                    ELSE
                    BEGIN
                        SELECT 0 as Success, 'Invalid number of playoff matches' as ErrorMessage
                    END
                    
                    DROP TABLE #PlayoffMatches
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP PROCEDURE IF EXISTS sp_GenerateFinal");
        }
    }
}
