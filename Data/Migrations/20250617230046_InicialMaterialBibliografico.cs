using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaBiblioteca.Data.Migrations
{
    /// <inheritdoc />
    public partial class InicialMaterialBibliografico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MaterialBibliografico",
                table: "MaterialBibliografico");

            migrationBuilder.RenameTable(
                name: "MaterialBibliografico",
                newName: "MaterialesBibliograficos");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MaterialesBibliograficos",
                table: "MaterialesBibliograficos",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_MaterialesBibliograficos",
                table: "MaterialesBibliograficos");

            migrationBuilder.RenameTable(
                name: "MaterialesBibliograficos",
                newName: "MaterialBibliografico");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MaterialBibliografico",
                table: "MaterialBibliografico",
                column: "Id");
        }
    }
}
