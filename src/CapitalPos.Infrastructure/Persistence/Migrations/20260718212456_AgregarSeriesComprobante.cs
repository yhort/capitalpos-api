using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarSeriesComprobante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "series_comprobante",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    SedeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoComprobante = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Serie = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CorrelativoActual = table.Column<int>(type: "integer", nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_series_comprobante", x => x.Id);
                    table.ForeignKey(
                        name: "FK_series_comprobante_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_series_comprobante_sedes_SedeId_EmpresaId",
                        columns: x => new { x.SedeId, x.EmpresaId },
                        principalTable: "sedes",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_series_comprobante_EmpresaId",
                table: "series_comprobante",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_series_comprobante_EmpresaId_SedeId",
                table: "series_comprobante",
                columns: new[] { "EmpresaId", "SedeId" });

            migrationBuilder.CreateIndex(
                name: "IX_series_comprobante_EmpresaId_SedeId_TipoComprobante_Serie",
                table: "series_comprobante",
                columns: new[] { "EmpresaId", "SedeId", "TipoComprobante", "Serie" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_series_comprobante_SedeId_EmpresaId",
                table: "series_comprobante",
                columns: new[] { "SedeId", "EmpresaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "series_comprobante");
        }
    }
}
