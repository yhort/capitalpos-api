using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCompras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SedeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Proveedor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TipoComprobante = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Serie = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Correlativo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaCompra = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras", x => x.Id);
                    table.UniqueConstraint("AK_compras_Id_EmpresaId", x => new { x.Id, x.EmpresaId });
                    table.ForeignKey(
                        name: "FK_compras_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_sedes_SedeId_EmpresaId",
                        columns: x => new { x.SedeId, x.EmpresaId },
                        principalTable: "sedes",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compras_detalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompraId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoVarianteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compras_detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_compras_detalles_compras_CompraId_EmpresaId",
                        columns: x => new { x.CompraId, x.EmpresaId },
                        principalTable: "compras",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_detalles_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_detalles_productos_ProductoId_EmpresaId",
                        columns: x => new { x.ProductoId, x.EmpresaId },
                        principalTable: "productos",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compras_detalles_productos_variantes_ProductoVarianteId_Emp~",
                        columns: x => new { x.ProductoVarianteId, x.EmpresaId },
                        principalTable: "productos_variantes",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_compras_EmpresaId",
                table: "compras",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_compras_EmpresaId_SedeId",
                table: "compras",
                columns: new[] { "EmpresaId", "SedeId" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_EmpresaId_TipoComprobante_Serie_Correlativo",
                table: "compras",
                columns: new[] { "EmpresaId", "TipoComprobante", "Serie", "Correlativo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compras_SedeId_EmpresaId",
                table: "compras",
                columns: new[] { "SedeId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_CompraId_EmpresaId",
                table: "compras_detalles",
                columns: new[] { "CompraId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_EmpresaId",
                table: "compras_detalles",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_EmpresaId_CompraId",
                table: "compras_detalles",
                columns: new[] { "EmpresaId", "CompraId" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_ProductoId_EmpresaId",
                table: "compras_detalles",
                columns: new[] { "ProductoId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_compras_detalles_ProductoVarianteId_EmpresaId",
                table: "compras_detalles",
                columns: new[] { "ProductoVarianteId", "EmpresaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compras_detalles");

            migrationBuilder.DropTable(
                name: "compras");
        }
    }
}
