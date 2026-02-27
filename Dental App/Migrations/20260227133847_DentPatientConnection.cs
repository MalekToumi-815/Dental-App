using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental_App.Migrations
{
    /// <inheritdoc />
    public partial class DentPatientConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PatientId",
                table: "Dent",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dent_PatientId",
                table: "Dent",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_Dent_Patient",
                table: "Dent",
                column: "PatientId",
                principalTable: "Patient",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Dent_Patient",
                table: "Dent");

            migrationBuilder.DropIndex(
                name: "IX_Dent_PatientId",
                table: "Dent");

            migrationBuilder.DropColumn(
                name: "PatientId",
                table: "Dent");
        }
    }
}
