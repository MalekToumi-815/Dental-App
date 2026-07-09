using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental_App.Migrations
{
    /// <inheritdoc />
    public partial class FixCaisseKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Caisse",
                table: "Caisse");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Caisse",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            // Assigner un Id unique à chaque ligne existante (basé sur le rowid interne SQLite)
            migrationBuilder.Sql("UPDATE \"Caisse\" SET \"Id\" = \"rowid\";");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Caisse",
                table: "Caisse",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Caisse",
                table: "Caisse");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Caisse");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Caisse",
                table: "Caisse",
                columns: new[] { "DateDuJour", "IsRevenu" });
        }
    }
}
