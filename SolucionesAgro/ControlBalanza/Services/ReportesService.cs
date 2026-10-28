using System.Globalization;
using System.Text;
using ControlBalanza.Data;
using ControlBalanza.Models;
using ControlBalanza.Validacion;
using ControlBalanza.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ControlBalanza.Services
{
    /// <summary>Consultas de solo lectura: dashboard, historial filtrado y exportación.</summary>
    public class ReportesService
    {
        public const int TamanioPagina = 50;

        private readonly AgroDbContext _context;
        private readonly BalanzaOptions _opciones;
        private readonly TimeProvider _reloj;

        public ReportesService(AgroDbContext context, IOptions<BalanzaOptions> opciones, TimeProvider reloj)
        {
            _context = context;
            _opciones = opciones.Value;
            _reloj = reloj;
        }

        private DateTime Hoy => _reloj.GetLocalNow().Date;

        public async Task<DashboardViewModel> ObtenerDashboardAsync()
        {
            var hoy = Hoy;
            var manana = hoy.AddDays(1);

            var porEstado = await _context.Pesajes
                .Where(p => p.Estado != EstadoPesaje.Completado)
                .GroupBy(p => p.Estado)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            var completadosHoy = await _context.Pesajes.AsNoTracking()
                .Include(p => p.Producto)
                .Where(p => p.Estado == EstadoPesaje.Completado && p.FechaSalida >= hoy && p.FechaSalida < manana)
                .ToListAsync();

            var alertas = await _context.Pesajes.AsNoTracking()
                .Include(p => p.Proveedor)
                .Where(p => p.Estado == EstadoPesaje.Completado && p.FechaSalida >= hoy.AddDays(-7))
                .OrderByDescending(p => p.FechaSalida)
                .ToListAsync();

            return new DashboardViewModel
            {
                EnEspera = porEstado.FirstOrDefault(x => x.Estado == EstadoPesaje.EnEspera)?.Cantidad ?? 0,
                EnDescarga = porEstado.FirstOrDefault(x => x.Estado == EstadoPesaje.EnDescarga)?.Cantidad ?? 0,
                ListosSalida = porEstado.FirstOrDefault(x => x.Estado == EstadoPesaje.ListoSalida)?.Cantidad ?? 0,
                IngresosHoy = await _context.Pesajes.CountAsync(p => p.FechaEntrada >= hoy && p.FechaEntrada < manana),
                CompletadosHoy = completadosHoy.Count,
                NetoHoyKg = completadosHoy.Sum(p => p.PesoNeto ?? 0),
                DemoraPromedioMinutos = completadosHoy.Count > 0
                    ? completadosHoy.Average(p => (p.FechaSalida!.Value - p.FechaEntrada).TotalMinutes)
                    : null,
                Tolerancia = _opciones.ToleranciaDiferenciaPorcentaje,
                PorProductoHoy = completadosHoy
                    .GroupBy(p => p.Producto?.Nombre ?? "-")
                    .Select(g => new TotalPorProducto { Producto = g.Key, Camiones = g.Count(), NetoKg = g.Sum(p => p.PesoNeto ?? 0) })
                    .OrderByDescending(t => t.NetoKg)
                    .ToList(),
                UltimosMovimientos = await _context.Pesajes.AsNoTracking()
                    .Include(p => p.Proveedor).Include(p => p.Producto)
                    .OrderByDescending(p => p.FechaSalida ?? p.FechaEntrada)
                    .Take(10)
                    .ToListAsync(),
                Alertas = alertas.Where(FueraDeTolerancia).Take(10).ToList()
            };
        }

        public async Task<ReporteViewModel> BuscarAsync(FiltroPesajes filtro)
        {
            var consulta = AplicarFiltro(filtro);

            // El filtro por tolerancia se calcula en memoria (depende de un % configurable)
            if (filtro.SoloConAlerta)
            {
                var todos = await consulta.OrderByDescending(p => p.FechaEntrada).ToListAsync();
                var conAlerta = todos.Where(FueraDeTolerancia).ToList();
                return ArmarReporte(filtro, conAlerta.Count, conAlerta.Skip((PaginaValida(filtro, conAlerta.Count) - 1) * TamanioPagina).Take(TamanioPagina).ToList(),
                    conAlerta.Sum(p => p.PesoNeto ?? 0), conAlerta.Where(p => p.PesoNeto != null).Sum(p => p.KilosDeclarados));
            }

            var total = await consulta.CountAsync();
            var pagina = PaginaValida(filtro, total);
            var items = await consulta
                .OrderByDescending(p => p.FechaEntrada)
                .Skip((pagina - 1) * TamanioPagina)
                .Take(TamanioPagina)
                .ToListAsync();
            var totalNeto = await consulta.SumAsync(p => p.PesoNeto ?? 0);
            var totalDeclarado = await consulta.Where(p => p.PesoNeto != null).SumAsync(p => p.KilosDeclarados);

            return ArmarReporte(filtro, total, items, totalNeto, totalDeclarado);
        }

        /// <summary>CSV separado por ';' con BOM UTF-8 (abre bien en Excel en español).</summary>
        public async Task<byte[]> ExportarCsvAsync(FiltroPesajes filtro)
        {
            var datos = await AplicarFiltro(filtro).OrderByDescending(p => p.FechaEntrada).ToListAsync();
            if (filtro.SoloConAlerta) datos = datos.Where(FueraDeTolerancia).ToList();

            var cultura = CultureInfo.GetCultureInfo("es-AR");
            var sb = new StringBuilder();
            sb.AppendLine("Nro CPE;Fecha entrada;Fecha salida;Estado;Proveedor;CUIT;Producto;Patente camion;Patente acoplado;Chofer;Kg declarados;Peso bruto;Peso tara;Peso neto;Diferencia kg;Diferencia %;Silo;Usuario entrada;Usuario playon;Usuario salida;Observaciones");
            foreach (var p in datos)
            {
                sb.AppendJoin(';', new[]
                {
                    Csv(p.NumeroCPE),
                    p.FechaEntrada.ToString("dd/MM/yyyy HH:mm", cultura),
                    p.FechaSalida?.ToString("dd/MM/yyyy HH:mm", cultura) ?? "",
                    p.Estado.ToString(),
                    Csv(p.Proveedor?.RazonSocial),
                    Csv(p.Proveedor?.CUIT),
                    Csv(p.Producto?.Nombre),
                    Csv(p.PatenteCamion),
                    Csv(p.PatenteAcoplado),
                    Csv(p.NombreChofer),
                    p.KilosDeclarados.ToString("0.##", cultura),
                    p.PesoBruto?.ToString("0.##", cultura) ?? "",
                    p.PesoTara?.ToString("0.##", cultura) ?? "",
                    p.PesoNeto?.ToString("0.##", cultura) ?? "",
                    p.DiferenciaKg?.ToString("0.##", cultura) ?? "",
                    p.DiferenciaPorcentaje?.ToString("0.##", cultura) ?? "",
                    Csv(p.SiloDestino),
                    Csv(p.UsuarioBalanzaEntrada?.Email),
                    Csv(p.UsuarioPlayon?.Email),
                    Csv(p.UsuarioBalanzaSalida?.Email),
                    Csv(p.Observaciones)
                });
                sb.AppendLine();
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        public bool FueraDeTolerancia(Pesaje p) =>
            p.DiferenciaPorcentaje.HasValue && Math.Abs(p.DiferenciaPorcentaje.Value) > _opciones.ToleranciaDiferenciaPorcentaje;

        private IQueryable<Pesaje> AplicarFiltro(FiltroPesajes f)
        {
            var q = _context.Pesajes.AsNoTracking()
                .Include(p => p.Proveedor)
                .Include(p => p.Producto)
                .Include(p => p.UsuarioBalanzaEntrada)
                .Include(p => p.UsuarioPlayon)
                .Include(p => p.UsuarioBalanzaSalida)
                .AsQueryable();

            if (f.Desde.HasValue) q = q.Where(p => p.FechaEntrada >= f.Desde.Value.Date);
            if (f.Hasta.HasValue) q = q.Where(p => p.FechaEntrada < f.Hasta.Value.Date.AddDays(1));
            if (f.ProveedorId.HasValue) q = q.Where(p => p.ProveedorId == f.ProveedorId);
            if (f.ProductoId.HasValue) q = q.Where(p => p.ProductoId == f.ProductoId);
            if (f.Estado.HasValue) q = q.Where(p => p.Estado == f.Estado);
            if (!string.IsNullOrWhiteSpace(f.NumeroCPE)) q = q.Where(p => p.NumeroCPE.Contains(f.NumeroCPE.Trim()));
            if (!string.IsNullOrWhiteSpace(f.Patente))
            {
                var patente = Patente.Normalizar(f.Patente);
                q = q.Where(p => p.PatenteCamion.Contains(patente) || (p.PatenteAcoplado != null && p.PatenteAcoplado.Contains(patente)));
            }
            return q;
        }

        private ReporteViewModel ArmarReporte(FiltroPesajes filtro, int total, List<Pesaje> items, double neto, double declarado) => new()
        {
            Filtro = filtro,
            Pesajes = items,
            TotalRegistros = total,
            Pagina = PaginaValida(filtro, total),
            TotalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanioPagina)),
            TotalNetoKg = neto,
            TotalDeclaradoKg = declarado,
            Tolerancia = _opciones.ToleranciaDiferenciaPorcentaje
        };

        private static int PaginaValida(FiltroPesajes f, int total)
        {
            var paginas = Math.Max(1, (int)Math.Ceiling(total / (double)TamanioPagina));
            return Math.Clamp(f.Pagina, 1, paginas);
        }

        private static string Csv(string? valor)
        {
            if (string.IsNullOrEmpty(valor)) return "";
            // Evita inyección de fórmulas en Excel y escapa separadores/comillas
            if ("=+-@".Contains(valor[0])) valor = "'" + valor;
            return valor.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0 ? $"\"{valor.Replace("\"", "\"\"")}\"" : valor;
        }
    }
}
