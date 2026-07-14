using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarStockProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stocks_productos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoVarianteId = table.Column<Guid>(type: "uuid", nullable: true),
                    CantidadDisponible = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CantidadReservada = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stocks_productos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stocks_productos_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stocks_productos_productos_ProductoId_EmpresaId",
                        columns: x => new { x.ProductoId, x.EmpresaId },
                        principalTable: "productos",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stocks_productos_productos_variantes_ProductoVarianteId_Emp~",
                        columns: x => new { x.ProductoVarianteId, x.EmpresaId },
                        principalTable: "productos_variantes",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stocks_productos_EmpresaId",
                table: "stocks_productos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_productos_EmpresaId_ProductoId",
                table: "stocks_productos",
                columns: new[] { "EmpresaId", "ProductoId" },
                unique: true,
                filter: "\"ProductoVarianteId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_productos_EmpresaId_ProductoId_ProductoVarianteId",
                table: "stocks_productos",
                columns: new[] { "EmpresaId", "ProductoId", "ProductoVarianteId" },
                unique: true,
                filter: "\"ProductoVarianteId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_productos_ProductoId_EmpresaId",
                table: "stocks_productos",
                columns: new[] { "ProductoId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_stocks_productos_ProductoVarianteId_EmpresaId",
                table: "stocks_productos",
                columns: new[] { "ProductoVarianteId", "EmpresaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stocks_productos");
        }
    }
}
