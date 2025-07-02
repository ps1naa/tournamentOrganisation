CREATE OR ALTER VIEW vw_TournamentWithParticipants
AS
SELECT 
    t.Id as TournamentId, 
    t.Name as TournamentName, 
    t.StartDate, 
    t.EndDate, 
    t.Description, 
    t.MatchesPerOpponent, 
    t.IsCompleted, 
    t.PlayoffGenerated, 
    t.CreatedAt as TournamentCreatedAt,
    p.Id as ParticipantId, 
    p.Name as ParticipantName, 
    p.Email as ParticipantEmail, 
    p.Phone as ParticipantPhone, 
    p.CreatedAt as ParticipantCreatedAt,
    tp.JoinedAt
FROM Tournaments t
LEFT JOIN TournamentParticipants tp ON t.Id = tp.TournamentId
LEFT JOIN Participants p ON tp.ParticipantId = p.Id
GO

CREATE OR ALTER VIEW vw_MatchDetails
AS
SELECT 
    m.Id as MatchId, 
    m.TournamentId, 
    m.HomeParticipantId, 
    m.AwayParticipantId,
    m.HomeScore, 
    m.AwayScore, 
    m.PlayedAt, 
    m.IsCompleted, 
    m.Type, 
    m.CreatedAt as MatchCreatedAt,
    hp.Name as HomeParticipantName, 
    ap.Name as AwayParticipantName,
    t.Name as TournamentName,
    t.StartDate as TournamentStartDate,
    t.EndDate as TournamentEndDate
FROM Matches m
INNER JOIN Participants hp ON m.HomeParticipantId = hp.Id
INNER JOIN Participants ap ON m.AwayParticipantId = ap.Id
INNER JOIN Tournaments t ON m.TournamentId = t.Id
GO

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
GROUP BY tp.TournamentId, p.Id, p.Name
GO

CREATE OR ALTER VIEW vw_ParticipantStatistics
AS
SELECT 
    p.Id as ParticipantId,
    p.Name as ParticipantName,
    p.Email,
    p.Phone,
    p.CreatedAt,
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
             ELSE ISNULL(m.HomeScore, 0) END) as TotalGoalsConceded,
    SUM(CASE 
        WHEN (m.HomeParticipantId = p.Id AND m.HomeScore > m.AwayScore) OR 
             (m.AwayParticipantId = p.Id AND m.AwayScore > m.HomeScore) 
        THEN 3 
        WHEN m.HomeScore = m.AwayScore THEN 1 
        ELSE 0 END) as TotalPoints
FROM Participants p
LEFT JOIN TournamentParticipants tp ON p.Id = tp.ParticipantId
LEFT JOIN Matches m ON (m.HomeParticipantId = p.Id OR m.AwayParticipantId = p.Id)
    AND m.IsCompleted = 1
GROUP BY p.Id, p.Name, p.Email, p.Phone, p.CreatedAt
GO

CREATE OR ALTER VIEW vw_HeadToHeadStatistics
AS
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
GO

CREATE OR ALTER VIEW vw_PlayoffWinners
AS
SELECT 
    TournamentId,
    CASE 
        WHEN HomeScore > AwayScore THEN HomeParticipantId
        WHEN AwayScore > HomeScore THEN AwayParticipantId
        ELSE NULL
    END as WinnerId,
    CASE 
        WHEN HomeScore > AwayScore THEN 'Home'
        WHEN AwayScore > HomeScore THEN 'Away'
        ELSE 'Draw'
    END as WinnerType
FROM Matches 
WHERE Type = 1 
AND IsCompleted = 1 
AND HomeScore IS NOT NULL 
AND AwayScore IS NOT NULL
AND HomeScore <> AwayScore
GO 