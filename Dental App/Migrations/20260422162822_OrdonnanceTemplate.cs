using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental_App.Migrations
{
    /// <inheritdoc />
    public partial class OrdonnanceTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdonnanceTemplate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TemplateX = table.Column<double>(type: "REAL", nullable: false),
                    TemplateY = table.Column<double>(type: "REAL", nullable: false),
                    TemplatePath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdonnanceTemplate", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrdonnanceTemplate");
        }
    }
}
