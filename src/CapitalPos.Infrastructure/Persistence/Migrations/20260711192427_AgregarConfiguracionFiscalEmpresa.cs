using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConfiguracionFiscalEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "configuraciones_fiscales_empresas",
                columns: table => new
                {
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ruc = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NombreComercial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Ubigeo = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Departamento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Provincia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Distrito = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuraciones_fiscales_empresas", x => x.EmpresaId);
                    table.ForeignKey(
                        name: "FK_configuraciones_fiscales_empresas_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_configuraciones_fiscales_empresas_EmpresaId",
                table: "configuraciones_fiscales_empresas",
                column: "EmpresaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "configuraciones_fiscales_empresas");
        }
    }
}
