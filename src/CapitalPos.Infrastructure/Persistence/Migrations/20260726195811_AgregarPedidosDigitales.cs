using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPedidosDigitales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pedidos_digitales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    SedeId = table.Column<Guid>(type: "uuid", nullable: false),
                    PuntoVentaId = table.Column<Guid>(type: "uuid", nullable: true),
                    CanalPedido = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FechaPedido = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Igv = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenciaExterna = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedidos_digitales", x => x.Id);
                    table.UniqueConstraint("AK_pedidos_digitales_Id_EmpresaId", x => new { x.Id, x.EmpresaId });
                    table.ForeignKey(
                        name: "FK_pedidos_digitales_clientes_ClienteId_EmpresaId",
                        columns: x => new { x.ClienteId, x.EmpresaId },
                        principalTable: "clientes",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_digitales_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_digitales_puntos_venta_PuntoVentaId_EmpresaId",
                        columns: x => new { x.PuntoVentaId, x.EmpresaId },
                        principalTable: "puntos_venta",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_digitales_sedes_SedeId_EmpresaId",
                        columns: x => new { x.SedeId, x.EmpresaId },
                        principalTable: "sedes",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pedidos_digitales_detalles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PedidoDigitalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductoVarianteId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductoPresentacionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FactorConversionAplicado = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    CantidadBase = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedidos_digitales_detalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pedidos_digitales_detalles_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_digitales_detalles_pedidos_digitales_PedidoDigitalI~",
                        columns: x => new { x.PedidoDigitalId, x.EmpresaId },
                        principalTable: "pedidos_digitales",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_digitales_detalles_productos_ProductoId_EmpresaId",
                        columns: x => new { x.ProductoId, x.EmpresaId },
                        principalTable: "productos",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_digitales_detalles_productos_presentaciones_Product~",
                        columns: x => new { x.ProductoPresentacionId, x.EmpresaId },
                        principalTable: "productos_presentaciones",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_digitales_detalles_productos_variantes_ProductoVari~",
                        columns: x => new { x.ProductoVarianteId, x.EmpresaId },
                        principalTable: "productos_variantes",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pedidos_digitales_historial_estados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PedidoDigitalId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    EstadoNuevo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Fecha = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pedidos_digitales_historial_estados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pedidos_digitales_historial_estados_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_digitales_historial_estados_pedidos_digitales_Pedid~",
                        columns: x => new { x.PedidoDigitalId, x.EmpresaId },
                        principalTable: "pedidos_digitales",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_pedidos_digitales_historial_estados_usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_ClienteId_EmpresaId",
                table: "pedidos_digitales",
                columns: new[] { "ClienteId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_EmpresaId",
                table: "pedidos_digitales",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_EmpresaId_CanalPedido_FechaPedido",
                table: "pedidos_digitales",
                columns: new[] { "EmpresaId", "CanalPedido", "FechaPedido" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_EmpresaId_Estado_FechaPedido",
                table: "pedidos_digitales",
                columns: new[] { "EmpresaId", "Estado", "FechaPedido" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_PuntoVentaId_EmpresaId",
                table: "pedidos_digitales",
                columns: new[] { "PuntoVentaId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_SedeId_EmpresaId",
                table: "pedidos_digitales",
                columns: new[] { "SedeId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_detalles_EmpresaId",
                table: "pedidos_digitales_detalles",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_detalles_EmpresaId_PedidoDigitalId",
                table: "pedidos_digitales_detalles",
                columns: new[] { "EmpresaId", "PedidoDigitalId" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_detalles_PedidoDigitalId_EmpresaId",
                table: "pedidos_digitales_detalles",
                columns: new[] { "PedidoDigitalId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_detalles_ProductoId_EmpresaId",
                table: "pedidos_digitales_detalles",
                columns: new[] { "ProductoId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_detalles_ProductoPresentacionId_EmpresaId",
                table: "pedidos_digitales_detalles",
                columns: new[] { "ProductoPresentacionId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_detalles_ProductoVarianteId_EmpresaId",
                table: "pedidos_digitales_detalles",
                columns: new[] { "ProductoVarianteId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_historial_estados_EmpresaId",
                table: "pedidos_digitales_historial_estados",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_historial_estados_EmpresaId_PedidoDigital~",
                table: "pedidos_digitales_historial_estados",
                columns: new[] { "EmpresaId", "PedidoDigitalId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_historial_estados_PedidoDigitalId_Empresa~",
                table: "pedidos_digitales_historial_estados",
                columns: new[] { "PedidoDigitalId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_pedidos_digitales_historial_estados_UsuarioId",
                table: "pedidos_digitales_historial_estados",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pedidos_digitales_detalles");

            migrationBuilder.DropTable(
                name: "pedidos_digitales_historial_estados");

            migrationBuilder.DropTable(
                name: "pedidos_digitales");
        }
    }
}
