using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDimensionesComercialesVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanalVenta",
                table: "ventas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "TIENDA");

            migrationBuilder.AddColumn<Guid>(
                name: "PuntoVentaId",
                table: "ventas",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "VendedorId",
                table: "ventas",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ventas_EmpresaId_CanalVenta_Fecha",
                table: "ventas",
                columns: new[] { "EmpresaId", "CanalVenta", "Fecha" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ventas_EmpresaId_CanalVenta_Fecha",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "CanalVenta",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "PuntoVentaId",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "VendedorId",
                table: "ventas");
        }
    }
}
