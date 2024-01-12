using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Generellem.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DocumentHashes",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DocumentReference = table.Column<string>(type: "TEXT", nullable: true),
                Hash = table.Column<string>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentHashes", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DocumentHashes_DocumentReference",
            table: "DocumentHashes",
            column: "DocumentReference");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DocumentHashes");
    }
}
