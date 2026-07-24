using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarReglasPreciosMayoristas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PrecioMayoristaAplicado",
                table: "ventas_detalles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "reglas_precios_mayoristas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CantidadMinima = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitarioMayorista = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reglas_precios_mayoristas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reglas_precios_mayoristas_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reglas_precios_mayoristas_productos_ProductoId_EmpresaId",
                        columns: x => new { x.ProductoId, x.EmpresaId },
                        principalTable: "productos",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reglas_precios_mayoristas_EmpresaId",
                table: "reglas_precios_mayoristas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_reglas_precios_mayoristas_EmpresaId_ProductoId",
                table: "reglas_precios_mayoristas",
                columns: new[] { "EmpresaId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_reglas_precios_mayoristas_EmpresaId_ProductoId_CantidadMini~",
                table: "reglas_precios_mayoristas",
                columns: new[] { "EmpresaId", "ProductoId", "CantidadMinima" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reglas_precios_mayoristas_ProductoId_EmpresaId",
                table: "reglas_precios_mayoristas",
                columns: new[] { "ProductoId", "EmpresaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reglas_precios_mayoristas");

            migrationBuilder.DropColumn(
                name: "PrecioMayoristaAplicado",
                table: "ventas_detalles");
        }
    }
}
