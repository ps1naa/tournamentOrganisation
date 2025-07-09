using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    /// <inheritdoc />
    public partial class CreateTournamentCompletionTrigger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Убеждаемся, что view правильно исключает плей-офф и финальные матчи
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

            // Создаем процедуру для автоматического завершения турнира
            migrationBuilder.Sql(@"
                CREATE OR ALTER PROCEDURE sp_AutoCompleteTournament
                    @TournamentId INT
                AS
                BEGIN
                    DECLARE @WinnerId INT = NULL;
                    
                    -- Сначала проверяем финальный матч
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
                    
                    -- Если финального матча нет или ничья, берем лидера по очкам
                    IF @WinnerId IS NULL
                    BEGIN
                        SELECT TOP 1 @WinnerId = ParticipantId 
                        FROM vw_TournamentStandings 
                        WHERE TournamentId = @TournamentId 
                        ORDER BY Points DESC, GoalDifference DESC, GoalsFor DESC;
                    END
                    
                    -- Обновляем турнир
                    UPDATE Tournaments 
                    SET IsCompleted = 1, WinnerId = @WinnerId 
                    WHERE Id = @TournamentId AND IsCompleted = 0;
                END
            ");

            // Создаем триггер для автоматического завершения турнира при завершении финального матча
            migrationBuilder.Sql(@"
                CREATE OR ALTER TRIGGER tr_AutoCompleteTournament
                ON Matches
                AFTER UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    -- Проверяем, есть ли завершенные финальные матчи в обновленных записях
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
                        -- Для каждого турнира с завершенным финалом
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Удаляем триггер
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS tr_AutoCompleteTournament;
            ");

            // Удаляем процедуру
            migrationBuilder.Sql(@"
                DROP PROCEDURE IF EXISTS sp_AutoCompleteTournament;
            ");
        }
    }
}
