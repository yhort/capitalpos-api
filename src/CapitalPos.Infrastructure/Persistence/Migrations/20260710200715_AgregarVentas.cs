using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarVentas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_productos_variantes_Id_EmpresaId",
                table: "productos_variantes",
                columns: new[] { "Id", "EmpresaId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_clientes_Id_EmpresaId",
                table: "clientes",
                columns: new[] { "Id", "EmpresaId" });

            migrationBuilder.CreateTable(
                name: "ventas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Igv = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas", x => x.Id);
                    table.UniqueConstraint("AK_ventas_Id_EmpresaId", x => new { x.Id, x.EmpresaId });
                    table.ForeignKey(
                        name: "FK_ventas_clientes_ClienteId_EmpresaId",
                        columns: x => new { x.ClienteId, x.EmpresaId },
                        principalTable: "clientes",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ventas_detalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    VentaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoVarianteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Igv = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ventas_detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ventas_detalles_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_detalles_productos_ProductoId_EmpresaId",
                        columns: x => new { x.ProductoId, x.EmpresaId },
                        principalTable: "productos",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_detalles_productos_variantes_ProductoVarianteId_Empr~",
                        columns: x => new { x.ProductoVarianteId, x.EmpresaId },
                        principalTable: "productos_variantes",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ventas_detalles_ventas_VentaId_EmpresaId",
                        columns: x => new { x.VentaId, x.EmpresaId },
                        principalTable: "ventas",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_ClienteId_EmpresaId",
                table: "ventas",
                columns: new[] { "ClienteId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_EmpresaId",
                table: "ventas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_detalles_EmpresaId",
                table: "ventas_detalles",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_detalles_EmpresaId_VentaId",
                table: "ventas_detalles",
                columns: new[] { "EmpresaId", "VentaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_detalles_ProductoId_EmpresaId",
                table: "ventas_detalles",
                columns: new[] { "ProductoId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_detalles_ProductoVarianteId_EmpresaId",
                table: "ventas_detalles",
                columns: new[] { "ProductoVarianteId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_detalles_VentaId_EmpresaId",
                table: "ventas_detalles",
                columns: new[] { "VentaId", "EmpresaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ventas_detalles");

            migrationBuilder.DropTable(
                name: "ventas");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_productos_variantes_Id_EmpresaId",
                table: "productos_variantes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_clientes_Id_EmpresaId",
                table: "clientes");
        }
    }
}
