using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CapitalPos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCategoriasYMarcas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoriaId",
                table: "productos",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MarcaId",
                table: "productos",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaPadreId = table.Column<Guid>(type: "uuid", nullable: true),
                    Nombre = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.Id);
                    table.UniqueConstraint("AK_categorias_Id_EmpresaId", x => new { x.Id, x.EmpresaId });
                    table.ForeignKey(
                        name: "FK_categorias_categorias_CategoriaPadreId_EmpresaId",
                        columns: x => new { x.CategoriaPadreId, x.EmpresaId },
                        principalTable: "categorias",
                        principalColumns: new[] { "Id", "EmpresaId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_categorias_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "marcas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmpresaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marcas", x => x.Id);
                    table.UniqueConstraint("AK_marcas_Id_EmpresaId", x => new { x.Id, x.EmpresaId });
                    table.ForeignKey(
                        name: "FK_marcas_empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_productos_CategoriaId_EmpresaId",
                table: "productos",
                columns: new[] { "CategoriaId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_productos_MarcaId_EmpresaId",
                table: "productos",
                columns: new[] { "MarcaId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_categorias_CategoriaPadreId_EmpresaId",
                table: "categorias",
                columns: new[] { "CategoriaPadreId", "EmpresaId" });

            migrationBuilder.CreateIndex(
                name: "IX_categorias_EmpresaId",
                table: "categorias",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_EmpresaId_Nombre",
                table: "categorias",
                columns: new[] { "EmpresaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_marcas_EmpresaId",
                table: "marcas",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_marcas_EmpresaId_Nombre",
                table: "marcas",
                columns: new[] { "EmpresaId", "Nombre" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_productos_categorias_CategoriaId_EmpresaId",
                table: "productos",
                columns: new[] { "CategoriaId", "EmpresaId" },
                principalTable: "categorias",
                principalColumns: new[] { "Id", "EmpresaId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_productos_marcas_MarcaId_EmpresaId",
                table: "productos",
                columns: new[] { "MarcaId", "EmpresaId" },
                principalTable: "marcas",
                principalColumns: new[] { "Id", "EmpresaId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_productos_categorias_CategoriaId_EmpresaId",
                table: "productos");

            migrationBuilder.DropForeignKey(
                name: "FK_productos_marcas_MarcaId_EmpresaId",
                table: "productos");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "marcas");

            migrationBuilder.DropIndex(
                name: "IX_productos_CategoriaId_EmpresaId",
                table: "productos");

            migrationBuilder.DropIndex(
                name: "IX_productos_MarcaId_EmpresaId",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "CategoriaId",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "MarcaId",
                table: "productos");
        }
    }
}
