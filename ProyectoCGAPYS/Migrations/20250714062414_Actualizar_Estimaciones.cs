using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCGAPYS.Migrations
{
    /// <inheritdoc />
    public partial class Actualizar_Estimaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Estimaciones",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FaseViewModels",
                columns: table => new
                {
                    Fase = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalProyectos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "FondoViewModels",
                columns: table => new
                {
                    NombreFondo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MontoAutorizado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoEjercido = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "KPIsViewModels",
                columns: table => new
                {
                    ProyectosTotales = table.Column<int>(type: "int", nullable: false),
                    ProyectosActivos = table.Column<int>(type: "int", nullable: false),
                    PresupuestoTotalAutorizado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoTotalEjercido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceGeneralDisponible = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "ProyectoAlertaViewModels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreProyecto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreResponsable = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiasTranscurridos = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaseViewModels");

            migrationBuilder.DropTable(
                name: "FondoViewModels");

            migrationBuilder.DropTable(
                name: "KPIsViewModels");

            migrationBuilder.DropTable(
                name: "ProyectoAlertaViewModels");

            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Estimaciones");
        }
    }
}
