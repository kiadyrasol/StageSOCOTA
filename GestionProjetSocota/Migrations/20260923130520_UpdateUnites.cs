using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionProjetSocota.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUnites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "UnitesProjets",
                keyColumn: "Id",
                keyValue: 1,
                column: "Nom",
                value: "CF : COTONA Fabrics");

            migrationBuilder.UpdateData(
                table: "UnitesProjets",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Nom", "Prefixe" },
                values: new object[] { "SGL : SOCOTA Garments Limited", "SGL" });

            migrationBuilder.UpdateData(
                table: "UnitesProjets",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Nom", "Prefixe" },
                values: new object[] { "CRE : COTONA Real Estate", "CRE" });

            migrationBuilder.InsertData(
                table: "UnitesProjets",
                columns: new[] { "Id", "Actif", "Nom", "Prefixe" },
                values: new object[,]
                {
                    { 4, true, "SH : SOCOTA House", "SH" },
                    { 5, true, "GS : GROUPE SOCOTA", "GS" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UnitesProjets",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "UnitesProjets",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.UpdateData(
                table: "UnitesProjets",
                keyColumn: "Id",
                keyValue: 1,
                column: "Nom",
                value: "CTN");

            migrationBuilder.UpdateData(
                table: "UnitesProjets",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Nom", "Prefixe" },
                values: new object[] { "SGL", "SG" });

            migrationBuilder.UpdateData(
                table: "UnitesProjets",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Nom", "Prefixe" },
                values: new object[] { "CRE", "CR" });
        }
    }
}
