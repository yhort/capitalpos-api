using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPresentacionesYUnidadesMedida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModoManejo",
                table: "productos",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "SIMPLE");

            migrationBuilder.CreateTable(
                name: "unidades_medida",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unidades_medida", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "productos_presentaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnidadMedidaId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactorConversion = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    EsUnidadBase = table.Column<bool>(type: "boolean", nullable: false),
                    PrecioVenta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CodigoBarras = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_productos_presentaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_productos_presentaciones_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_presentaciones_productos_ProductoId_EmpresaId",
                        columns: x => new { x.ProductoId, x.EmpresaId },
                        principalTable: "productos",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_productos_presentaciones_unidades_medida_UnidadMedidaId",
                        column: x => x.UnidadMedidaId,
                        principalTable: "unidades_medida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_productos_presentaciones_EmpresaId",
                table: "productos_presentaciones",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_productos_presentaciones_EmpresaId_CodigoBarras",
                table: "productos_presentaciones",
                columns: new[] { "EmpresaId", "CodigoBarras" },
                unique: true,
                filter: "\"CodigoBarras\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_productos_presentaciones_EmpresaId_ProductoId",
                table: "productos_presentaciones",
                columns: new[] { "EmpresaId", "ProductoId" });

            migrationBuilder.CreateIndex(
                name: "IX_productos_presentaciones_EmpresaId_ProductoId_EsUnidadBase",
                table: "productos_presentaciones",
                columns: new[] { "EmpresaId", "ProductoId", "EsUnidadBase" },
                unique: true,
                filter: "\"EsUnidadBase\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_productos_presentaciones_EmpresaId_ProductoId_UnidadMedidaId",
                table: "productos_presentaciones",
                columns: new[] { "EmpresaId", "ProductoId", "UnidadMedidaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_productos_presentaciones_ProductoId_EmpresaId",
                table: "productos_presentaciones",
                columns: new[] { "ProductoId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_productos_presentaciones_UnidadMedidaId",
                table: "productos_presentaciones",
                column: "UnidadMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_unidades_medida_Codigo",
                table: "unidades_medida",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "productos_presentaciones");

            migrationBuilder.DropTable(
                name: "unidades_medida");

            migrationBuilder.DropColumn(
                name: "ModoManejo",
                table: "productos");
        }
    }
}
