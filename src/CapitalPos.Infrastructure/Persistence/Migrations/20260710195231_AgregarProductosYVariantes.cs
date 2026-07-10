using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarProductosYVariantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "productos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CodigoSku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Costo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos", x => x.Id);
                    table.UniqueConstraint("AK_productos_Id_EmpresaId", x => new { x.Id, x.EmpresaId });
                    table.ForeignKey(
                        name: "FK_productos_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "productos_variantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Talla = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CodigoSku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    StockActual = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos_variantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_productos_variantes_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_variantes_productos_ProductoId_EmpresaId",
                        columns: x => new { x.ProductoId, x.EmpresaId },
                        principalTable: "productos",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_productos_EmpresaId",
                table: "productos",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_productos_EmpresaId_CodigoBarras",
                table: "productos",
                columns: new[] { "EmpresaId", "CodigoBarras" },
                unique: true,
                filter: "\"CodigoBarras\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_productos_EmpresaId_CodigoSku",
                table: "productos",
                columns: new[] { "EmpresaId", "CodigoSku" },
                unique: true,
                filter: "\"CodigoSku\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_productos_variantes_EmpresaId",
                table: "productos_variantes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_productos_variantes_EmpresaId_CodigoBarras",
                table: "productos_variantes",
                columns: new[] { "EmpresaId", "CodigoBarras" },
                unique: true,
                filter: "\"CodigoBarras\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_productos_variantes_EmpresaId_CodigoSku",
                table: "productos_variantes",
                columns: new[] { "EmpresaId", "CodigoSku" },
                unique: true,
                filter: "\"CodigoSku\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_productos_variantes_EmpresaId_ProductoId",
                table: "productos_variantes",
                columns: new[] { "EmpresaId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_productos_variantes_ProductoId_EmpresaId",
                table: "productos_variantes",
                columns: new[] { "ProductoId", "EmpresaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "productos_variantes");

            migrationBuilder.DropTable(
                name: "productos");
        }
    }
}
