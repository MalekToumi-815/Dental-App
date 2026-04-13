using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental_App.Migrations
{
    /// <inheritdoc />
    public partial class removeRevenueDefaultVaue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsRevenu",
                table: "Caisse",
                type: "BOOLEAN",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN",
                oldDefaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsRevenu",
                table: "Caisse",
                type: "BOOLEAN",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "BOOLEAN");
        }
    }
}
