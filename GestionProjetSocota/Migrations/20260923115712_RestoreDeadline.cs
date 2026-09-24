using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionProjetSocota.Migrations
{
    /// <inheritdoc />
    public partial class RestoreDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Deadline",
                table: "Projets",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deadline",
                table: "Projets");
        }
    }
}
