using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionProjetSocota.Migrations
{
    public partial class CorrigerEstActifUtilisateurs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Utilisateurs SET EstActif = 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
    
        }
    }
}