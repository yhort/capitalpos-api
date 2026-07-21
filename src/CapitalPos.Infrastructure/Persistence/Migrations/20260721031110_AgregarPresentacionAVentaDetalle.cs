using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPresentacionAVentaDetalle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductoPresentacionId",
                table: "ventas_detalles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_productos_presentaciones_Id_EmpresaId",
                table: "productos_presentaciones",
                columns: new[] { "Id", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ventas_detalles_ProductoPresentacionId_EmpresaId",
                table: "ventas_detalles",
                columns: new[] { "ProductoPresentacionId", "EmpresaId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ventas_detalles_productos_presentaciones_ProductoPresentaci~",
                table: "ventas_detalles",
                columns: new[] { "ProductoPresentacionId", "EmpresaId" },
                principalTable: "productos_presentaciones",
                principalColumns: new[] { "Id", "EmpresaId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ventas_detalles_productos_presentaciones_ProductoPresentaci~",
                table: "ventas_detalles");

            migrationBuilder.DropIndex(
                name: "IX_ventas_detalles_ProductoPresentacionId_EmpresaId",
                table: "ventas_detalles");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_productos_presentaciones_Id_EmpresaId",
                table: "productos_presentaciones");

            migrationBuilder.DropColumn(
                name: "ProductoPresentacionId",
                table: "ventas_detalles");
        }
    }
}
