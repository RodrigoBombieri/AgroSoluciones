using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ControlBalanza.IntegrationTests.Infraestructura;
using ControlBalanza.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.IntegrationTests.Http
{
    /// <summary>
    /// Un camión recorre todo el circuito por HTTP, cada etapa con el operario de su rol:
    /// balanza de entrada (formulario) → playón (API de Angular) → balanza de salida (formulario) → ticket.
    /// </summary>
    public class CircuitoCompletoTests : IClassFixture<AppFactory>
    {
        private readonly AppFactory _app;

        public CircuitoCompletoTests(AppFactory app) => _app = app;

        [Fact]
        public async Task Camion_RecorreEntradaPlayonYSalida()
        {
            int proveedorId, productoId;
            await using (var ctx = SqlServerDePrueba.CrearContexto(_app.Conexion))
            {
                proveedorId = (await ctx.Proveedores.FirstAsync()).Id;
                productoId = (await ctx.Productos.FirstAsync(p => p.Nombre == "Soja")).Id;
            }

            // 1. Balanza de entrada
            var entrada = await _app.ClienteLogueado(AppFactory.Entrada);
            var token = await AppFactory.TokenAntiforgery(entrada, "/Pesajes/Create");
            var alta = await entrada.PostAsync("/Pesajes/Create", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["NumeroCPE"] = "00004-00012871",
                ["Ctg"] = "10100018841",
                ["PatenteCamion"] = "af482kd",
                ["PatenteAcoplado"] = "AF113PL",
                ["NombreChofer"] = "Ramón Acosta",
                ["KilosDeclarados"] = "29850",
                ["PesoBruto"] = "44960",
                ["ProveedorId"] = proveedorId.ToString(),
                ["ProductoId"] = productoId.ToString(),
                ["__RequestVerificationToken"] = token
            }));
            Assert.Equal(HttpStatusCode.Redirect, alta.StatusCode);
            Assert.Equal("/Pesajes/Create", AppFactory.Destino(alta));

            int id;
            await using (var ctx = SqlServerDePrueba.CrearContexto(_app.Conexion))
            {
                var p = await ctx.Pesajes.SingleAsync(x => x.Ctg == "10100018841");
                id = p.Id;
                Assert.Equal(EstadoPesaje.EnEspera, p.Estado);
                Assert.Equal("AF482KD", p.PatenteCamion); // normalizada
            }

            // 2. Playón (API usada por Angular, con el token antiforgery en el header)
            var playon = await _app.ClienteLogueado(AppFactory.Playon);
            var xsrf = await AppFactory.TokenApiPlayon(playon);

            var camiones = await playon.GetFromJsonAsync<JsonElement>("/api/playon/camiones");
            Assert.Contains(camiones.EnumerateArray(), c => c.GetProperty("id").GetInt32() == id);

            var iniciar = new HttpRequestMessage(HttpMethod.Post, $"/api/playon/camiones/{id}/iniciar-descarga")
            {
                Content = JsonContent.Create(new { siloDestino = "Silo 2" })
            };
            iniciar.Headers.Add("X-XSRF-TOKEN", xsrf);
            var r1 = await playon.SendAsync(iniciar);
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            Assert.Equal("EnDescarga", (await r1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("estado").GetString());

            // Iniciar dos veces: 409 (otro operario ya lo movió)
            var repetido = new HttpRequestMessage(HttpMethod.Post, $"/api/playon/camiones/{id}/iniciar-descarga") { Content = JsonContent.Create(new { }) };
            repetido.Headers.Add("X-XSRF-TOKEN", xsrf);
            Assert.Equal(HttpStatusCode.Conflict, (await playon.SendAsync(repetido)).StatusCode);

            var finalizar = new HttpRequestMessage(HttpMethod.Post, $"/api/playon/camiones/{id}/finalizar-descarga")
            {
                Content = JsonContent.Create(new { observaciones = "Humedad 13,5%" })
            };
            finalizar.Headers.Add("X-XSRF-TOKEN", xsrf);
            Assert.Equal(HttpStatusCode.OK, (await playon.SendAsync(finalizar)).StatusCode);

            // 3. Balanza de salida
            var salida = await _app.ClienteLogueado(AppFactory.Salida);
            Assert.Contains("AF482KD", await AppFactory.Html(salida, "/Pesajes/Salida"));
            var tokenSalida = await AppFactory.TokenAntiforgery(salida, $"/Pesajes/RegistrarSalida/{id}");
            var registro = await salida.PostAsync($"/Pesajes/RegistrarSalida/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["PesoTara"] = "15240",
                ["__RequestVerificationToken"] = tokenSalida
            }));
            Assert.Equal(HttpStatusCode.Redirect, registro.StatusCode);
            Assert.Equal($"/Pesajes/Ticket/{id}", AppFactory.Destino(registro));

            // 4. Ticket con el neto calculado por SQL Server (formato argentino)
            var ticket = await AppFactory.Html(salida, $"/Pesajes/Ticket/{id}");
            Assert.Contains("29.720 kg", ticket); // 44.960 - 15.240

            await using (var ctx = SqlServerDePrueba.CrearContexto(_app.Conexion))
            {
                var p = await ctx.Pesajes.Include(x => x.UsuarioBalanzaEntrada).Include(x => x.UsuarioPlayon).Include(x => x.UsuarioBalanzaSalida)
                    .SingleAsync(x => x.Id == id);
                Assert.Equal(EstadoPesaje.Completado, p.Estado);
                Assert.Equal(29720, p.PesoNeto);
                Assert.Equal("Silo 2", p.SiloDestino);
                Assert.Equal(AppFactory.Entrada, p.UsuarioBalanzaEntrada!.Email);
                Assert.Equal(AppFactory.Playon, p.UsuarioPlayon!.Email);
                Assert.Equal(AppFactory.Salida, p.UsuarioBalanzaSalida!.Email);
            }
        }

        [Fact]
        public async Task Entrada_ConDatosInvalidos_MuestraLosErrores()
        {
            var entrada = await _app.ClienteLogueado(AppFactory.Entrada);
            var token = await AppFactory.TokenAntiforgery(entrada, "/Pesajes/Create");
            var r = await entrada.PostAsync("/Pesajes/Create", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["NumeroCPE"] = "00004-00099999",
                ["Ctg"] = "123",
                ["PatenteCamion"] = "XX1",
                ["KilosDeclarados"] = "30000",
                ["PesoBruto"] = "45000",
                ["ProveedorId"] = "1",
                ["ProductoId"] = "1",
                ["__RequestVerificationToken"] = token
            }));

            Assert.Equal(HttpStatusCode.OK, r.StatusCode); // vuelve al formulario
            var html = await AppFactory.Html(r);
            Assert.Contains("El CTG no es válido", html);
            Assert.Contains("La patente no es válida", html);
        }

        [Fact]
        public async Task InterpretarTextoCpe_DevuelveLosDatosYBuscaElProveedor()
        {
            var entrada = await _app.ClienteLogueado(AppFactory.Entrada);
            var token = await AppFactory.TokenAntiforgery(entrada, "/Pesajes/Create");
            var texto =
                "Carta de Porte Electrónica Automotor Fecha: 31/10/2026\n" +
                "N° CPE: 00002-00007310\n" +
                "CTG: 10200009377 Vencimiento:\n" +
                "A - INTERVINIENTES\n" +
                "Titular Carta de Porte: 20123456786 - PRODUCTORA UNO\n" +
                "Chofer : 20288776650 - RAMON ACOSTA\n" +
                "B - GRANO / ESPECIE\n" +
                "Grano / especie: Soja Tipo: Soja Peso Bruto (kg): 44.760\n" +
                "Peso Tara (kg): 14.640\n" +
                "Peso Neto (kg): 30.120\n" +
                "E - DATOS DEL TRANSPORTE\n" +
                "Dominios: AB123CD - AC456DE\n";

            var request = new HttpRequestMessage(HttpMethod.Post, "/Pesajes/InterpretarTextoCpe")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["texto"] = texto, ["qr"] = "10200009377", ["origen"] = "PDF" })
            };
            request.Headers.Add("X-XSRF-TOKEN", token);
            var r = await entrada.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var json = await r.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("10200009377", json.GetProperty("ctg").GetString());
            Assert.Equal("00002-00007310", json.GetProperty("numeroCPE").GetString());
            Assert.Equal(30120, json.GetProperty("kilosDeclarados").GetDouble());
            Assert.Equal(JsonValueKind.Number, json.GetProperty("proveedorId").ValueKind); // "Productora Uno" del seed
            Assert.Equal(JsonValueKind.Number, json.GetProperty("productoId").ValueKind);
        }
    }
}
