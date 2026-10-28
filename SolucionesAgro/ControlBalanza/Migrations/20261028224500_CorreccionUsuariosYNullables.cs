using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlBalanza.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// - Pasa las FK de usuario de Pesajes de int a nvarchar(450) (Id de Identity) y crea las relaciones con AspNetUsers.
    /// - Hace opcionales: Pesajes.NombreChofer, Pesajes.PatenteAcoplado, Proveedores.Localidad,
    ///   Proveedores.Telefono y AspNetUsers.ImagenUrlPerfil.
    /// IMPORTANTE: si la tabla Pesajes tiene filas con Ids de usuario int, las FK van a fallar. Vaciarla antes.
    /// </remarks>
    public partial class CorreccionUsuariosYNullables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Pesajes: FKs de usuario int -> string ---
            migrationBuilder.AlterColumn<string>(
                name: "UsuarioBalanzaEntradaId",
                table: "Pesajes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioPlayonId",
                table: "Pesajes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UsuarioBalanzaSalidaId",
                table: "Pesajes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // --- Columnas que pasan a ser opcionales ---
            migrationBuilder.AlterColumn<string>(
                name: "PatenteAcoplado",
                table: "Pesajes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "NombreChofer",
                table: "Pesajes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Localidad",
                table: "Proveedores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Proveedores",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ImagenUrlPerfil",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            // --- Índices y relaciones con AspNetUsers ---
            migrationBuilder.CreateIndex(
                name: "IX_Pesajes_UsuarioBalanzaEntradaId",
                table: "Pesajes",
                column: "UsuarioBalanzaEntradaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pesajes_UsuarioBalanzaSalidaId",
                table: "Pesajes",
                column: "UsuarioBalanzaSalidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Pesajes_UsuarioPlayonId",
                table: "Pesajes",
                column: "UsuarioPlayonId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pesajes_AspNetUsers_UsuarioBalanzaEntradaId",
                table: "Pesajes",
                column: "UsuarioBalanzaEntradaId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pesajes_AspNetUsers_UsuarioBalanzaSalidaId",
                table: "Pesajes",
                column: "UsuarioBalanzaSalidaId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Pesajes_AspNetUsers_UsuarioPlayonId",
                table: "Pesajes",
                column: "UsuarioPlayonId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pesajes_AspNetUsers_UsuarioBalanzaEntradaId",
                table: "Pesajes");

            migrationBuilder.DropForeignKey(
                name: "FK_Pesajes_AspNetUsers_UsuarioBalanzaSalidaId",
                table: "Pesajes");

            migrationBuilder.DropForeignKey(
                name: "FK_Pesajes_AspNetUsers_UsuarioPlayonId",
                table: "Pesajes");

            migrationBuilder.DropIndex(
                name: "IX_Pesajes_UsuarioBalanzaEntradaId",
                table: "Pesajes");

            migrationBuilder.DropIndex(
                name: "IX_Pesajes_UsuarioBalanzaSalidaId",
                table: "Pesajes");

            migrationBuilder.DropIndex(
                name: "IX_Pesajes_UsuarioPlayonId",
                table: "Pesajes");

            migrationBuilder.AlterColumn<string>(
                name: "ImagenUrlPerfil",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Telefono",
                table: "Proveedores",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Localidad",
                table: "Proveedores",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NombreChofer",
                table: "Pesajes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PatenteAcoplado",
                table: "Pesajes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioBalanzaSalidaId",
                table: "Pesajes",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioPlayonId",
                table: "Pesajes",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UsuarioBalanzaEntradaId",
                table: "Pesajes",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
