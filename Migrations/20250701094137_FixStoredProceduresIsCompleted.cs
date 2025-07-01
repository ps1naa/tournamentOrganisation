using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    public partial class FixStoredProceduresIsCompleted : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_CreateTournamentMatches
    @TournamentId INT,
    @MatchesPerOpponent INT
AS
BEGIN
    DECLARE @ParticipantIds TABLE (Id INT)
    INSERT INTO @ParticipantIds
    SELECT ParticipantId FROM TournamentParticipants WHERE TournamentId = @TournamentId
    
    DECLARE @Round INT = 0
    WHILE @Round < @MatchesPerOpponent
    BEGIN
        INSERT INTO Matches (TournamentId, HomeParticipantId, AwayParticipantId, Type, IsCompleted, CreatedAt)
        SELECT 
            @TournamentId,
            p1.Id,
            p2.Id,
            0, -- Group match
            0, -- IsCompleted = false
            GETDATE()
        FROM @ParticipantIds p1
        CROSS JOIN @ParticipantIds p2
        WHERE p1.Id < p2.Id
        
        SET @Round = @Round + 1
    END
END");


            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE sp_GeneratePlayoff
    @TournamentId INT
AS
BEGIN
    -- Проверяем, не сгенерирован ли уже плей-офф
    IF EXISTS (SELECT 1 FROM Tournaments WHERE Id = @TournamentId AND PlayoffGenerated = 1)
    BEGIN
        SELECT 0 as Success, 'Playoff already generated' as ErrorMessage
        RETURN
    END
    
    -- Получаем топ-4 участников
    CREATE TABLE #TopParticipants (
        ParticipantId INT,
        Position INT
    )
    
    INSERT INTO #TopParticipants
    SELECT TOP 4 
        ParticipantId,
        ROW_NUMBER() OVER (ORDER BY 
            SUM(CASE 
                WHEN (m.HomeParticipantId = tp.ParticipantId AND m.HomeScore > m.AwayScore) OR 
                     (m.AwayParticipantId = tp.ParticipantId AND m.AwayScore > m.HomeScore) 
                THEN 3 
                WHEN m.HomeScore = m.AwayScore THEN 1 
                ELSE 0 END) DESC,
            SUM(CASE WHEN m.HomeParticipantId = tp.ParticipantId THEN ISNULL(m.HomeScore, 0) 
                     ELSE ISNULL(m.AwayScore, 0) END) - 
            SUM(CASE WHEN m.HomeParticipantId = tp.ParticipantId THEN ISNULL(m.AwayScore, 0) 
                     ELSE ISNULL(m.HomeScore, 0) END) DESC,
            SUM(CASE WHEN m.HomeParticipantId = tp.ParticipantId THEN ISNULL(m.HomeScore, 0) 
                     ELSE ISNULL(m.AwayScore, 0) END) DESC
        ) as Position
    FROM TournamentParticipants tp
    LEFT JOIN Matches m ON m.TournamentId = @TournamentId 
        AND (m.HomeParticipantId = tp.ParticipantId OR m.AwayParticipantId = tp.ParticipantId)
        AND m.IsCompleted = 1
    WHERE tp.TournamentId = @TournamentId
    GROUP BY tp.ParticipantId
    
    DECLARE @ParticipantCount INT = (SELECT COUNT(*) FROM #TopParticipants)
    
    IF @ParticipantCount >= 4
    BEGIN
        -- Создаем полуфиналы
        INSERT INTO Matches (TournamentId, HomeParticipantId, AwayParticipantId, Type, IsCompleted, CreatedAt)
        SELECT 
            @TournamentId,
            (SELECT ParticipantId FROM #TopParticipants WHERE Position = 1),
            (SELECT ParticipantId FROM #TopParticipants WHERE Position = 4),
            1, -- Playoff
            0, -- IsCompleted = false
            GETDATE()
        UNION ALL
        SELECT 
            @TournamentId,
            (SELECT ParticipantId FROM #TopParticipants WHERE Position = 2),
            (SELECT ParticipantId FROM #TopParticipants WHERE Position = 3),
            1, -- Playoff
            0, -- IsCompleted = false
            GETDATE()
    END
    ELSE IF @ParticipantCount >= 2
    BEGIN
        -- Создаем финал
        INSERT INTO Matches (TournamentId, HomeParticipantId, AwayParticipantId, Type, IsCompleted, CreatedAt)
        SELECT 
            @TournamentId,
            (SELECT ParticipantId FROM #TopParticipants WHERE Position = 1),
            (SELECT ParticipantId FROM #TopParticipants WHERE Position = 2),
            2, -- Final
            0, -- IsCompleted = false
            GETDATE()
    END
    
    -- Помечаем турнир как имеющий сгенерированный плей-офф
    UPDATE Tournaments SET PlayoffGenerated = 1 WHERE Id = @TournamentId
    
    DROP TABLE #TopParticipants
    
    SELECT 1 as Success, '' as ErrorMessage
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
