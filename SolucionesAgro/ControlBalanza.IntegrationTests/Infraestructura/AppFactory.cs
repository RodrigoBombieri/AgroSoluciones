using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.IntegrationTests.Infraestructura
{
    /// <summary>
    /// Levanta la app real en memoria contra una base de prueba propia. Al arrancar, la app aplica
    /// las migraciones y crea los usuarios de prueba (entorno Development), igual que en una PC de desarrollo.
    /// </summary>
    public class AppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const string Password = "Agro1234";
        public const string Admin = "admin@admin.com";
        public const string PasswordAdmin = "Admin123";
        public const string Entrada = "entrada@agro.local";
        public const string Playon = "playon@agro.local";
        public const string Salida = "salida@agro.local";

        public string Conexion { get; } = SqlServerDePrueba.NuevaBase("Http");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:DbConnection", Conexion);
        }

        /// <summary>Cliente que no sigue redirecciones (para poder verificarlas) y guarda las cookies.</summary>
        public HttpClient NuevoCliente() =>
            CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

        /// <summary>Cliente ya logueado con el usuario indicado.</summary>
        public async Task<HttpClient> ClienteLogueado(string email, string password = Password)
        {
            var cliente = NuevoCliente();
            var respuesta = await Login(cliente, email, password);
            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            return cliente;
        }

        public static async Task<HttpResponseMessage> Login(HttpClient cliente, string email, string password)
        {
            var token = await TokenAntiforgery(cliente, "/Identity/Account/Login");
            return await cliente.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Email"] = email,
                ["Input.Password"] = password,
                ["__RequestVerificationToken"] = token
            }));
        }

        /// <summary>HTML de la respuesta ya decodificado (Razor codifica las tildes como entidades).</summary>
        public static async Task<string> Html(HttpResponseMessage respuesta) =>
            WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        public static async Task<string> Html(HttpClient cliente, string url) =>
            await Html(await cliente.GetAsync(url));

        /// <summary>Ruta a la que redirige una respuesta (la Location puede venir absoluta o relativa).</summary>
        public static string Destino(HttpResponseMessage respuesta)
        {
            var location = respuesta.Headers.Location ?? throw new InvalidOperationException("La respuesta no es una redirección");
            return location.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
        }

        /// <summary>Lee el token antiforgery de un formulario (y deja la cookie correspondiente en el cliente).</summary>
        public static async Task<string> TokenAntiforgery(HttpClient cliente, string url)
        {
            var html = await cliente.GetStringAsync(url);
            var m = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
            Assert.True(m.Success, $"No se encontró el token antiforgery en {url}");
            return m.Groups[1].Value;
        }

        /// <summary>Token para la API del playón: la vista Playon lo publica en un meta tag.</summary>
        public static async Task<string> TokenApiPlayon(HttpClient cliente)
        {
            var html = await cliente.GetStringAsync("/Home/Playon");
            var m = Regex.Match(html, "<meta name=\"xsrf-token\" content=\"([^\"]+)\"");
            Assert.True(m.Success, "No se encontró el meta xsrf-token en /Home/Playon");
            return m.Groups[1].Value;
        }

        public Task InitializeAsync()
        {
            _ = Server; // fuerza el arranque: migraciones + datos iniciales
            return Task.CompletedTask;
        }

        async Task IAsyncLifetime.DisposeAsync()
        {
            await using (var ctx = SqlServerDePrueba.CrearContexto(Conexion))
                await ctx.Database.EnsureDeletedAsync();
            await base.DisposeAsync();
        }
    }
}
