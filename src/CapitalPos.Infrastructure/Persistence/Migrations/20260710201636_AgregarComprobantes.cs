using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarComprobantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "comprobantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    VentaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoComprobante = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Serie = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Correlativo = table.Column<int>(type: "integer", nullable: false),
                    EstadoCpe = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Mensaje = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NombreXml = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NombreZip = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NombreCdr = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comprobantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comprobantes_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_comprobantes_ventas_VentaId_EmpresaId",
                        columns: x => new { x.VentaId, x.EmpresaId },
                        principalTable: "ventas",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_EmpresaId",
                table: "comprobantes",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_EmpresaId_TipoComprobante_Serie_Correlativo",
                table: "comprobantes",
                columns: new[] { "EmpresaId", "TipoComprobante", "Serie", "Correlativo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_comprobantes_VentaId_EmpresaId",
                table: "comprobantes",
                columns: new[] { "VentaId", "EmpresaId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comprobantes");
        }
    }
}
