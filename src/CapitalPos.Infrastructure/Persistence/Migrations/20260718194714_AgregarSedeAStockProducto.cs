using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSedeAStockProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stocks_productos_EmpresaId_ProductoId",
                table: "stocks_productos");

            migrationBuilder.DropIndex(
                name: "IX_stocks_productos_EmpresaId_ProductoId_ProductoVarianteId",
                table: "stocks_productos");

            migrationBuilder.AddColumn<Guid>(
                name: "SedeId",
                table: "stocks_productos",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                WITH sede_por_empresa AS (
                    SELECT DISTINCT ON ("EmpresaId") "EmpresaId", "Id"
                    FROM "sedes"
                    WHERE "Activa" = TRUE
                    ORDER BY "EmpresaId", "FechaCreacion", "Id"
                )
                UPDATE "stocks_productos" AS stock
                SET "SedeId" = sede_por_empresa."Id"
                FROM sede_por_empresa
                WHERE stock."EmpresaId" = sede_por_empresa."EmpresaId";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SedeId",
                table: "stocks_productos",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stocks_productos_EmpresaId_SedeId",
                table: "stocks_productos",
                columns: new[] { "EmpresaId", "SedeId" });

            migrationBuilder.CreateIndex(
                name: "IX_stocks_productos_EmpresaId_SedeId_ProductoId",
                table: "stocks_productos",
                columns: new[] { "EmpresaId", "SedeId", "ProductoId" },
                unique: true,
                filter: "\"ProductoVarianteId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_productos_EmpresaId_SedeId_ProductoVarianteId",
                table: "stocks_productos",
                columns: new[] { "EmpresaId", "SedeId", "ProductoVarianteId" },
                unique: true,
                filter: "\"ProductoVarianteId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stocks_productos_SedeId_EmpresaId",
                table: "stocks_productos",
                columns: new[] { "SedeId", "EmpresaId" });

            migrationBuilder.AddForeignKey(
                name: "FK_stocks_productos_sedes_SedeId_EmpresaId",
                table: "stocks_productos",
                columns: new[] { "SedeId", "EmpresaId" },
                principalTable: "sedes",
                principalColumns: new[] { "Id", "EmpresaId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stocks_productos_sedes_SedeId_EmpresaId",
                table: "stocks_productos");

            migrationBuilder.DropIndex(
                name: "IX_stocks_productos_EmpresaId_SedeId",
                table: "stocks_productos");

            migrationBuilder.DropIndex(
                name: "IX_stocks_productos_EmpresaId_SedeId_ProductoId",
                table: "stocks_productos");

            migrationBuilder.DropIndex(
                name: "IX_stocks_productos_EmpresaId_SedeId_ProductoVarianteId",
                table: "stocks_productos");

            migrationBuilder.DropIndex(
                name: "IX_stocks_productos_SedeId_EmpresaId",
                table: "stocks_productos");

            migrationBuilder.DropColumn(
                name: "SedeId",
                table: "stocks_productos");

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
        }
    }
}
