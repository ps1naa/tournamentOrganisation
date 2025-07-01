
CREATE OR ALTER PROCEDURE sp_GetAllTournaments
AS
BEGIN
    SELECT 
        t.Id, t.Name, t.StartDate, t.EndDate, t.Description, 
        t.MatchesPerOpponent, t.IsCompleted, t.PlayoffGenerated, t.CreatedAt,
        p.Id as ParticipantId, p.Name as ParticipantName, p.Email as ParticipantEmail, 
        p.Phone as ParticipantPhone, p.CreatedAt as ParticipantCreatedAt,
        tp.JoinedAt
    FROM Tournaments t
    LEFT JOIN TournamentParticipants tp ON t.Id = tp.TournamentId
    LEFT JOIN Participants p ON tp.ParticipantId = p.Id
    ORDER BY t.CreatedAt DESC
END
GO


CREATE OR ALTER PROCEDURE sp_GetTournamentById
    @TournamentId INT
AS
BEGIN

    SELECT 
        t.Id, t.Name, t.StartDate, t.EndDate, t.Description, 
        t.MatchesPerOpponent, t.IsCompleted, t.PlayoffGenerated, t.CreatedAt
    FROM Tournaments t
    WHERE t.Id = @TournamentId
    

    SELECT 
        p.Id, p.Name, p.Email, p.Phone, p.CreatedAt,
        tp.JoinedAt
    FROM TournamentParticipants tp
    INNER JOIN Participants p ON tp.ParticipantId = p.Id
    WHERE tp.TournamentId = @TournamentId
    

    SELECT 
        m.Id, m.TournamentId, m.HomeParticipantId, m.AwayParticipantId,
        m.HomeScore, m.AwayScore, m.PlayedAt, m.IsCompleted, m.Type, m.CreatedAt,
        hp.Name as HomeParticipantName, ap.Name as AwayParticipantName
    FROM Matches m
    INNER JOIN Participants hp ON m.HomeParticipantId = hp.Id
    INNER JOIN Participants ap ON m.AwayParticipantId = ap.Id
    WHERE m.TournamentId = @TournamentId
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
        m.Id, m.TournamentId, m.HomeParticipantId, m.AwayParticipantId,
        m.HomeScore, m.AwayScore, m.PlayedAt, m.IsCompleted, m.Type, m.CreatedAt,
        hp.Name as HomeParticipantName, ap.Name as AwayParticipantName
    FROM Matches m
    INNER JOIN Participants hp ON m.HomeParticipantId = hp.Id
    INNER JOIN Participants ap ON m.AwayParticipantId = ap.Id
    WHERE m.TournamentId = @TournamentId
    ORDER BY m.CreatedAt
END
GO


CREATE OR ALTER PROCEDURE sp_GetMatchById
    @MatchId INT
AS
BEGIN
    SELECT 
        m.Id, m.TournamentId, m.HomeParticipantId, m.AwayParticipantId,
        m.HomeScore, m.AwayScore, m.PlayedAt, m.IsCompleted, m.Type, m.CreatedAt,
        hp.Name as HomeParticipantName, ap.Name as AwayParticipantName,
        t.Name as TournamentName
    FROM Matches m
    INNER JOIN Participants hp ON m.HomeParticipantId = hp.Id
    INNER JOIN Participants ap ON m.AwayParticipantId = ap.Id
    INNER JOIN Tournaments t ON m.TournamentId = t.Id
    WHERE m.Id = @MatchId
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
                 ELSE ISNULL(m.HomeScore, 0) END) as GoalsAgainst
    FROM TournamentParticipants tp
    INNER JOIN Participants p ON tp.ParticipantId = p.Id
    LEFT JOIN Matches m ON m.TournamentId = @TournamentId 
        AND (m.HomeParticipantId = p.Id OR m.AwayParticipantId = p.Id)
        AND m.IsCompleted = 1
    WHERE tp.TournamentId = @TournamentId
    GROUP BY p.Id, p.Name
    ORDER BY 
        (SUM(CASE 
            WHEN (m.HomeParticipantId = p.Id AND m.HomeScore > m.AwayScore) OR 
                 (m.AwayParticipantId = p.Id AND m.AwayScore > m.HomeScore) 
            THEN 3 
            WHEN m.HomeScore = m.AwayScore THEN 1 
            ELSE 0 END)) DESC,
        (SUM(CASE WHEN m.HomeParticipantId = p.Id THEN ISNULL(m.HomeScore, 0) 
                  ELSE ISNULL(m.AwayScore, 0) END) - 
         SUM(CASE WHEN m.HomeParticipantId = p.Id THEN ISNULL(m.AwayScore, 0) 
                  ELSE ISNULL(m.HomeScore, 0) END)) DESC,
        SUM(CASE WHEN m.HomeParticipantId = p.Id THEN ISNULL(m.HomeScore, 0) 
                 ELSE ISNULL(m.AwayScore, 0) END) DESC
