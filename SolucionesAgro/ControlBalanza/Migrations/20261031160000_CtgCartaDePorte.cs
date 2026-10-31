using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlBalanza.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// Agrega el CTG (Código de Trazabilidad de Granos) de la CPE: es el identificador oficial de ARCA
    /// y lo único que trae el QR. Opcional (los pesajes anteriores no lo tienen) y único cuando se carga.
    /// </remarks>
    public partial class CtgCartaDePorte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Ctg",
                table: "Pesajes",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pesajes_Ctg",
                table: "Pesajes",
                column: "Ctg",
                unique: true,
                filter: "[Ctg] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pesajes_Ctg",
                table: "Pesajes");

            migrationBuilder.DropColumn(
                name: "Ctg",
                table: "Pesajes");
        }
    }
}
