using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPuntosVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "puntos_venta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SedeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_puntos_venta", x => x.Id);
                    table.UniqueConstraint("AK_puntos_venta_Id_EmpresaId", x => new { x.Id, x.EmpresaId });
                    table.ForeignKey(
                        name: "FK_puntos_venta_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_puntos_venta_sedes_SedeId_EmpresaId",
                        columns: x => new { x.SedeId, x.EmpresaId },
                        principalTable: "sedes",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_puntos_venta_EmpresaId",
                table: "puntos_venta",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_puntos_venta_EmpresaId_SedeId",
                table: "puntos_venta",
                columns: new[] { "EmpresaId", "SedeId" });

            migrationBuilder.CreateIndex(
                name: "IX_puntos_venta_SedeId_EmpresaId",
                table: "puntos_venta",
                columns: new[] { "SedeId", "EmpresaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "puntos_venta");
        }
    }
}
