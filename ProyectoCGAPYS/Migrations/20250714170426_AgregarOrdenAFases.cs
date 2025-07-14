using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCGAPYS.Migrations
{
    /// <inheritdoc />
    public partial class AgregarOrdenAFases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Orden",
                table: "Fases",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Orden",
                table: "Fases");
        }
    }
}
