using ControlBalanza.IntegrationTests.Infraestructura;
using ControlBalanza.Models;
using ControlBalanza.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ControlBalanza.IntegrationTests.Servicios
{
    /// <summary>Ciclo de vida del camión contra SQL Server real (incluye la columna calculada PesoNeto).</summary>
    public class PesajeWorkflowServiceTests : IClassFixture<BaseDeDatosFixture>
    {
        private static readonly DateTime Inicio = new(2026, 10, 31, 8, 0, 0);
        private readonly BaseDeDatosFixture _db;

        public PesajeWorkflowServiceTests(BaseDeDatosFixture db) => _db = db;

        private PesajeWorkflowService Servicio(Data.AgroDbContext ctx, RelojFijo? reloj = null, BalanzaOptions? opciones = null) =>
            new(ctx, Options.Create(opciones ?? new BalanzaOptions()), reloj ?? new RelojFijo(Inicio));

        private Pesaje NuevoPesaje(double bruto = 45000, double declarado = 30000)
        {
            var (cpe, ctg, patente) = _db.DatosUnicos();
            return new Pesaje
            {
                NumeroCPE = cpe, Ctg = ctg, PatenteCamion = patente, KilosDeclarados = declarado, PesoBruto = bruto,
                ProveedorId = _db.ProveedorId, ProductoId = _db.ProductoSojaId
            };
        }

        [Fact]
        public async Task CircuitoCompleto_DeEntradaASalida()
        {
            await using var ctx = _db.NuevoContexto();
            var reloj = new RelojFijo(Inicio);
            var servicio = Servicio(ctx, reloj);

            var entrada = await servicio.RegistrarEntradaAsync(NuevoPesaje(bruto: 45410, declarado: 30000), BaseDeDatosFixture.OperarioEntrada);
            Assert.True(entrada.Ok, entrada.MensajeError);
            var id = entrada.Pesaje!.Id;
            Assert.Equal(EstadoPesaje.EnEspera, entrada.Pesaje.Estado);
            Assert.Equal(Inicio, entrada.Pesaje.FechaEntrada);

            reloj.Avanzar(TimeSpan.FromMinutes(20));
            var descarga = await servicio.IniciarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, " Silo 2 ");
            Assert.True(descarga.Ok, descarga.MensajeError);
            Assert.Equal(EstadoPesaje.EnDescarga, descarga.Pesaje!.Estado);
            Assert.Equal("Silo 2", descarga.Pesaje.SiloDestino);

            var fin = await servicio.FinalizarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, "Humedad 13,5%");
            Assert.True(fin.Ok, fin.MensajeError);
            Assert.Equal(EstadoPesaje.ListoSalida, fin.Pesaje!.Estado);

            reloj.Avanzar(TimeSpan.FromMinutes(40));
            var salida = await servicio.RegistrarSalidaAsync(id, 15360, BaseDeDatosFixture.OperarioSalida, null);
            Assert.True(salida.Ok, salida.MensajeError);

            // Se relee de la base para verificar lo que quedó persistido
            await using var verificacion = _db.NuevoContexto();
            var p = await verificacion.Pesajes.SingleAsync(x => x.Id == id);
            Assert.Equal(EstadoPesaje.Completado, p.Estado);
            Assert.Equal(30050, p.PesoNeto);              // columna calculada por SQL Server: 45410 - 15360
            Assert.Equal(50, p.DiferenciaKg);
            Assert.Equal(Inicio.AddMinutes(60), p.FechaSalida);
            Assert.Equal(BaseDeDatosFixture.OperarioEntrada, p.UsuarioBalanzaEntradaId);
            Assert.Equal(BaseDeDatosFixture.OperarioPlayon, p.UsuarioPlayonId);
            Assert.Equal(BaseDeDatosFixture.OperarioSalida, p.UsuarioBalanzaSalidaId);
            Assert.Equal("Humedad 13,5%", p.Observaciones);
        }

        [Fact]
        public async Task Entrada_NormalizaPatentesYCtg()
        {
            await using var ctx = _db.NuevoContexto();
            var pesaje = NuevoPesaje();
            var patente = pesaje.PatenteCamion;
            pesaje.PatenteCamion = $" {patente[..3].ToLower()}-{patente[3..]} ";
            pesaje.PatenteAcoplado = "  ";
            pesaje.Ctg = "0" + pesaje.Ctg; // formato de 12 dígitos

            var r = await Servicio(ctx).RegistrarEntradaAsync(pesaje, BaseDeDatosFixture.OperarioEntrada);

            Assert.True(r.Ok, r.MensajeError);
            Assert.Equal(patente, r.Pesaje!.PatenteCamion);
            Assert.Null(r.Pesaje.PatenteAcoplado);
            Assert.Equal(11, r.Pesaje.Ctg!.Length);
        }

        [Fact]
        public async Task Entrada_SinPesoBruto_EsRechazada()
        {
            await using var ctx = _db.NuevoContexto();
            var pesaje = NuevoPesaje();
            pesaje.PesoBruto = null;

            var r = await Servicio(ctx).RegistrarEntradaAsync(pesaje, BaseDeDatosFixture.OperarioEntrada);

            Assert.False(r.Ok);
            Assert.Contains(r.Errores, e => e.Contains("peso bruto"));
        }

        [Fact]
        public async Task Entrada_PesoBrutoMayorAlMaximo_EsRechazada()
        {
            await using var ctx = _db.NuevoContexto();
            var r = await Servicio(ctx, opciones: new BalanzaOptions { PesoMaximoKg = 60000 })
                .RegistrarEntradaAsync(NuevoPesaje(bruto: 61000), BaseDeDatosFixture.OperarioEntrada);

            Assert.False(r.Ok);
            Assert.Contains(r.Errores, e => e.Contains("máximo"));
        }

        [Fact]
        public async Task Entrada_CpeYCtgDuplicados_SonRechazados()
        {
            await using var ctx = _db.NuevoContexto();
            var servicio = Servicio(ctx);
            var primero = NuevoPesaje();
            Assert.True((await servicio.RegistrarEntradaAsync(primero, BaseDeDatosFixture.OperarioEntrada)).Ok);

            var repetido = NuevoPesaje();
            repetido.NumeroCPE = primero.NumeroCPE;
            repetido.Ctg = primero.Ctg;
            var r = await servicio.RegistrarEntradaAsync(repetido, BaseDeDatosFixture.OperarioEntrada);

            Assert.False(r.Ok);
            Assert.Contains(r.Errores, e => e.Contains($"La CPE {primero.NumeroCPE} ya fue ingresada"));
            Assert.Contains(r.Errores, e => e.Contains($"El CTG {primero.Ctg} ya fue ingresado"));
        }

        [Fact]
        public async Task Entrada_CamionYaEnPlanta_EsRechazada_HastaQueRegistraSalida()
        {
            await using var ctx = _db.NuevoContexto();
            var servicio = Servicio(ctx);
            var primero = NuevoPesaje();
            var r1 = await servicio.RegistrarEntradaAsync(primero, BaseDeDatosFixture.OperarioEntrada);
            Assert.True(r1.Ok);

            var segundo = NuevoPesaje();
            segundo.PatenteCamion = primero.PatenteCamion;
            var r2 = await servicio.RegistrarEntradaAsync(segundo, BaseDeDatosFixture.OperarioEntrada);
            Assert.False(r2.Ok);
            Assert.Contains(r2.Errores, e => e.Contains("ya está en planta"));

            // Completa el circuito: ahora sí puede volver a ingresar
            var id = r1.Pesaje!.Id;
            await servicio.IniciarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, null);
            await servicio.FinalizarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, null);
            Assert.True((await servicio.RegistrarSalidaAsync(id, 15000, BaseDeDatosFixture.OperarioSalida, null)).Ok);

            var tercero = NuevoPesaje();
            tercero.PatenteCamion = primero.PatenteCamion;
            var r3 = await servicio.RegistrarEntradaAsync(tercero, BaseDeDatosFixture.OperarioEntrada);
            Assert.True(r3.Ok, r3.MensajeError);
        }

        [Fact]
        public async Task Entrada_ProveedorOProductoInexistente_EsRechazada()
        {
            await using var ctx = _db.NuevoContexto();
            var pesaje = NuevoPesaje();
            pesaje.ProveedorId = 999_999;
            pesaje.ProductoId = 999_999;

            var r = await Servicio(ctx).RegistrarEntradaAsync(pesaje, BaseDeDatosFixture.OperarioEntrada);

            Assert.False(r.Ok);
            Assert.Contains(r.Errores, e => e.Contains("proveedor"));
            Assert.Contains(r.Errores, e => e.Contains("producto"));
        }

        [Fact]
        public async Task NoSePuedenSaltearEtapas()
        {
            await using var ctx = _db.NuevoContexto();
            var servicio = Servicio(ctx);
            var id = (await servicio.RegistrarEntradaAsync(NuevoPesaje(), BaseDeDatosFixture.OperarioEntrada)).Pesaje!.Id;

            // En espera: no se puede finalizar descarga ni registrar salida
            Assert.False((await servicio.FinalizarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, null)).Ok);
            Assert.False((await servicio.RegistrarSalidaAsync(id, 15000, BaseDeDatosFixture.OperarioSalida, null)).Ok);

            // Dos operarios inician la descarga del mismo camión: el segundo recibe error
            Assert.True((await servicio.IniciarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, null)).Ok);
            var repetido = await servicio.IniciarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, null);
            Assert.False(repetido.Ok);
            Assert.Contains("en espera", repetido.MensajeError);

            // Descargando: todavía no puede salir
            Assert.False((await servicio.RegistrarSalidaAsync(id, 15000, BaseDeDatosFixture.OperarioSalida, null)).Ok);
        }

        [Theory]
        [InlineData(0, "mayor a cero")]
        [InlineData(-10, "mayor a cero")]
        [InlineData(45000, "menor que el peso bruto")]
        [InlineData(46000, "menor que el peso bruto")]
        public async Task Salida_TaraInvalida_EsRechazada(double tara, string mensaje)
        {
            await using var ctx = _db.NuevoContexto();
            var servicio = Servicio(ctx);
            var id = (await servicio.RegistrarEntradaAsync(NuevoPesaje(bruto: 45000), BaseDeDatosFixture.OperarioEntrada)).Pesaje!.Id;
            await servicio.IniciarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, null);
            await servicio.FinalizarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, null);

            var r = await servicio.RegistrarSalidaAsync(id, tara, BaseDeDatosFixture.OperarioSalida, null);

            Assert.False(r.Ok);
            Assert.Contains(mensaje, r.MensajeError);
        }

        [Fact]
        public async Task PesajeInexistente_DevuelveError()
        {
            await using var ctx = _db.NuevoContexto();
            var r = await Servicio(ctx).IniciarDescargaAsync(999_999, BaseDeDatosFixture.OperarioPlayon, null);
            Assert.False(r.Ok);
            Assert.Contains("no existe", r.MensajeError);
        }

        [Fact]
        public async Task Observaciones_SeAcumulanYNoSuperan500Caracteres()
        {
            await using var ctx = _db.NuevoContexto();
            var servicio = Servicio(ctx);
            var id = (await servicio.RegistrarEntradaAsync(NuevoPesaje(), BaseDeDatosFixture.OperarioEntrada)).Pesaje!.Id;
            await servicio.IniciarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, null);
            await servicio.FinalizarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, "Humedad alta");
            var r = await servicio.RegistrarSalidaAsync(id, 15000, BaseDeDatosFixture.OperarioSalida, new string('x', 600));

            Assert.True(r.Ok, r.MensajeError);
            Assert.StartsWith("Humedad alta | xxx", r.Pesaje!.Observaciones);
            Assert.Equal(500, r.Pesaje.Observaciones!.Length);
        }

        [Theory]
        [InlineData(30000, 29900, false)] // -0,33 %: dentro de la tolerancia de 0,5 %
        [InlineData(30000, 29800, true)]  // -0,67 %
        [InlineData(30000, 30200, true)]  // +0,67 %
        public async Task SuperaTolerancia_ComparaNetoContraDeclarado(double declarado, double neto, bool supera)
        {
            await using var ctx = _db.NuevoContexto();
            var servicio = Servicio(ctx);
            var id = (await servicio.RegistrarEntradaAsync(NuevoPesaje(bruto: 45000, declarado: declarado), BaseDeDatosFixture.OperarioEntrada)).Pesaje!.Id;
            await servicio.IniciarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, null);
            await servicio.FinalizarDescargaAsync(id, BaseDeDatosFixture.OperarioPlayon, null);
            var r = await servicio.RegistrarSalidaAsync(id, 45000 - neto, BaseDeDatosFixture.OperarioSalida, null);

            Assert.True(r.Ok, r.MensajeError);
            Assert.Equal(supera, servicio.SuperaTolerancia(r.Pesaje!));
        }
    }
}
