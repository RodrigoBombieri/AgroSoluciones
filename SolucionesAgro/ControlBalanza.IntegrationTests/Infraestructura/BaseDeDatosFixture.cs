using ControlBalanza.Data;
using ControlBalanza.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.IntegrationTests.Infraestructura
{
    /// <summary>
    /// Base de prueba compartida por una clase de tests: se crea con las migraciones reales,
    /// se cargan proveedores, productos y operarios, y se borra al final.
    /// Cada test usa datos propios (CPE, CTG y patentes distintos), así no se pisan entre sí.
    /// </summary>
    public class BaseDeDatosFixture : IAsyncLifetime
    {
        public string Conexion { get; } = SqlServerDePrueba.NuevaBase("Servicios");

        public DateTime Hoy { get; } = new(2026, 10, 31, 15, 0, 0);

        public int ProveedorId { get; private set; }
        public int ProductoSojaId { get; private set; }
        public int ProductoMaizId { get; private set; }
        public const string CuitProveedor = "30-70912345-5";

        public const string OperarioEntrada = "op-entrada";
        public const string OperarioPlayon = "op-playon";
        public const string OperarioSalida = "op-salida";

        private int _secuencia;

        public AgroDbContext NuevoContexto() => SqlServerDePrueba.CrearContexto(Conexion);

        /// <summary>Datos únicos para cada pesaje de prueba.</summary>
        public (string Cpe, string Ctg, string Patente) DatosUnicos()
        {
            var n = Interlocked.Increment(ref _secuencia);
            var letras = $"{(char)('A' + n / 26 % 26)}{(char)('A' + n % 26)}";
            return ($"00009-{n:00000000}", $"101{n:00000000}", $"T{letras}{n % 1000:000}");
        }

        public virtual async Task InitializeAsync()
        {
            await using var ctx = NuevoContexto();
            await ctx.Database.MigrateAsync();

            var proveedor = new Proveedor { RazonSocial = "Agropecuaria Los Talas S.A.", CUIT = CuitProveedor, Localidad = "Urdinarrain" };
            var soja = new Producto { Nombre = "Soja", Variedad = "RR" };
            var maiz = new Producto { Nombre = "Maíz", Variedad = "Común" };
            ctx.AddRange(proveedor, soja, maiz);
            ctx.Users.AddRange(
                new Usuario { Id = OperarioEntrada, UserName = "entrada", Nombre = "Marcos", Apellido = "Ríos" },
                new Usuario { Id = OperarioPlayon, UserName = "playon", Nombre = "Julián", Apellido = "Sosa" },
                new Usuario { Id = OperarioSalida, UserName = "salida", Nombre = "Laura", Apellido = "Benítez" });
            await ctx.SaveChangesAsync();

            ProveedorId = proveedor.Id;
            ProductoSojaId = soja.Id;
            ProductoMaizId = maiz.Id;
        }

        public async Task DisposeAsync()
        {
            await using var ctx = NuevoContexto();
            await ctx.Database.EnsureDeletedAsync();
        }
    }
}
