using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCGAPYS.Migrations
{
    /// <inheritdoc />
    public partial class AgregaTablasFasesEstimacionesYRelacionFaseEnProyectos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdFaseFk",
                table: "Proyectos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Estimaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Monto = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    FechaEstimacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdProyectoFk = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estimaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Estimaciones_Proyectos_IdProyectoFk",
                        column: x => x.IdProyectoFk,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fases", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_IdFaseFk",
                table: "Proyectos",
                column: "IdFaseFk");

            migrationBuilder.CreateIndex(
                name: "IX_Estimaciones_IdProyectoFk",
                table: "Estimaciones",
                column: "IdProyectoFk");

            migrationBuilder.AddForeignKey(
                name: "FK_Proyectos_Fases_IdFaseFk",
                table: "Proyectos",
                column: "IdFaseFk",
                principalTable: "Fases",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Proyectos_Fases_IdFaseFk",
                table: "Proyectos");

            migrationBuilder.DropTable(
                name: "Estimaciones");

            migrationBuilder.DropTable(
                name: "Fases");

            migrationBuilder.DropIndex(
                name: "IX_Proyectos_IdFaseFk",
                table: "Proyectos");

            migrationBuilder.DropColumn(
                name: "IdFaseFk",
                table: "Proyectos");
        }
    }
}
