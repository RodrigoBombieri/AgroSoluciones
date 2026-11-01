using ControlBalanza.Data;
using ControlBalanza.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ControlBalanza.IntegrationTests.Infraestructura
{
    /// <summary>Arma la cadena de conexión a una base de prueba nueva (nombre único) en el SQL Server configurado.</summary>
    public static class SqlServerDePrueba
    {
        public const string VariableEntorno = "AGRO_TEST_SQLSERVER";
        private const string ServidorLocal = @"Server=.\SQLEXPRESS01;Integrated Security=true;TrustServerCertificate=true";

        public static string NuevaBase(string prefijo)
        {
            var servidor = Environment.GetEnvironmentVariable(VariableEntorno);
            var nombre = $"AgroTests_{prefijo}_{Guid.NewGuid():N}";
            var builder = new SqlConnectionStringBuilder(string.IsNullOrWhiteSpace(servidor) ? ServidorLocal : servidor)
            {
                InitialCatalog = nombre.Length > 100 ? nombre[..100] : nombre,
                ConnectTimeout = 30
            };
            return builder.ConnectionString;
        }

        /// <summary>
        /// Contexto con el mismo modelo que usa la app. IdentityDbContext arma sus tablas según las opciones
        /// de Identity registradas en DI (AddDefaultIdentity), así que un contexto creado "a mano" sin ellas
        /// tiene otro modelo y MigrateAsync lo rechaza por "pending model changes".
        /// </summary>
        public static AgroDbContext CrearContexto(string conexion) =>
            new(new DbContextOptionsBuilder<AgroDbContext>()
                .UseSqlServer(conexion)
                .UseApplicationServiceProvider(ServiciosApp.Value)
                .Options);

        // Un único proveedor compartido: EF cachea su configuración interna por proveedor
        private static readonly Lazy<IServiceProvider> ServiciosApp = new(() =>
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDefaultIdentity<Usuario>().AddRoles<IdentityRole>();
            return services.BuildServiceProvider();
        });
    }
}
