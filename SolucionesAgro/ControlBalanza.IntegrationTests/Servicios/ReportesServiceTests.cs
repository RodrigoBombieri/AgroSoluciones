using System.Text;
using ControlBalanza.IntegrationTests.Infraestructura;
using ControlBalanza.Models;
using ControlBalanza.Services;
using ControlBalanza.ViewModels;
using Microsoft.Extensions.Options;

namespace ControlBalanza.IntegrationTests.Servicios
{
    /// <summary>Base con tres pesajes completados hoy: uno fuera de tolerancia (-1 %) y dos dentro.</summary>
    public class ReportesFixture : BaseDeDatosFixture
    {
        public string PatenteConAlerta { get; private set; } = "";
        public string CtgConAlerta { get; private set; } = "";

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            await using var ctx = NuevoContexto();

            Pesaje Completado(double declarado, double bruto, double tara, int horasAtras)
            {
                var (cpe, ctg, patente) = DatosUnicos();
                return new Pesaje
                {
                    NumeroCPE = cpe, Ctg = ctg, PatenteCamion = patente, KilosDeclarados = declarado, PesoBruto = bruto, PesoTara = tara,
                    Estado = EstadoPesaje.Completado, FechaEntrada = Hoy.AddHours(-horasAtras), FechaSalida = Hoy.AddHours(-horasAtras + 1),
                    ProveedorId = ProveedorId, ProductoId = ProductoSojaId,
                    UsuarioBalanzaEntradaId = OperarioEntrada, UsuarioBalanzaSalidaId = OperarioSalida
                };
            }

            var conAlerta = Completado(30000, 45000, 15300, 5);   // neto 29.700: -1 %
            PatenteConAlerta = conAlerta.PatenteCamion;
            CtgConAlerta = conAlerta.Ctg!;
            ctx.Pesajes.AddRange(
                conAlerta,
                Completado(30000, 45000, 15000, 4),                // neto 30.000: 0 %
                Completado(28000, 43000, 15010, 3));               // neto 27.990: -0,04 %
            await ctx.SaveChangesAsync();
        }
    }

    /// <summary>Filtros del historial, totales, exportación CSV y dashboard.</summary>
    public class ReportesServiceTests : IClassFixture<ReportesFixture>
    {
        private readonly ReportesFixture _db;

        public ReportesServiceTests(ReportesFixture db) => _db = db;

        private ReportesService Servicio(Data.AgroDbContext ctx) =>
            new(ctx, Options.Create(new BalanzaOptions { ToleranciaDiferenciaPorcentaje = 0.5 }), new RelojFijo(_db.Hoy));

        [Fact]
        public async Task FiltraPorPatente_SinImportarMayusculas()
        {
            await using var ctx = _db.NuevoContexto();
            var r = await Servicio(ctx).BuscarAsync(new FiltroPesajes { Patente = _db.PatenteConAlerta.ToLower() });
            Assert.Equal(1, r.TotalRegistros);
            Assert.Equal(_db.PatenteConAlerta, r.Pesajes.Single().PatenteCamion);
        }

        [Fact]
        public async Task FiltraPorCtg_EnElCampoNumeroCpe()
        {
            await using var ctx = _db.NuevoContexto();
            var r = await Servicio(ctx).BuscarAsync(new FiltroPesajes { NumeroCPE = _db.CtgConAlerta });
            Assert.Equal(1, r.TotalRegistros);
        }

        [Fact]
        public async Task SoloConAlerta_DevuelveSoloLosFueraDeTolerancia()
        {
            await using var ctx = _db.NuevoContexto();
            var r = await Servicio(ctx).BuscarAsync(new FiltroPesajes { SoloConAlerta = true });
            var p = Assert.Single(r.Pesajes);
            Assert.Equal(_db.PatenteConAlerta, p.PatenteCamion);
        }

        [Fact]
        public async Task Totales_SumanNetoYDeclarado()
        {
            await using var ctx = _db.NuevoContexto();
            var r = await Servicio(ctx).BuscarAsync(new FiltroPesajes { Desde = _db.Hoy.Date, Hasta = _db.Hoy.Date });
            Assert.Equal(3, r.TotalRegistros);
            Assert.Equal(29700 + 30000 + 27990, r.TotalNetoKg);
            Assert.Equal(30000 + 30000 + 28000, r.TotalDeclaradoKg);
        }

        [Fact]
        public async Task FiltroDeFechas_FueraDeRango_NoDevuelveNada()
        {
            await using var ctx = _db.NuevoContexto();
            var r = await Servicio(ctx).BuscarAsync(new FiltroPesajes { Desde = _db.Hoy.Date.AddDays(1) });
            Assert.Equal(0, r.TotalRegistros);
        }

        [Fact]
        public async Task ExportarCsv_ConBomYSeparadorPuntoYComa()
        {
            await using var ctx = _db.NuevoContexto();
            var bytes = await Servicio(ctx).ExportarCsvAsync(new FiltroPesajes { Patente = _db.PatenteConAlerta });

            Assert.Equal(Encoding.UTF8.GetPreamble(), bytes.Take(3).ToArray());
            var lineas = Encoding.UTF8.GetString(bytes[3..]).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.StartsWith("Nro CPE;CTG;Fecha entrada;", lineas[0]);
            Assert.Equal(2, lineas.Length);
            Assert.Contains(_db.PatenteConAlerta, lineas[1]);
            Assert.Contains(";29700;", lineas[1]); // neto
        }

        [Fact]
        public async Task Dashboard_CompletadosNetoYAlertasDelDia()
        {
            await using var ctx = _db.NuevoContexto();
            var d = await Servicio(ctx).ObtenerDashboardAsync();

            Assert.Equal(3, d.CompletadosHoy);
            Assert.Equal(87690, d.NetoHoyKg);
            Assert.Equal(60, d.DemoraPromedioMinutos);
            var alerta = Assert.Single(d.Alertas);
            Assert.Equal(_db.PatenteConAlerta, alerta.PatenteCamion);
        }
    }
}
