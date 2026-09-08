using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionProjetSocota.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceCompteurs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReferenceCompteurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Unite = table.Column<int>(type: "int", nullable: false),
                    Prefixe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DernierNumero = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceCompteurs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceCompteurs_Unite",
                table: "ReferenceCompteurs",
                column: "Unite",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferenceCompteurs");
        }
    }
}
