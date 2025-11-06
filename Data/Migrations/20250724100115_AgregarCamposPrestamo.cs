using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaBiblioteca.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposPrestamo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApellidoNino",
                table: "Prestamos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Devuelto",
                table: "Prestamos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaLimiteDevolucion",
                table: "Prestamos",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "GradoNino",
                table: "Prestamos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NombreNino",
                table: "Prestamos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApellidoNino",
                table: "Prestamos");

            migrationBuilder.DropColumn(
                name: "Devuelto",
                table: "Prestamos");

            migrationBuilder.DropColumn(
                name: "FechaLimiteDevolucion",
                table: "Prestamos");

            migrationBuilder.DropColumn(
                name: "GradoNino",
                table: "Prestamos");

            migrationBuilder.DropColumn(
                name: "NombreNino",
                table: "Prestamos");
        }
    }
}
