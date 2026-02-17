using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental_App.Migrations
{
    /// <inheritdoc />
    public partial class InitialSQLiteCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActeMedical",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Libelle = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Prix = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ActeMedi__3214EC075F8B031D", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Antecedant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Anteceda__3214EC07B461AA38", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Caisse",
                columns: table => new
                {
                    DateDuJour = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18, 2)", nullable: true, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Caisse__60CDBF3EC933E1D1", x => x.DateDuJour);
                });

            migrationBuilder.CreateTable(
                name: "Dent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CodeFDI = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Dent__3214EC07A95B2285", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patient",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DateNaissance = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Sexe = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Telephone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SommePaye = table.Column<decimal>(type: "decimal(18, 2)", nullable: true, defaultValue: 0m),
                    Adresse = table.Column<string>(type: "TEXT", nullable: false),
                    Profession = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CIN = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Patient__3214EC078A5281F7", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateur",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Prenom = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MotDePasseHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Utilisat__3214EC0709A8F6B9", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Consultation",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateConsultation = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    IdDent = table.Column<int>(type: "INTEGER", nullable: true),
                    MontantTotal = table.Column<decimal>(type: "decimal(18, 2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Consulta__3214EC070B62EC18", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consultation_Dent",
                        column: x => x.IdDent,
                        principalTable: "Dent",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Consultation_Patient",
                        column: x => x.PatientId,
                        principalTable: "Patient",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OdontogrammeLibre",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    InkFilePath = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Odontogr__3214EC078C2999DB", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Odontogramme_Patient",
                        column: x => x.PatientId,
                        principalTable: "Patient",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Ordonnance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Ordonnan__3214EC0769A52545", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ordonnance_Patient",
                        column: x => x.PatientId,
                        principalTable: "Patient",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PatientAntecedant",
                columns: table => new
                {
                    IdPatient = table.Column<int>(type: "INTEGER", nullable: false),
                    IdAntecedant = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PatientA__798C43F908F64248", x => new { x.IdPatient, x.IdAntecedant });
                    table.ForeignKey(
                        name: "FK_PatAnt_Ant",
                        column: x => x.IdAntecedant,
                        principalTable: "Antecedant",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PatAnt_Patient",
                        column: x => x.IdPatient,
                        principalTable: "Patient",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RadioImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    DatePrise = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RadioIma__3214EC07261052E2", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioImage_Patient",
                        column: x => x.PatientId,
                        principalTable: "Patient",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RendezVous",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "datetime", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime", nullable: true),
                    Statut = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RendezVo__3214EC07CC1B80E1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RendezVous_Patient",
                        column: x => x.PatientId,
                        principalTable: "Patient",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ActeConsultation",
                columns: table => new
                {
                    IdConsul = table.Column<int>(type: "INTEGER", nullable: false),
                    IdActe = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ActeCons__205BE8A7DAD9A0EC", x => new { x.IdConsul, x.IdActe });
                    table.ForeignKey(
                        name: "FK_ActeConsul_Acte",
                        column: x => x.IdActe,
                        principalTable: "ActeMedical",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActeConsul_Consul",
                        column: x => x.IdConsul,
                        principalTable: "Consultation",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Medicament",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Posologie = table.Column<string>(type: "TEXT", nullable: true),
                    OrdonnanceId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Medicame__3214EC0705259ACA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medicament_Ordonnance",
                        column: x => x.OrdonnanceId,
                        principalTable: "Ordonnance",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActeConsultation_IdActe",
                table: "ActeConsultation",
                column: "IdActe");

            migrationBuilder.CreateIndex(
                name: "IX_Consultation_IdDent",
                table: "Consultation",
                column: "IdDent");

            migrationBuilder.CreateIndex(
                name: "IX_Consultation_PatientId",
                table: "Consultation",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicament_OrdonnanceId",
                table: "Medicament",
                column: "OrdonnanceId");

            migrationBuilder.CreateIndex(
                name: "IX_OdontogrammeLibre_PatientId",
                table: "OdontogrammeLibre",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Ordonnance_PatientId",
                table: "Ordonnance",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAntecedant_IdAntecedant",
                table: "PatientAntecedant",
                column: "IdAntecedant");

            migrationBuilder.CreateIndex(
                name: "IX_RadioImage_PatientId",
                table: "RadioImage",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_RendezVous_PatientId",
                table: "RendezVous",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActeConsultation");

            migrationBuilder.DropTable(
                name: "Caisse");

            migrationBuilder.DropTable(
                name: "Medicament");

            migrationBuilder.DropTable(
                name: "OdontogrammeLibre");

            migrationBuilder.DropTable(
                name: "PatientAntecedant");

            migrationBuilder.DropTable(
                name: "RadioImage");

            migrationBuilder.DropTable(
                name: "RendezVous");

            migrationBuilder.DropTable(
                name: "Utilisateur");

            migrationBuilder.DropTable(
                name: "ActeMedical");

            migrationBuilder.DropTable(
                name: "Consultation");

            migrationBuilder.DropTable(
                name: "Ordonnance");

            migrationBuilder.DropTable(
                name: "Antecedant");

            migrationBuilder.DropTable(
                name: "Dent");

            migrationBuilder.DropTable(
                name: "Patient");
        }
    }
}
