using ControlBalanza.Data;
using ControlBalanza.Seguridad;
using ControlBalanza.Services;
using ControlBalanza.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Controllers
{
    /// <summary>Historial de pesajes con filtros para auditoría, y exportación a CSV (Excel).</summary>
    [Authorize(Roles = Roles.Admin)]
    public class ReportesController : Controller
    {
        private readonly ReportesService _reportes;
        private readonly AgroDbContext _context;
        private readonly TimeProvider _reloj;

        public ReportesController(ReportesService reportes, AgroDbContext context, TimeProvider reloj)
        {
            _reportes = reportes;
            _context = context;
            _reloj = reloj;
        }

        // GET: Reportes?Desde=...&Hasta=...&ProveedorId=...
        public async Task<IActionResult> Index([FromQuery] FiltroPesajes filtro)
        {
            // Sin filtro de fechas se muestran los últimos 30 días
            if (!Request.Query.ContainsKey(nameof(FiltroPesajes.Desde)) && !Request.Query.ContainsKey(nameof(FiltroPesajes.Hasta)))
            {
                var hoy = _reloj.GetLocalNow().Date;
                filtro.Desde = hoy.AddDays(-30);
                filtro.Hasta = hoy;
            }

            await CargarListas(filtro);
            return View(await _reportes.BuscarAsync(filtro));
        }

        // GET: Reportes/ExportarCsv?...
        public async Task<IActionResult> ExportarCsv([FromQuery] FiltroPesajes filtro)
        {
            var bytes = await _reportes.ExportarCsvAsync(filtro);
            var nombre = $"pesajes_{_reloj.GetLocalNow():yyyyMMdd_HHmm}.csv";
            return File(bytes, "text/csv", nombre);
        }

        private async Task CargarListas(FiltroPesajes filtro)
        {
            ViewData["Proveedores"] = new SelectList(
                await _context.Proveedores.AsNoTracking().OrderBy(p => p.RazonSocial).ToListAsync(),
                "Id", "RazonSocial", filtro.ProveedorId);
            ViewData["Productos"] = new SelectList(
                await _context.Productos.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync(),
                "Id", "Nombre", filtro.ProductoId);
        }
    }
}
