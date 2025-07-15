
CREATE OR ALTER PROCEDURE sp_GetAllTournaments
AS
BEGIN
    SELECT 
        t.Id, t.Name, t.StartDate, t.EndDate, t.Description, 
        t.MatchesPerOpponent, t.IsCompleted, t.PlayoffGenerated, t.WinnerId, t.CreatedAt,
        p.Id as ParticipantId, p.Name as ParticipantName, p.Email as ParticipantEmail, 
        p.Phone as ParticipantPhone, p.CreatedAt as ParticipantCreatedAt,
        tp.JoinedAt,
        pw.Name as WinnerName
    FROM Tournaments t
    LEFT JOIN TournamentParticipants tp ON t.Id = tp.TournamentId
    LEFT JOIN Participants p ON tp.ParticipantId = p.Id
    LEFT JOIN Participants pw ON t.WinnerId = pw.Id
    ORDER BY t.CreatedAt DESC
END
GO


CREATE OR ALTER PROCEDURE sp_GetTournamentById
    @TournamentId INT
AS
BEGIN

    SELECT 
        t.Id, t.Name, t.StartDate, t.EndDate, t.Description, 
        t.MatchesPerOpponent, t.IsCompleted, t.PlayoffGenerated, t.WinnerId, t.CreatedAt,
        pw.Name as WinnerName
    FROM Tournaments t
    LEFT JOIN Participants pw ON t.WinnerId = pw.Id
    WHERE t.Id = @TournamentId
    

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
GO


CREATE OR ALTER PROCEDURE sp_CreateTournament
    @Name NVARCHAR(100),
    @StartDate DATETIME,
    @EndDate DATETIME = NULL,
    @Description NVARCHAR(MAX) = NULL,
    @MatchesPerOpponent INT = 1
AS
BEGIN
    DECLARE @TournamentId INT
    
    INSERT INTO Tournaments (Name, StartDate, EndDate, Description, MatchesPerOpponent, IsCompleted, PlayoffGenerated, CreatedAt)
    VALUES (@Name, @StartDate, @EndDate, @Description, @MatchesPerOpponent, 0, 0, GETDATE())
    
    SET @TournamentId = SCOPE_IDENTITY()
    
    SELECT @TournamentId as TournamentId
END
GO


CREATE OR ALTER PROCEDURE sp_AddTournamentParticipants
    @TournamentId INT,
    @ParticipantIds NVARCHAR(MAX)
AS
BEGIN
    DECLARE @ParticipantId INT
    DECLARE @Pos INT = 1
    DECLARE @NextPos INT
    
    WHILE @Pos <= LEN(@ParticipantIds)
    BEGIN
        SET @NextPos = CHARINDEX(',', @ParticipantIds, @Pos)
        IF @NextPos = 0
            SET @NextPos = LEN(@ParticipantIds) + 1
            
        SET @ParticipantId = CAST(SUBSTRING(@ParticipantIds, @Pos, @NextPos - @Pos) AS INT)
        
        INSERT INTO TournamentParticipants (TournamentId, ParticipantId, JoinedAt)
        VALUES (@TournamentId, @ParticipantId, GETDATE())
        
        SET @Pos = @NextPos + 1
    END
END
GO


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
            0,
            0,
            GETDATE()
        FROM @ParticipantIds p1
        CROSS JOIN @ParticipantIds p2
        WHERE p1.Id < p2.Id
        
        SET @Round = @Round + 1
    END
END
GO


CREATE OR ALTER PROCEDURE sp_UpdateTournament
    @TournamentId INT,
    @Name NVARCHAR(100),
    @StartDate DATETIME,
    @EndDate DATETIME = NULL,
    @Description NVARCHAR(MAX) = NULL
AS
BEGIN
    UPDATE Tournaments 
    SET Name = @Name, 
        StartDate = @StartDate, 
        EndDate = @EndDate, 
        Description = @Description
    WHERE Id = @TournamentId
    
    SELECT @@ROWCOUNT as RowsAffected
END
GO


CREATE OR ALTER PROCEDURE sp_DeleteTournament
    @TournamentId INT
AS
BEGIN
    DECLARE @RowsAffected INT = 0
    

    IF EXISTS (SELECT 1 FROM Tournaments WHERE Id = @TournamentId)
    BEGIN
        BEGIN TRANSACTION
        

        DELETE FROM Matches WHERE TournamentId = @TournamentId
        

        DELETE FROM TournamentParticipants WHERE TournamentId = @TournamentId
        

        DELETE FROM Tournaments WHERE Id = @TournamentId
        SET @RowsAffected = @@ROWCOUNT
        
        IF @@ERROR = 0
            COMMIT TRANSACTION
        ELSE
        BEGIN
            ROLLBACK TRANSACTION
            SET @RowsAffected = 0
        END
    END
    
    SELECT @RowsAffected as RowsAffected
