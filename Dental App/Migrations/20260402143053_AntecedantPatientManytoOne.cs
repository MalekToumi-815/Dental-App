using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental_App.Migrations
{
    /// <inheritdoc />
    public partial class AntecedantPatientManytoOne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientAntecedant");

            migrationBuilder.AddColumn<int>(
                name: "PatientId",
                table: "Antecedant",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Antecedant_PatientId",
                table: "Antecedant",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Antecedant_Patient",
                table: "Antecedant",
                column: "PatientId",
                principalTable: "Patient",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Antecedant_Patient",
                table: "Antecedant");

            migrationBuilder.DropIndex(
                name: "IX_Antecedant_PatientId",
                table: "Antecedant");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "Antecedant");

            migrationBuilder.CreateTable(
                name: "PatientAntecedant",
                columns: table => new
                {
                    IdPatient = table.Column<int>(type: "INTEGER", nullable: false),
                    IdAntecedant = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAntecedant", x => new { x.IdPatient, x.IdAntecedant });
                    table.ForeignKey(
                        name: "FK_PatientAntecedant_Antecedant_IdAntecedant",
                        column: x => x.IdAntecedant,
                        principalTable: "Antecedant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatientAntecedant_Patient_IdPatient",
                        column: x => x.IdPatient,
                        principalTable: "Patient",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAntecedant_IdAntecedant",
                table: "PatientAntecedant",
                column: "IdAntecedant");
        }
    }
}
