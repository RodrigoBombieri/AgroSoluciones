using ControlBalanza.Data;
using ControlBalanza.Models;
using ControlBalanza.Validacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ControlBalanza.Services
{
    /// <summary>Resultado de una operación del flujo: Ok o lista de errores para mostrar al operario.</summary>
    public record ResultadoOperacion(bool Ok, IReadOnlyList<string> Errores, Pesaje? Pesaje = null)
    {
        public static ResultadoOperacion Exito(Pesaje p) => new(true, Array.Empty<string>(), p);
        public static ResultadoOperacion Error(params string[] errores) => new(false, errores);
        public string MensajeError => string.Join(" ", Errores);
    }

    public interface IPesajeWorkflowService
    {
        Task<ResultadoOperacion> RegistrarEntradaAsync(Pesaje pesaje, string usuarioId);
        Task<ResultadoOperacion> IniciarDescargaAsync(int pesajeId, string usuarioId, string? siloDestino);
        Task<ResultadoOperacion> FinalizarDescargaAsync(int pesajeId, string usuarioId, string? observaciones);
        Task<ResultadoOperacion> RegistrarSalidaAsync(int pesajeId, double pesoTara, string usuarioId, string? observaciones);
        bool SuperaTolerancia(Pesaje pesaje);
    }

    /// <summary>
    /// Ciclo de vida del camión:
    /// Balanza Entrada (bruto) → EnEspera → EnDescarga (playón) → ListoSalida → Balanza Salida (tara) → Completado.
    /// Toda transición pasa por acá para que las reglas estén en un solo lugar.
    /// </summary>
    public class PesajeWorkflowService : IPesajeWorkflowService
    {
        private readonly AgroDbContext _context;
        private readonly BalanzaOptions _opciones;
        private readonly TimeProvider _reloj;

        public PesajeWorkflowService(AgroDbContext context, IOptions<BalanzaOptions> opciones, TimeProvider reloj)
        {
            _context = context;
            _opciones = opciones.Value;
            _reloj = reloj;
        }

        private DateTime Ahora => _reloj.GetLocalNow().DateTime;

        public async Task<ResultadoOperacion> RegistrarEntradaAsync(Pesaje pesaje, string usuarioId)
        {
            var errores = new List<string>();

            pesaje.NumeroCPE = pesaje.NumeroCPE.Trim();
            pesaje.Ctg = string.IsNullOrWhiteSpace(pesaje.Ctg) ? null : Ctg.Normalizar(pesaje.Ctg);
            pesaje.PatenteCamion = Patente.Normalizar(pesaje.PatenteCamion);
            pesaje.PatenteAcoplado = string.IsNullOrWhiteSpace(pesaje.PatenteAcoplado) ? null : Patente.Normalizar(pesaje.PatenteAcoplado);
            pesaje.NombreChofer = string.IsNullOrWhiteSpace(pesaje.NombreChofer) ? null : pesaje.NombreChofer.Trim();

            if (pesaje.PesoBruto is not > 0)
                errores.Add("Debe registrar el peso bruto de entrada.");
            else if (pesaje.PesoBruto > _opciones.PesoMaximoKg)
                errores.Add($"El peso bruto supera el máximo admitido ({_opciones.PesoMaximoKg:N0} kg). Verifique la lectura.");

            if (pesaje.KilosDeclarados <= 0)
                errores.Add("Debe indicar los kilos declarados en la CPE.");

            if (await _context.Pesajes.AnyAsync(p => p.NumeroCPE == pesaje.NumeroCPE))
                errores.Add($"La CPE {pesaje.NumeroCPE} ya fue ingresada.");
            if (pesaje.Ctg != null && await _context.Pesajes.AnyAsync(p => p.Ctg == pesaje.Ctg))
                errores.Add($"El CTG {pesaje.Ctg} ya fue ingresado.");

            // Un camión no puede estar dos veces en planta
            if (await _context.Pesajes.AnyAsync(p => p.PatenteCamion == pesaje.PatenteCamion && p.Estado != EstadoPesaje.Completado))
                errores.Add($"El camión {pesaje.PatenteCamion} ya está en planta (no registró salida).");

            if (!await _context.Proveedores.AnyAsync(p => p.Id == pesaje.ProveedorId))
                errores.Add("El proveedor seleccionado no existe.");
            if (!await _context.Productos.AnyAsync(p => p.Id == pesaje.ProductoId))
                errores.Add("El producto seleccionado no existe.");

            if (errores.Count > 0) return new ResultadoOperacion(false, errores);

            pesaje.Id = 0;
            pesaje.Estado = EstadoPesaje.EnEspera;
            pesaje.FechaEntrada = Ahora;
            pesaje.FechaSalida = null;
            pesaje.PesoTara = null;
            pesaje.UsuarioBalanzaEntradaId = usuarioId;
            pesaje.UsuarioPlayonId = null;
            pesaje.UsuarioBalanzaSalidaId = null;

            _context.Pesajes.Add(pesaje);
            await _context.SaveChangesAsync();
            return ResultadoOperacion.Exito(pesaje);
        }

        public async Task<ResultadoOperacion> IniciarDescargaAsync(int pesajeId, string usuarioId, string? siloDestino)
        {
            var pesaje = await _context.Pesajes.FindAsync(pesajeId);
            if (pesaje == null) return ResultadoOperacion.Error("El pesaje no existe.");
            if (pesaje.Estado != EstadoPesaje.EnEspera)
                return ResultadoOperacion.Error($"Solo se puede iniciar la descarga de un camión en espera (estado actual: {pesaje.Estado}).");

            pesaje.Estado = EstadoPesaje.EnDescarga;
            pesaje.UsuarioPlayonId = usuarioId;
            if (!string.IsNullOrWhiteSpace(siloDestino)) pesaje.SiloDestino = siloDestino.Trim();

            await _context.SaveChangesAsync();
            return ResultadoOperacion.Exito(pesaje);
        }

        public async Task<ResultadoOperacion> FinalizarDescargaAsync(int pesajeId, string usuarioId, string? observaciones)
        {
            var pesaje = await _context.Pesajes.FindAsync(pesajeId);
            if (pesaje == null) return ResultadoOperacion.Error("El pesaje no existe.");
            if (pesaje.Estado != EstadoPesaje.EnDescarga)
                return ResultadoOperacion.Error("Solo se puede finalizar la descarga de un camión que está descargando.");

            pesaje.Estado = EstadoPesaje.ListoSalida;
            pesaje.UsuarioPlayonId = usuarioId;
            AgregarObservacion(pesaje, observaciones);

            await _context.SaveChangesAsync();
            return ResultadoOperacion.Exito(pesaje);
        }

        public async Task<ResultadoOperacion> RegistrarSalidaAsync(int pesajeId, double pesoTara, string usuarioId, string? observaciones)
        {
            var pesaje = await _context.Pesajes.FindAsync(pesajeId);
            if (pesaje == null) return ResultadoOperacion.Error("El pesaje no existe.");
            if (pesaje.Estado != EstadoPesaje.ListoSalida)
                return ResultadoOperacion.Error("Solo se puede registrar la salida de un camión listo para salir.");

            if (pesoTara <= 0)
                return ResultadoOperacion.Error("La tara debe ser mayor a cero.");
            if (pesaje.PesoBruto.HasValue && pesoTara >= pesaje.PesoBruto.Value)
                return ResultadoOperacion.Error($"La tara ({pesoTara:N0} kg) debe ser menor que el peso bruto ({pesaje.PesoBruto:N0} kg).");

            pesaje.PesoTara = pesoTara;
            pesaje.FechaSalida = Ahora;
            pesaje.Estado = EstadoPesaje.Completado;
            pesaje.UsuarioBalanzaSalidaId = usuarioId;
            AgregarObservacion(pesaje, observaciones);

            await _context.SaveChangesAsync();

            // PesoNeto es una columna calculada por SQL Server: se recarga para tenerlo actualizado
            await _context.Entry(pesaje).ReloadAsync();
            return ResultadoOperacion.Exito(pesaje);
        }

        public bool SuperaTolerancia(Pesaje pesaje) =>
            pesaje.DiferenciaPorcentaje.HasValue &&
            Math.Abs(pesaje.DiferenciaPorcentaje.Value) > _opciones.ToleranciaDiferenciaPorcentaje;

        private static void AgregarObservacion(Pesaje pesaje, string? observacion)
        {
            if (string.IsNullOrWhiteSpace(observacion)) return;
            var texto = string.IsNullOrWhiteSpace(pesaje.Observaciones)
                ? observacion.Trim()
                : $"{pesaje.Observaciones} | {observacion.Trim()}";
            pesaje.Observaciones = texto.Length > 500 ? texto[..500] : texto;
        }
    }
}
