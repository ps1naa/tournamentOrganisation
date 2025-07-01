using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    public partial class FixDeleteTournamentProcedure : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Обновление процедуры sp_DeleteTournament
                CREATE OR ALTER PROCEDURE sp_DeleteTournament
                    @TournamentId INT
                AS
                BEGIN
                    DECLARE @RowsAffected INT = 0
                    
                    -- Проверяем, существует ли турнир
                    IF EXISTS (SELECT 1 FROM Tournaments WHERE Id = @TournamentId)
                    BEGIN
                        BEGIN TRANSACTION
                        
                        -- Удаляем матчи
                        DELETE FROM Matches WHERE TournamentId = @TournamentId
                        
                        -- Удаляем участников турнира
                        DELETE FROM TournamentParticipants WHERE TournamentId = @TournamentId
                        
                        -- Удаляем турнир
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
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Возврат к старой версии процедуры sp_DeleteTournament
                CREATE OR ALTER PROCEDURE sp_DeleteTournament
                    @TournamentId INT
                AS
                BEGIN
                    BEGIN TRANSACTION
                    
                    -- Удаляем матчи
                    DELETE FROM Matches WHERE TournamentId = @TournamentId
                    
                    -- Удаляем участников турнира
                    DELETE FROM TournamentParticipants WHERE TournamentId = @TournamentId
                    
                    -- Удаляем турнир
                    DELETE FROM Tournaments WHERE Id = @TournamentId
                    
                    IF @@ERROR = 0
                        COMMIT TRANSACTION
                    ELSE
                        ROLLBACK TRANSACTION
                        
                    SELECT @@ROWCOUNT as RowsAffected
                END
            ");
        }
    }
}