END
GO


CREATE OR ALTER PROCEDURE sp_GetTournamentMatches
    @TournamentId INT
AS
BEGIN
    SELECT 
        MatchId as Id, TournamentId, HomeParticipantId, AwayParticipantId,
        HomeScore, AwayScore, PlayedAt, IsCompleted, Type, MatchCreatedAt as CreatedAt,
        HomeParticipantName, AwayParticipantName
    FROM vw_MatchDetails
    WHERE TournamentId = @TournamentId
    ORDER BY MatchCreatedAt
END
GO


CREATE OR ALTER PROCEDURE sp_GetMatchById
    @MatchId INT
AS
BEGIN
    SELECT 
        MatchId as Id, TournamentId, HomeParticipantId, AwayParticipantId,
        HomeScore, AwayScore, PlayedAt, IsCompleted, Type, MatchCreatedAt as CreatedAt,
        HomeParticipantName, AwayParticipantName,
        TournamentName
    FROM vw_MatchDetails
    WHERE MatchId = @MatchId
END
GO


CREATE OR ALTER PROCEDURE sp_UpdateMatchResult
    @MatchId INT,
    @HomeScore INT = NULL,
    @AwayScore INT = NULL,
    @IsCompleted BIT
AS
BEGIN
    UPDATE Matches 
    SET HomeScore = @HomeScore,
        AwayScore = @AwayScore,
        IsCompleted = @IsCompleted,
        PlayedAt = CASE WHEN @IsCompleted = 1 THEN GETDATE() ELSE NULL END
    WHERE Id = @MatchId
    
    SELECT @@ROWCOUNT as RowsAffected
END
GO


CREATE OR ALTER PROCEDURE sp_GetAllParticipants
AS
BEGIN
    SELECT Id, Name, Email, Phone, CreatedAt
    FROM Participants
    ORDER BY Name
END
GO


CREATE OR ALTER PROCEDURE sp_CreateParticipant
    @Name NVARCHAR(50),
    @Email NVARCHAR(100) = NULL,
    @Phone NVARCHAR(20) = NULL
AS
BEGIN
    DECLARE @ParticipantId INT
    
    INSERT INTO Participants (Name, Email, Phone, CreatedAt)
    VALUES (@Name, @Email, @Phone, GETDATE())
    
    SET @ParticipantId = SCOPE_IDENTITY()
    
    SELECT @ParticipantId as ParticipantId
END
GO


CREATE OR ALTER PROCEDURE sp_GetParticipantById
    @ParticipantId INT
AS
BEGIN
    SELECT Id, Name, Email, Phone, CreatedAt
    FROM Participants
    WHERE Id = @ParticipantId
END
GO


CREATE OR ALTER PROCEDURE sp_UpdateParticipant
    @ParticipantId INT,
    @Name NVARCHAR(50),
    @Email NVARCHAR(100) = NULL,
    @Phone NVARCHAR(20) = NULL
AS
BEGIN
    UPDATE Participants 
    SET Name = @Name, Email = @Email, Phone = @Phone
    WHERE Id = @ParticipantId
    
    SELECT @@ROWCOUNT as RowsAffected
END
GO


CREATE OR ALTER PROCEDURE sp_DeleteParticipant
    @ParticipantId INT
AS
BEGIN

    IF EXISTS (
        SELECT 1 FROM Matches 
        WHERE (HomeParticipantId = @ParticipantId OR AwayParticipantId = @ParticipantId) 
        AND IsCompleted = 1
    )
    BEGIN
        SELECT 0 as RowsAffected, 'Participant has completed matches' as ErrorMessage
        RETURN
    END
    
    DELETE FROM Participants WHERE Id = @ParticipantId
    
    SELECT @@ROWCOUNT as RowsAffected, '' as ErrorMessage
END
GO


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
GO


CREATE OR ALTER PROCEDURE sp_GetParticipantStatistics
    @ParticipantId INT
AS
BEGIN
    SELECT 
        ParticipantId,
        ParticipantName,
        TotalTournaments,
        TournamentsWon,
        TotalMatches,
        TotalWins,
        TotalDraws,
        TotalLosses,
        TotalGoalsScored,
        TotalGoalsConceded
    FROM vw_ParticipantStatistics
    WHERE ParticipantId = @ParticipantId
