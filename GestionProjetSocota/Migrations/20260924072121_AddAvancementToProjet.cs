using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionProjetSocota.Migrations
{
    /// <inheritdoc />
    public partial class AddAvancementToProjet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Avancement",
                table: "Projets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Avancement",
                table: "Projets");
        }
    }
}
