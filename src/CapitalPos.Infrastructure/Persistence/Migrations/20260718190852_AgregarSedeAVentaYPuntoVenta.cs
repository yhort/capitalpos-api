using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSedeAVentaYPuntoVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SedeId",
                table: "ventas",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "ventas" AS venta
                SET "SedeId" = punto_venta."SedeId"
                FROM "puntos_venta" AS punto_venta
                WHERE venta."PuntoVentaId" IS NOT NULL
                    AND venta."PuntoVentaId" = punto_venta."Id"
                    AND venta."EmpresaId" = punto_venta."EmpresaId";
                """);

            migrationBuilder.Sql("""
                WITH punto_venta_por_empresa AS (
                    SELECT DISTINCT ON ("EmpresaId") "EmpresaId", "Id", "SedeId"
                    FROM "puntos_venta"
                    WHERE "Activo" = TRUE
                    ORDER BY "EmpresaId", "FechaCreacion", "Id"
                )
                UPDATE "ventas" AS venta
                SET
                    "PuntoVentaId" = punto_venta_por_empresa."Id",
                    "SedeId" = punto_venta_por_empresa."SedeId"
                FROM punto_venta_por_empresa
                WHERE venta."PuntoVentaId" IS NULL
                    AND venta."EmpresaId" = punto_venta_por_empresa."EmpresaId";
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "PuntoVentaId",
                table: "ventas",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SedeId",
                table: "ventas",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ventas_EmpresaId_SedeId",
                table: "ventas",
                columns: new[] { "EmpresaId", "SedeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_PuntoVentaId_EmpresaId",
                table: "ventas",
                columns: new[] { "PuntoVentaId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_SedeId_EmpresaId",
                table: "ventas",
                columns: new[] { "SedeId", "EmpresaId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ventas_puntos_venta_PuntoVentaId_EmpresaId",
                table: "ventas",
                columns: new[] { "PuntoVentaId", "EmpresaId" },
                principalTable: "puntos_venta",
                principalColumns: new[] { "Id", "EmpresaId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ventas_sedes_SedeId_EmpresaId",
                table: "ventas",
                columns: new[] { "SedeId", "EmpresaId" },
                principalTable: "sedes",
                principalColumns: new[] { "Id", "EmpresaId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ventas_puntos_venta_PuntoVentaId_EmpresaId",
                table: "ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_ventas_sedes_SedeId_EmpresaId",
                table: "ventas");

            migrationBuilder.DropIndex(
                name: "IX_ventas_EmpresaId_SedeId",
                table: "ventas");

            migrationBuilder.DropIndex(
                name: "IX_ventas_PuntoVentaId_EmpresaId",
                table: "ventas");

            migrationBuilder.DropIndex(
                name: "IX_ventas_SedeId_EmpresaId",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "SedeId",
                table: "ventas");

            migrationBuilder.AlterColumn<Guid>(
                name: "PuntoVentaId",
                table: "ventas",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