END
GO


CREATE OR ALTER PROCEDURE sp_GetHeadToHeadStatistics
AS
BEGIN
    SELECT 
        Participant1Id,
        Participant1Name,
        Participant2Id,
        Participant2Name,
        TotalMatches,
        Participant1Wins,
        Participant2Wins,
        Draws,
        Participant1Goals,
        Participant2Goals
    FROM vw_HeadToHeadStatistics
END
GO


CREATE OR ALTER PROCEDURE sp_GeneratePlayoff
    @TournamentId INT
AS
BEGIN

    IF EXISTS (SELECT 1 FROM Tournaments WHERE Id = @TournamentId AND PlayoffGenerated = 1)
    BEGIN
        SELECT 0 as Success, 'Playoff already generated' as ErrorMessage
        RETURN
    END
    

    CREATE TABLE #TopParticipants (
        ParticipantId INT,
        Position INT
    )
    
    INSERT INTO #TopParticipants
    SELECT TOP 4 
        ParticipantId,
        ROW_NUMBER() OVER (ORDER BY Points DESC, GoalDifference DESC, GoalsFor DESC) as Position
    FROM vw_TournamentStandings
    WHERE TournamentId = @TournamentId
    
    DECLARE @ParticipantCount INT = (SELECT COUNT(*) FROM #TopParticipants)
    
    IF @ParticipantCount >= 4
    BEGIN

        INSERT INTO Matches (TournamentId, HomeParticipantId, AwayParticipantId, Type, IsCompleted, CreatedAt)
        SELECT 
            @TournamentId,
            (SELECT ParticipantId FROM #TopParticipants WHERE Position = 1),
            (SELECT ParticipantId FROM #TopParticipants WHERE Position = 4),
            1,
            0,
            GETDATE()
        UNION ALL
        SELECT 
            @TournamentId,
            (SELECT ParticipantId FROM #TopParticipants WHERE Position = 2),
            (SELECT ParticipantId FROM #TopParticipants WHERE Position = 3),
            1,
            0,
            GETDATE()
    END
    ELSE IF @ParticipantCount >= 2
    BEGIN
        INSERT INTO Matches (TournamentId, HomeParticipantId, AwayParticipantId, Type, IsCompleted, CreatedAt)
        SELECT 
            @TournamentId,
            (SELECT ParticipantId FROM #TopParticipants WHERE Position = 1),
            (SELECT ParticipantId FROM #TopParticipants WHERE Position = 2),
            2,
            0,
            GETDATE()
    END
    

    UPDATE Tournaments SET PlayoffGenerated = 1 WHERE Id = @TournamentId
    
    DROP TABLE #TopParticipants
    
    SELECT 1 as Success, '' as ErrorMessage
END
GO


CREATE OR ALTER PROCEDURE sp_GenerateFinal
    @TournamentId INT
AS
BEGIN

    IF EXISTS (SELECT 1 FROM Matches WHERE TournamentId = @TournamentId AND Type = 2)
    BEGIN
        SELECT 0 as Success, 'Final already exists' as ErrorMessage
        RETURN
    END
    

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
    AND Type = 1
    

    IF EXISTS (SELECT 1 FROM #PlayoffMatches WHERE IsCompleted = 0)
    BEGIN
        SELECT 0 as Success, 'Not all playoff matches are completed' as ErrorMessage
        DROP TABLE #PlayoffMatches
        RETURN
    END
    

    DECLARE @PlayoffCount INT = (SELECT COUNT(*) FROM #PlayoffMatches)
    
    IF @PlayoffCount = 2
    BEGIN

        CREATE TABLE #Winners (
            ParticipantId INT
        )
        
        INSERT INTO #Winners
        SELECT WinnerId
        FROM vw_PlayoffWinners
        WHERE TournamentId = @TournamentId
        

        DECLARE @WinnerCount INT = (SELECT COUNT(*) FROM #Winners WHERE ParticipantId IS NOT NULL)
        
        IF @WinnerCount = 2
        BEGIN

            DECLARE @Winner1 INT, @Winner2 INT
            
            SELECT @Winner1 = MIN(ParticipantId), @Winner2 = MAX(ParticipantId) 
            FROM #Winners
            
            INSERT INTO Matches (TournamentId, HomeParticipantId, AwayParticipantId, Type, IsCompleted, CreatedAt)
            VALUES (@TournamentId, @Winner1, @Winner2, 2, 0, GETDATE())
            
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
GO 


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
GO


CREATE OR ALTER TRIGGER tr_AutoCompleteTournament
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
GO 


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
GO 