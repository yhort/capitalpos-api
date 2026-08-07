using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarReferenciaNotaCreditoComprobante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoMotivo",
                table: "comprobantes",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ComprobanteAfectadoId",
                table: "comprobantes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorrelativoAfectado",
                table: "comprobantes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescripcionMotivo",
                table: "comprobantes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SerieAfectada",
                table: "comprobantes",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoComprobanteAfectado",
                table: "comprobantes",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_ComprobanteAfectadoId",
                table: "comprobantes",
                column: "ComprobanteAfectadoId");

            migrationBuilder.AddForeignKey(
                name: "FK_comprobantes_comprobantes_ComprobanteAfectadoId",
                table: "comprobantes",
                column: "ComprobanteAfectadoId",
                principalTable: "comprobantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comprobantes_comprobantes_ComprobanteAfectadoId",
                table: "comprobantes");

            migrationBuilder.DropIndex(
                name: "IX_comprobantes_ComprobanteAfectadoId",
                table: "comprobantes");

            migrationBuilder.DropColumn(
                name: "CodigoMotivo",
                table: "comprobantes");

            migrationBuilder.DropColumn(
                name: "ComprobanteAfectadoId",
                table: "comprobantes");

            migrationBuilder.DropColumn(
                name: "CorrelativoAfectado",
                table: "comprobantes");

            migrationBuilder.DropColumn(
                name: "DescripcionMotivo",
                table: "comprobantes");

            migrationBuilder.DropColumn(
                name: "SerieAfectada",
                table: "comprobantes");

            migrationBuilder.DropColumn(
                name: "TipoComprobanteAfectado",
                table: "comprobantes");
        }
    }
}
