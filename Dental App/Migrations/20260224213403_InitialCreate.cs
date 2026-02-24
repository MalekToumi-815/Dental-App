using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dental_App.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                    Libelle = table.Column<string>(type: "TEXT", nullable: false),
                    Prix = table.Column<decimal>(type: "decimal(18, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActeMedical", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Antecedant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Antecedant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Caisse",
                columns: table => new
                {
                    DateDuJour = table.Column<string>(type: "TEXT", nullable: false),
                    Montant = table.Column<decimal>(type: "decimal(18, 2)", nullable: true, defaultValue: 0.0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caisse", x => x.DateDuJour);
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
                    table.PrimaryKey("PK_Dent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patient",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Prenom = table.Column<string>(type: "TEXT", nullable: false),
                    DateNaissance = table.Column<string>(type: "TEXT", nullable: false),
                    Sexe = table.Column<string>(type: "TEXT", nullable: true),
                    Telephone = table.Column<string>(type: "TEXT", nullable: false),
                    SommePaye = table.Column<decimal>(type: "decimal(18, 2)", nullable: true, defaultValue: 0.0m),
                    Adresse = table.Column<string>(type: "TEXT", nullable: false),
                    Profession = table.Column<string>(type: "TEXT", nullable: true),
                    CIN = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patient", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prothesiste",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Adresse = table.Column<string>(type: "TEXT", nullable: true),
                    Tel = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prothesiste", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Utilisateur",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Prenom = table.Column<string>(type: "TEXT", nullable: false),
                    MotDePasseHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateur", x => x.Id);
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
                    table.PrimaryKey("PK_Consultation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consultation_Dent_IdDent",
                        column: x => x.IdDent,
                        principalTable: "Dent",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Consultation_Patient_PatientId",
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
                    table.PrimaryKey("PK_OdontogrammeLibre", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OdontogrammeLibre_Patient_PatientId",
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
                    table.PrimaryKey("PK_Ordonnance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ordonnance_Patient_PatientId",
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

            migrationBuilder.CreateTable(
                name: "RadioImage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: true),
                    DatePrise = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Type = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadioImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadioImage_Patient_PatientId",
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
                    Statut = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RendezVous", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RendezVous_Patient_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patient",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Commande_Prothesiste",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Id_Prothesiste = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "DATETIME", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Achats = table.Column<string>(type: "TEXT", nullable: true),
                    Somme_Payees = table.Column<double>(type: "REAL", nullable: true, defaultValue: 0.0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commande_Prothesiste", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commande_Prothesiste_Prothesiste_Id_Prothesiste",
                        column: x => x.Id_Prothesiste,
                        principalTable: "Prothesiste",
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
                    table.PrimaryKey("PK_ActeConsultation", x => new { x.IdConsul, x.IdActe });
                    table.ForeignKey(
                        name: "FK_ActeConsultation_ActeMedical_IdActe",
                        column: x => x.IdActe,
                        principalTable: "ActeMedical",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ActeConsultation_Consultation_IdConsul",
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
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Posologie = table.Column<string>(type: "TEXT", nullable: true),
                    OrdonnanceId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicament", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Medicament_Ordonnance_OrdonnanceId",
                        column: x => x.OrdonnanceId,
                        principalTable: "Ordonnance",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActeConsultation_IdActe",
                table: "ActeConsultation",
                column: "IdActe");

            migrationBuilder.CreateIndex(
                name: "IX_Commande_Prothesiste_Id_Prothesiste",
                table: "Commande_Prothesiste",
                column: "Id_Prothesiste");

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
                name: "Commande_Prothesiste");

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
                name: "Prothesiste");

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
