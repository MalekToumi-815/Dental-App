using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental_App.Migrations
{
    /// <inheritdoc />
    public partial class EnableCascadeDeleteForProthesiste : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commande_Prothesiste_Prothesiste_Id_Prothesiste",
                table: "Commande_Prothesiste");

            migrationBuilder.AddForeignKey(
                name: "FK_Commande_Prothesiste_Prothesiste_Id_Prothesiste",
                table: "Commande_Prothesiste",
                column: "Id_Prothesiste",
                principalTable: "Prothesiste",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Commande_Prothesiste_Prothesiste_Id_Prothesiste",
                table: "Commande_Prothesiste");

            migrationBuilder.AddForeignKey(
                name: "FK_Commande_Prothesiste_Prothesiste_Id_Prothesiste",
                table: "Commande_Prothesiste",
                column: "Id_Prothesiste",
                principalTable: "Prothesiste",
                principalColumn: "Id");
        }
    }
}
