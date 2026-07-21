using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSesionesCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sesiones_caja",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SedeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PuntoVentaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioAperturaId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsuarioCierreId = table.Column<Guid>(type: "uuid", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MontoInicial = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoDeclaradoCierre = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DiferenciaCierre = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FechaApertura = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FechaCierre = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ObservacionApertura = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ObservacionCierre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sesiones_caja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sesiones_caja_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sesiones_caja_puntos_venta_PuntoVentaId_EmpresaId",
                        columns: x => new { x.PuntoVentaId, x.EmpresaId },
                        principalTable: "puntos_venta",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sesiones_caja_sedes_SedeId_EmpresaId",
                        columns: x => new { x.SedeId, x.EmpresaId },
                        principalTable: "sedes",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_caja_EmpresaId",
                table: "sesiones_caja",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_caja_EmpresaId_PuntoVentaId_Estado",
                table: "sesiones_caja",
                columns: new[] { "EmpresaId", "PuntoVentaId", "Estado" },
                unique: true,
                filter: "\"Estado\" = 'Abierta'");

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_caja_EmpresaId_SedeId",
                table: "sesiones_caja",
                columns: new[] { "EmpresaId", "SedeId" });

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_caja_PuntoVentaId_EmpresaId",
                table: "sesiones_caja",
                columns: new[] { "PuntoVentaId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_sesiones_caja_SedeId_EmpresaId",
                table: "sesiones_caja",
                columns: new[] { "SedeId", "EmpresaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sesiones_caja");
        }
    }
}
