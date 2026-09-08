using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionProjetSocota.Migrations
{
    /// <inheritdoc />
    public partial class AddClassificationReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DepartementsProjets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartementsProjets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlateformesProjets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlateformesProjets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TypesProjets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesProjets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitesProjets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Actif = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitesProjets", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "DepartementsProjets",
                columns: new[] { "Id", "Actif", "Nom" },
                values: new object[,]
                {
                    { 1, true, "CAL" },
                    { 2, true, "IND" },
                    { 3, true, "LOG" },
                    { 4, true, "IT" },
                    { 5, true, "PRO" },
                    { 6, true, "MPF" },
                    { 7, true, "QUA" },
                    { 8, true, "SUST" },
                    { 9, true, "CTE" },
                    { 10, true, "SALES" },
                    { 11, true, "PLN" }
                });

            migrationBuilder.InsertData(
                table: "PlateformesProjets",
                columns: new[] { "Id", "Actif", "Nom" },
                values: new object[,]
                {
                    { 1, true, "WEB" },
                    { 2, true, "GPAO" },
                    { 3, true, "PBI" },
                    { 4, true, "SUN" },
                    { 5, true, "Oracle" },
                    { 6, true, "CRP" },
                    { 7, true, "Mobile" },
                    { 8, true, "SEAM" },
                    { 9, true, "FREvolve" }
                });

            migrationBuilder.InsertData(
                table: "TypesProjets",
                columns: new[] { "Id", "Actif", "Nom" },
                values: new object[,]
                {
                    { 1, true, "InHouse" },
                    { 2, true, "Outsourced" }
                });

            migrationBuilder.InsertData(
                table: "UnitesProjets",
                columns: new[] { "Id", "Actif", "Nom" },
                values: new object[,]
                {
                    { 1, true, "CTN" },
                    { 2, true, "SGL" },
                    { 3, true, "CRE" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DepartementsProjets_Nom",
                table: "DepartementsProjets",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlateformesProjets_Nom",
                table: "PlateformesProjets",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TypesProjets_Nom",
                table: "TypesProjets",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitesProjets_Nom",
                table: "UnitesProjets",
                column: "Nom",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DepartementsProjets");

            migrationBuilder.DropTable(
                name: "PlateformesProjets");

            migrationBuilder.DropTable(
                name: "TypesProjets");

            migrationBuilder.DropTable(
                name: "UnitesProjets");
        }
    }
}
