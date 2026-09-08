using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionProjetSocota.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeDynamicClassifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // =========================================================
            // REFERENCE COMPTEURS
            // =========================================================

            migrationBuilder.RenameColumn(
                name: "Unite",
                table: "ReferenceCompteurs",
                newName: "UniteProjetId");

            migrationBuilder.RenameIndex(
                name: "IX_ReferenceCompteurs_Unite",
                table: "ReferenceCompteurs",
                newName: "IX_ReferenceCompteurs_UniteProjetId");

            // Ancien enum Unite :
            // CTN = 0, SGL = 1, CRE = 2
            //
            // Nouveaux IDs :
            // CTN = 1, SGL = 2, CRE = 3
            migrationBuilder.Sql(@"
                UPDATE ReferenceCompteurs
                SET UniteProjetId = UniteProjetId + 1;
            ");


            // =========================================================
            // PROJETS : RENOMMAGE DES COLONNES
            // =========================================================

            migrationBuilder.RenameColumn(
                name: "Unite",
                table: "Projets",
                newName: "UniteProjetId");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Projets",
                newName: "TypeProjetReferenceId");

            migrationBuilder.RenameColumn(
                name: "Plateforme",
                table: "Projets",
                newName: "PlateformeProjetId");

            migrationBuilder.RenameColumn(
                name: "Departement",
                table: "Projets",
                newName: "DepartementProjetId");


            // =========================================================
            // CONVERSION DES ANCIENS ENUMS VERS LES NOUVEAUX IDS
            // =========================================================

            // Anciennes valeurs des enums :
            //
            // Unite:
            // 0 CTN -> 1
            // 1 SGL -> 2
            // 2 CRE -> 3
            //
            // Departement:
            // 0 CAL  -> 1
            // 1 IND  -> 2
            // 2 LOG  -> 3
            // 3 IT   -> 4
            // 4 PRO  -> 5
            // 5 MPF  -> 6
            // 6 QUA  -> 7
            // 7 SUST -> 8
            // 8 CTE  -> 9
            // 9 SALES-> 10
            // 10 PLN -> 11
            //
            // Type:
            // 0 InHouse     -> 1
            // 1 Outsourced -> 2
            //
            // Plateforme:
            // 0 WEB      -> 1
            // 1 GPAO     -> 2
            // 2 PBI      -> 3
            // 3 SUN      -> 4
            // 4 Oracle   -> 5
            // 5 CRP      -> 6
            // 6 Mobile   -> 7
            // 7 SEAM     -> 8
            // 8 FREvolve -> 9

            migrationBuilder.Sql(@"
                UPDATE Projets
                SET
                    UniteProjetId = UniteProjetId + 1,
                    DepartementProjetId = DepartementProjetId + 1,
                    TypeProjetReferenceId = TypeProjetReferenceId + 1,
                    PlateformeProjetId = PlateformeProjetId + 1;
            ");


            // =========================================================
            // UNITÉS : AJOUT DU PRÉFIXE
            // =========================================================

            migrationBuilder.AddColumn<string>(
                name: "Prefixe",
                table: "UnitesProjets",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");


            migrationBuilder.UpdateData(
                table: "UnitesProjets",
                keyColumn: "Id",
                keyValue: 1,
                column: "Prefixe",
                value: "CF");

            migrationBuilder.UpdateData(
                table: "UnitesProjets",
                keyColumn: "Id",
                keyValue: 2,
                column: "Prefixe",
                value: "SG");

            migrationBuilder.UpdateData(
                table: "UnitesProjets",
                keyColumn: "Id",
                keyValue: 3,
                column: "Prefixe",
                value: "CR");


            // =========================================================
            // INDEX
            // =========================================================

            migrationBuilder.CreateIndex(
                name: "IX_Projets_DepartementProjetId",
                table: "Projets",
                column: "DepartementProjetId");

            migrationBuilder.CreateIndex(
                name: "IX_Projets_PlateformeProjetId",
                table: "Projets",
                column: "PlateformeProjetId");

            migrationBuilder.CreateIndex(
                name: "IX_Projets_TypeProjetReferenceId",
                table: "Projets",
                column: "TypeProjetReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Projets_UniteProjetId",
                table: "Projets",
                column: "UniteProjetId");


            // =========================================================
            // CLÉS ÉTRANGÈRES
            // =========================================================

            migrationBuilder.AddForeignKey(
                name: "FK_Projets_DepartementsProjets_DepartementProjetId",
                table: "Projets",
                column: "DepartementProjetId",
                principalTable: "DepartementsProjets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projets_PlateformesProjets_PlateformeProjetId",
                table: "Projets",
                column: "PlateformeProjetId",
                principalTable: "PlateformesProjets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projets_TypesProjets_TypeProjetReferenceId",
                table: "Projets",
                column: "TypeProjetReferenceId",
                principalTable: "TypesProjets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projets_UnitesProjets_UniteProjetId",
                table: "Projets",
                column: "UniteProjetId",
                principalTable: "UnitesProjets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ReferenceCompteurs_UnitesProjets_UniteProjetId",
                table: "ReferenceCompteurs",
                column: "UniteProjetId",
                principalTable: "UnitesProjets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // =========================================================
            // SUPPRESSION DES CLÉS ÉTRANGÈRES
            // =========================================================

            migrationBuilder.DropForeignKey(
                name: "FK_Projets_DepartementsProjets_DepartementProjetId",
                table: "Projets");

            migrationBuilder.DropForeignKey(
                name: "FK_Projets_PlateformesProjets_PlateformeProjetId",
                table: "Projets");

            migrationBuilder.DropForeignKey(
                name: "FK_Projets_TypesProjets_TypeProjetReferenceId",
                table: "Projets");

            migrationBuilder.DropForeignKey(
                name: "FK_Projets_UnitesProjets_UniteProjetId",
                table: "Projets");

            migrationBuilder.DropForeignKey(
                name: "FK_ReferenceCompteurs_UnitesProjets_UniteProjetId",
                table: "ReferenceCompteurs");


            // =========================================================
            // SUPPRESSION DES INDEX
            // =========================================================

            migrationBuilder.DropIndex(
                name: "IX_Projets_DepartementProjetId",
                table: "Projets");

            migrationBuilder.DropIndex(
                name: "IX_Projets_PlateformeProjetId",
                table: "Projets");

            migrationBuilder.DropIndex(
                name: "IX_Projets_TypeProjetReferenceId",
                table: "Projets");

            migrationBuilder.DropIndex(
                name: "IX_Projets_UniteProjetId",
                table: "Projets");


            // =========================================================
            // SUPPRESSION DU PRÉFIXE
            // =========================================================

            migrationBuilder.DropColumn(
                name: "Prefixe",
                table: "UnitesProjets");


            // =========================================================
            // RESTAURATION DES ANCIENS NOMS
            // =========================================================

            migrationBuilder.RenameColumn(
                name: "UniteProjetId",
                table: "ReferenceCompteurs",
                newName: "Unite");

            migrationBuilder.RenameIndex(
                name: "IX_ReferenceCompteurs_UniteProjetId",
                table: "ReferenceCompteurs",
                newName: "IX_ReferenceCompteurs_Unite");


            migrationBuilder.RenameColumn(
                name: "UniteProjetId",
                table: "Projets",
                newName: "Unite");

            migrationBuilder.RenameColumn(
                name: "TypeProjetReferenceId",
                table: "Projets",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "PlateformeProjetId",
                table: "Projets",
                newName: "Plateforme");

            migrationBuilder.RenameColumn(
                name: "DepartementProjetId",
                table: "Projets",
                newName: "Departement");


            // =========================================================
            // CONVERSION DES IDS VERS LES ANCIENS ENUMS
            // =========================================================

            migrationBuilder.Sql(@"
                UPDATE Projets
                SET
                    Unite = Unite - 1,
                    Departement = Departement - 1,
                    Type = Type - 1,
                    Plateforme = Plateforme - 1;
            ");

            migrationBuilder.Sql(@"
                UPDATE ReferenceCompteurs
                SET Unite = Unite - 1;
            ");
        }
    }
}