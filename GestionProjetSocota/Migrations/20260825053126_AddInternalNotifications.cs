using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionProjetSocota.Migrations
{
    /// <inheritdoc />
    public partial class AddInternalNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Destinataire",
                table: "Notifications",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "DateEnvoi",
                table: "Notifications",
                newName: "DateCreation");

            migrationBuilder.AddColumn<bool>(
                name: "EstLue",
                table: "Notifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UtilisateurId",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UtilisateurId",
                table: "Notifications",
                column: "UtilisateurId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Utilisateurs_UtilisateurId",
                table: "Notifications",
                column: "UtilisateurId",
                principalTable: "Utilisateurs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Utilisateurs_UtilisateurId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UtilisateurId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "EstLue",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "UtilisateurId",
                table: "Notifications");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "Notifications",
                newName: "Destinataire");

            migrationBuilder.RenameColumn(
                name: "DateCreation",
                table: "Notifications",
                newName: "DateEnvoi");
        }
    }
}
