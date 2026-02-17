using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental_App.Migrations
{
    /// <inheritdoc />
    public partial class AddCaisseUpsertTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create trigger for INSERT: if date exists, update the montant instead of inserting
            migrationBuilder.Sql(@"
                CREATE TRIGGER tr_Caisse_Insert
                BEFORE INSERT ON Caisse
                BEGIN
                    UPDATE Caisse 
                    SET Montant = COALESCE(Montant, 0) + COALESCE(NEW.Montant, 0)
                    WHERE DateDuJour = NEW.DateDuJour;
                    
                    SELECT CASE
                        WHEN (SELECT changes() = 0) THEN
                            RAISE(IGNORE)
                        END;
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS tr_Caisse_Insert;");
        }
    }
}
