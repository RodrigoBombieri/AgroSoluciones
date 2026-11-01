using System.Net;
using System.Net.Http.Json;
using ControlBalanza.IntegrationTests.Infraestructura;

namespace ControlBalanza.IntegrationTests.Http
{
    /// <summary>Login, roles y protecciones de la app completa.</summary>
    public class SeguridadTests : IClassFixture<AppFactory>
    {
        private readonly AppFactory _app;

        public SeguridadTests(AppFactory app) => _app = app;

        [Theory]
        [InlineData("/")]
        [InlineData("/Pesajes")]
        [InlineData("/Reportes")]
        [InlineData("/Usuarios")]
        public async Task SinLogin_RedirigeAlLogin(string url)
        {
            var r = await _app.NuevoCliente().GetAsync(url);
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.StartsWith("/Identity/Account/Login", AppFactory.Destino(r));
        }

        [Fact]
        public async Task PaginaDeLogin_EsPublicaYEnEspañol()
        {
            var r = await _app.NuevoCliente().GetAsync("/Identity/Account/Login");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("Ingresar", await AppFactory.Html(r));
        }

        [Fact]
        public async Task ArchivosEstaticos_NoRequierenLogin()
        {
            var r = await _app.NuevoCliente().GetAsync("/css/site.css");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        [Theory]
        [InlineData("/Identity/Account/Register")]
        [InlineData("/Identity/Account/ExternalLogin")]
        public async Task RegistroPublico_EstaBloqueado(string url)
        {
            var r = await _app.NuevoCliente().GetAsync(url);
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal("/Identity/Account/Login", AppFactory.Destino(r));
        }

        [Fact]
        public async Task Login_ConPasswordIncorrecta_MuestraError()
        {
            var r = await AppFactory.Login(_app.NuevoCliente(), AppFactory.Admin, "incorrecta");
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("Email o contraseña incorrectos", await AppFactory.Html(r));
        }

        [Fact]
        public async Task Admin_AccedeATodo()
        {
            var cliente = await _app.ClienteLogueado(AppFactory.Admin, AppFactory.PasswordAdmin);
            foreach (var url in new[] { "/", "/Pesajes/Create", "/Pesajes/Salida", "/Home/Playon", "/Reportes", "/Usuarios", "/Proveedores", "/Productos" })
            {
                var r = await cliente.GetAsync(url);
                Assert.True(r.StatusCode == HttpStatusCode.OK, $"{url} devolvió {(int)r.StatusCode}");
            }
        }

        [Theory]
        [InlineData(AppFactory.Entrada, "/Pesajes/Create", "/Pesajes/Salida")]
        [InlineData(AppFactory.Playon, "/Home/Playon", "/Pesajes/Create")]
        [InlineData(AppFactory.Salida, "/Pesajes/Salida", "/Home/Playon")]
        public async Task CadaOperario_SoloEntraASuPantalla(string usuario, string permitida, string prohibida)
        {
            var cliente = await _app.ClienteLogueado(usuario);

            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(permitida)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/")).StatusCode); // el dashboard es para todos

            var r = await cliente.GetAsync(prohibida);
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.StartsWith("/Identity/Account/AccessDenied", AppFactory.Destino(r));
        }

        [Theory]
        [InlineData("/Reportes")]
        [InlineData("/Usuarios")]
        [InlineData("/Proveedores")]
        public async Task Operario_NoEntraAAdministracion(string url)
        {
            var cliente = await _app.ClienteLogueado(AppFactory.Playon);
            var r = await cliente.GetAsync(url);
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.StartsWith("/Identity/Account/AccessDenied", AppFactory.Destino(r));
        }

        [Fact]
        public async Task ApiPlayon_SinLogin_Devuelve401()
        {
            var r = await _app.NuevoCliente().GetAsync("/api/playon/camiones");
            Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        }

        [Fact]
        public async Task ApiPlayon_ConOtroRol_Devuelve403()
        {
            var cliente = await _app.ClienteLogueado(AppFactory.Entrada);
            var r = await cliente.GetAsync("/api/playon/camiones");
            Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        }

        [Fact]
        public async Task ApiPlayon_PostSinTokenAntiforgery_EsRechazado()
        {
            var cliente = await _app.ClienteLogueado(AppFactory.Playon);
            var r = await cliente.PostAsync("/api/playon/camiones/1/iniciar-descarga", JsonContent.Create(new { siloDestino = "Silo 1" }));
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        }
    }
}
