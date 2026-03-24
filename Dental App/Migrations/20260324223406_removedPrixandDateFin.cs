using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental_App.Migrations
{
    /// <inheritdoc />
    public partial class removedPrixandDateFin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateFin",
                table: "RendezVous");

            migrationBuilder.DropColumn(
                name: "Prix",
                table: "ActeMedical");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateFin",
                table: "RendezVous",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Prix",
                table: "ActeMedical",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
