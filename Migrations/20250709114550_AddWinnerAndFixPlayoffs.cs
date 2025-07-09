using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TournamentApp.Migrations
{
    public partial class AddWinnerAndFixPlayoffs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Tournaments') AND name = 'WinnerId')
                BEGIN
                    ALTER TABLE [Tournaments] ADD [WinnerId] int NULL;
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Tournaments') AND name = 'IX_Tournaments_WinnerId')
                BEGIN
                    CREATE INDEX [IX_Tournaments_WinnerId] ON [Tournaments] ([WinnerId]);
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Tournaments_Participants_WinnerId')
                BEGIN
                    ALTER TABLE [Tournaments] ADD CONSTRAINT [FK_Tournaments_Participants_WinnerId] 
                    FOREIGN KEY ([WinnerId]) REFERENCES [Participants] ([Id]) ON DELETE SET NULL;
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Participants_WinnerId",
                table: "Tournaments");

            migrationBuilder.DropIndex(
                name: "IX_Tournaments_WinnerId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "WinnerId",
                table: "Tournaments");
        }
    }
}