END
GO


CREATE OR ALTER PROCEDURE sp_GetParticipantStatistics
    @ParticipantId INT
AS
BEGIN
    SELECT 
        p.Id as ParticipantId,
        p.Name as ParticipantName,
        COUNT(DISTINCT tp.TournamentId) as TotalTournaments,
        COUNT(m.Id) as TotalMatches,
        SUM(CASE 
            WHEN (m.HomeParticipantId = p.Id AND m.HomeScore > m.AwayScore) OR 
                 (m.AwayParticipantId = p.Id AND m.AwayScore > m.HomeScore) 
            THEN 1 ELSE 0 END) as TotalWins,
        SUM(CASE WHEN m.HomeScore = m.AwayScore THEN 1 ELSE 0 END) as TotalDraws,
        SUM(CASE 
            WHEN (m.HomeParticipantId = p.Id AND m.HomeScore < m.AwayScore) OR 
                 (m.AwayParticipantId = p.Id AND m.AwayScore < m.HomeScore) 
            THEN 1 ELSE 0 END) as TotalLosses,
        SUM(CASE WHEN m.HomeParticipantId = p.Id THEN ISNULL(m.HomeScore, 0) 
                 ELSE ISNULL(m.AwayScore, 0) END) as TotalGoalsScored,
        SUM(CASE WHEN m.HomeParticipantId = p.Id THEN ISNULL(m.AwayScore, 0) 
                 ELSE ISNULL(m.HomeScore, 0) END) as TotalGoalsConceded
    FROM Participants p
    LEFT JOIN TournamentParticipants tp ON p.Id = tp.ParticipantId
    LEFT JOIN Matches m ON (m.HomeParticipantId = p.Id OR m.AwayParticipantId = p.Id)
        AND m.IsCompleted = 1
    WHERE p.Id = @ParticipantId
    GROUP BY p.Id, p.Name
END
GO


CREATE OR ALTER PROCEDURE sp_GetHeadToHeadStatistics
AS
BEGIN
    SELECT 
        p1.Id as Participant1Id,
        p1.Name as Participant1Name,
        p2.Id as Participant2Id,
        p2.Name as Participant2Name,
        COUNT(m.Id) as TotalMatches,
        SUM(CASE 
            WHEN (m.HomeParticipantId = p1.Id AND m.HomeScore > m.AwayScore) OR 
                 (m.AwayParticipantId = p1.Id AND m.AwayScore > m.HomeScore) 
            THEN 1 ELSE 0 END) as Participant1Wins,
        SUM(CASE 
            WHEN (m.HomeParticipantId = p2.Id AND m.HomeScore > m.AwayScore) OR 
                 (m.AwayParticipantId = p2.Id AND m.AwayScore > m.HomeScore) 
            THEN 1 ELSE 0 END) as Participant2Wins,
        SUM(CASE WHEN m.HomeScore = m.AwayScore THEN 1 ELSE 0 END) as Draws,
        SUM(CASE WHEN m.HomeParticipantId = p1.Id THEN ISNULL(m.HomeScore, 0) 
                 ELSE ISNULL(m.AwayScore, 0) END) as Participant1Goals,
        SUM(CASE WHEN m.HomeParticipantId = p2.Id THEN ISNULL(m.HomeScore, 0) 
                 ELSE ISNULL(m.AwayScore, 0) END) as Participant2Goals
    FROM Participants p1
    CROSS JOIN Participants p2
    LEFT JOIN Matches m ON m.IsCompleted = 1 
        AND ((m.HomeParticipantId = p1.Id AND m.AwayParticipantId = p2.Id) OR
             (m.HomeParticipantId = p2.Id AND m.AwayParticipantId = p1.Id))
    WHERE p1.Id < p2.Id
    GROUP BY p1.Id, p1.Name, p2.Id, p2.Name
    HAVING COUNT(m.Id) > 0
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
        -- Создаем финал
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
        SELECT 
            CASE 
                WHEN HomeScore > AwayScore THEN HomeParticipantId
                WHEN AwayScore > HomeScore THEN AwayParticipantId
                ELSE NULL
            END
        FROM #PlayoffMatches
        WHERE HomeScore IS NOT NULL AND AwayScore IS NOT NULL
        AND HomeScore <> AwayScore
        

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