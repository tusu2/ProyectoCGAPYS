using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProyectoCGAPYS.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Campus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dependencias",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependencias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposFondo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposFondo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposProyecto",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposProyecto", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conceptos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Clave = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unidad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IdCategoriaFk = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conceptos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conceptos_Categorias_IdCategoriaFk",
                        column: x => x.IdCategoriaFk,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Proyectos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NombreProyecto = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFinalizacionAprox = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Presupuesto = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    Estatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NombreResponsable = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Celular = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NombreAnteproyecto = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Latitud = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Longitud = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Folio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IdCampusFk = table.Column<int>(type: "int", nullable: false),
                    IdDependenciaFk = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdTipoFondoFk = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdTipoProyectoFk = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proyectos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proyectos_Campus_IdCampusFk",
                        column: x => x.IdCampusFk,
                        principalTable: "Campus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Proyectos_Dependencias_IdDependenciaFk",
                        column: x => x.IdDependenciaFk,
                        principalTable: "Dependencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Proyectos_TiposFondo_IdTipoFondoFk",
                        column: x => x.IdTipoFondoFk,
                        principalTable: "TiposFondo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Proyectos_TiposProyecto_IdTipoProyectoFk",
                        column: x => x.IdTipoProyectoFk,
                        principalTable: "TiposProyecto",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Proyectos_Costos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    ImporteTotal = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    IdProyectoFk = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdConceptoFk = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proyectos_Costos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Proyectos_Costos_Conceptos_IdConceptoFk",
                        column: x => x.IdConceptoFk,
                        principalTable: "Conceptos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Proyectos_Costos_Proyectos_IdProyectoFk",
                        column: x => x.IdProyectoFk,
                        principalTable: "Proyectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conceptos_IdCategoriaFk",
                table: "Conceptos",
                column: "IdCategoriaFk");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_IdCampusFk",
                table: "Proyectos",
                column: "IdCampusFk");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_IdDependenciaFk",
                table: "Proyectos",
                column: "IdDependenciaFk");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_IdTipoFondoFk",
                table: "Proyectos",
                column: "IdTipoFondoFk");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_IdTipoProyectoFk",
                table: "Proyectos",
                column: "IdTipoProyectoFk");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_Costos_IdConceptoFk",
                table: "Proyectos_Costos",
                column: "IdConceptoFk");

            migrationBuilder.CreateIndex(
                name: "IX_Proyectos_Costos_IdProyectoFk",
                table: "Proyectos_Costos",
                column: "IdProyectoFk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Proyectos_Costos");

            migrationBuilder.DropTable(
                name: "Conceptos");

            migrationBuilder.DropTable(
                name: "Proyectos");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Campus");

            migrationBuilder.DropTable(
                name: "Dependencias");

            migrationBuilder.DropTable(
                name: "TiposFondo");

            migrationBuilder.DropTable(
                name: "TiposProyecto");
        }
    }
}
