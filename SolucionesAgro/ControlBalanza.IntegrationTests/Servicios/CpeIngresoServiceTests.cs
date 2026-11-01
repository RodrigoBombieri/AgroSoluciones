using ControlBalanza.IntegrationTests.Infraestructura;
using ControlBalanza.Models;
using ControlBalanza.Services;
using ControlBalanza.Services.Cpe;
using Microsoft.Extensions.Options;

namespace ControlBalanza.IntegrationTests.Servicios
{
    /// <summary>Cruce de los datos leídos de la CPE con la base: proveedor, producto y controles de la CPE.</summary>
    public class CpeIngresoServiceTests : IClassFixture<BaseDeDatosFixture>
    {
        private static readonly DateTime Ahora = new(2026, 10, 31, 12, 0, 0);
        private readonly BaseDeDatosFixture _db;

        public CpeIngresoServiceTests(BaseDeDatosFixture db) => _db = db;

        private CpeIngresoService Servicio(Data.AgroDbContext ctx, int? plantaRuca = null) =>
            new(ctx, Options.Create(new BalanzaOptions { NumeroPlantaRuca = plantaRuca }), new RelojFijo(Ahora));

        [Theory]
        [InlineData("30-70912345-5")]
        [InlineData("30709123455")] // sin guiones también lo encuentra
        public async Task BuscaElProveedorPorCuit(string cuit)
        {
            await using var ctx = _db.NuevoContexto();
            var d = await Servicio(ctx).CompletarAsync(new CpeDatos { CuitProveedor = cuit });
            Assert.Equal(_db.ProveedorId, d.ProveedorId);
            Assert.Empty(d.Advertencias);
        }

        [Fact]
        public async Task ProveedorInexistente_Avisa()
        {
            await using var ctx = _db.NuevoContexto();
            var d = await Servicio(ctx).CompletarAsync(new CpeDatos { CuitProveedor = "20-12345678-6", RazonSocialProveedor = "Otro S.A." });
            Assert.Null(d.ProveedorId);
            Assert.Contains(d.Advertencias, a => a.Contains("No hay un proveedor con CUIT 20-12345678-6 (Otro S.A.)"));
        }

        [Theory]
        [InlineData("Soja")]
        [InlineData("SOJA")]
        [InlineData("Soja RR")]
        public async Task BuscaElProductoPorNombre(string grano)
        {
            await using var ctx = _db.NuevoContexto();
            var d = await Servicio(ctx).CompletarAsync(new CpeDatos { Producto = grano });
            Assert.Equal(_db.ProductoSojaId, d.ProductoId);
        }

        [Fact]
        public async Task BuscaElProductoSinImportarAcentos()
        {
            await using var ctx = _db.NuevoContexto();
            var d = await Servicio(ctx).CompletarAsync(new CpeDatos { Producto = "MAIZ" });
            Assert.Equal(_db.ProductoMaizId, d.ProductoId);
        }

        [Fact]
        public async Task CtgDelQrDistintoAlImpreso_AvisaYUsaElDelQr()
        {
            await using var ctx = _db.NuevoContexto();
            var d = await Servicio(ctx).CompletarAsync(new CpeDatos { Ctg = "10100014235" }, ctgQr: "10200001245");
            Assert.Equal("10200001245", d.Ctg);
            Assert.Contains(d.Advertencias, a => a.Contains("NO coincide"));
        }

        [Fact]
        public async Task SinCtgImpreso_TomaElDelQr()
        {
            await using var ctx = _db.NuevoContexto();
            var d = await Servicio(ctx).CompletarAsync(new CpeDatos(), ctgQr: "10200001245");
            Assert.Equal("10200001245", d.Ctg);
            Assert.Empty(d.Advertencias);
        }

        [Fact]
        public async Task CpeVencida_Avisa()
        {
            await using var ctx = _db.NuevoContexto();
            var vencida = await Servicio(ctx).CompletarAsync(new CpeDatos { Vencimiento = Ahora.AddMinutes(-1) });
            var vigente = await Servicio(ctx).CompletarAsync(new CpeDatos { Vencimiento = Ahora.AddDays(2) });

            Assert.Contains(vencida.Advertencias, a => a.Contains("VENCIDA"));
            Assert.DoesNotContain(vigente.Advertencias, a => a.Contains("VENCIDA"));
        }

        [Fact]
        public async Task DestinoAOtraPlanta_AvisaSoloSiEstaConfigurada()
        {
            await using var ctx = _db.NuevoContexto();
            var otraPlanta = await Servicio(ctx, plantaRuca: 1782).CompletarAsync(new CpeDatos { PlantaDestino = 9999 });
            var mismaPlanta = await Servicio(ctx, plantaRuca: 1782).CompletarAsync(new CpeDatos { PlantaDestino = 1782 });
            var sinConfigurar = await Servicio(ctx).CompletarAsync(new CpeDatos { PlantaDestino = 9999 });

            Assert.Contains(otraPlanta.Advertencias, a => a.Contains("planta 9999"));
            Assert.Empty(mismaPlanta.Advertencias);
            Assert.Empty(sinConfigurar.Advertencias);
        }

        [Fact]
        public async Task CtgYaIngresado_Avisa()
        {
            var (cpe, ctg, patente) = _db.DatosUnicos();
            await using (var ctx = _db.NuevoContexto())
            {
                ctx.Pesajes.Add(new Pesaje
                {
                    NumeroCPE = cpe, Ctg = ctg, PatenteCamion = patente, KilosDeclarados = 30000, PesoBruto = 45000,
                    FechaEntrada = Ahora.AddHours(-3), ProveedorId = _db.ProveedorId, ProductoId = _db.ProductoSojaId,
                    UsuarioBalanzaEntradaId = BaseDeDatosFixture.OperarioEntrada
                });
                await ctx.SaveChangesAsync();
            }

            await using var ctx2 = _db.NuevoContexto();
            var d = await Servicio(ctx2).CompletarAsync(new CpeDatos(), ctgQr: ctg);

            Assert.Contains(d.Advertencias, a => a.Contains($"CTG {ctg}) ya fue ingresada"));
        }
    }
}
