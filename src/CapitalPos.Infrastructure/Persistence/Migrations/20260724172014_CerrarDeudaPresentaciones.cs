using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CerrarDeudaPresentaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockActual",
                table: "productos_variantes");

            migrationBuilder.AddColumn<decimal>(
                name: "CantidadBaseDescontada",
                table: "ventas_detalles",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FactorConversionAplicado",
                table: "ventas_detalles",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.Sql(
                """
                UPDATE "ventas_detalles"
                SET "CantidadBaseDescontada" = "Cantidad",
                    "FactorConversionAplicado" = 1
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CantidadBaseDescontada",
                table: "ventas_detalles");

            migrationBuilder.DropColumn(
                name: "FactorConversionAplicado",
                table: "ventas_detalles");

            migrationBuilder.AddColumn<decimal>(
                name: "StockActual",
                table: "productos_variantes",
                type: "numeric(18,3)",
                precision: 18,
                scale: 3,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
