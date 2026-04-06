using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental_App.Migrations
{
    /// <inheritdoc />
    public partial class PKCaisse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Caisse",
                table: "Caisse");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Caisse",
                table: "Caisse",
                columns: new[] { "DateDuJour", "IsRevenu" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Caisse",
                table: "Caisse");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Caisse",
                table: "Caisse",
                column: "DateDuJour");
        }
    }
}
