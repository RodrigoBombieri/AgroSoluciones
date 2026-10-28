using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlBalanza.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// - Pesajes: columnas SiloDestino y Observaciones (las carga el playón / balanza de salida).
    /// - CPE única por pesaje y CUIT único por proveedor (si hay duplicados en la base, la migración falla: corregirlos antes).
    /// - Índices por Estado y FechaEntrada (panel del playón y reportes).
    /// - Borrar un proveedor o producto ya no borra en cascada sus pesajes (Restrict).
    /// </remarks>
    public partial class FlujoPesajeYValidaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pesajes_Productos_ProductoId",
                table: "Pesajes");

            migrationBuilder.DropForeignKey(
                name: "FK_Pesajes_Proveedores_ProveedorId",
                table: "Pesajes");

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "Pesajes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SiloDestino",
                table: "Pesajes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_CUIT",
                table: "Proveedores",
                column: "CUIT",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pesajes_Estado",
                table: "Pesajes",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Pesajes_FechaEntrada",
                table: "Pesajes",
                column: "FechaEntrada");

            migrationBuilder.CreateIndex(
                name: "IX_Pesajes_NumeroCPE",
                table: "Pesajes",
                column: "NumeroCPE",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Pesajes_Productos_ProductoId",
                table: "Pesajes",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pesajes_Proveedores_ProveedorId",
                table: "Pesajes",
                column: "ProveedorId",
                principalTable: "Proveedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pesajes_Productos_ProductoId",
                table: "Pesajes");

            migrationBuilder.DropForeignKey(
                name: "FK_Pesajes_Proveedores_ProveedorId",
                table: "Pesajes");

            migrationBuilder.DropIndex(
                name: "IX_Proveedores_CUIT",
                table: "Proveedores");

            migrationBuilder.DropIndex(
                name: "IX_Pesajes_Estado",
                table: "Pesajes");

            migrationBuilder.DropIndex(
                name: "IX_Pesajes_FechaEntrada",
                table: "Pesajes");

            migrationBuilder.DropIndex(
                name: "IX_Pesajes_NumeroCPE",
                table: "Pesajes");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "Pesajes");

            migrationBuilder.DropColumn(
                name: "SiloDestino",
                table: "Pesajes");

            migrationBuilder.AddForeignKey(
                name: "FK_Pesajes_Productos_ProductoId",
                table: "Pesajes",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pesajes_Proveedores_ProveedorId",
                table: "Pesajes",
                column: "ProveedorId",
                principalTable: "Proveedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
