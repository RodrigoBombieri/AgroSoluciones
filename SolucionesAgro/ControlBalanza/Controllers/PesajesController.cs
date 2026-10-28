using ControlBalanza.Data;
using ControlBalanza.Models;
using ControlBalanza.Seguridad;
using ControlBalanza.Services;
using ControlBalanza.Services.Cpe;
using ControlBalanza.Validacion;
using ControlBalanza.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ControlBalanza.Controllers
{
    /// <summary>
    /// Balanza de entrada (alta del pesaje con lectura de CPE), balanza de salida (tara) y consulta de camiones en planta.
    /// El playón opera desde el panel Angular (Api/PlayonController).
    /// </summary>
    [Authorize]
    public class PesajesController : Controller
    {
        private const long TamanioMaximoImagen = 10 * 1024 * 1024; // 10 MB

        private readonly AgroDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IPesajeWorkflowService _workflow;
        private readonly CpeIngresoService _cpeIngreso;
        private readonly ICpeExtractorIA _extractorIA;
        private readonly BalanzaOptions _opciones;

        public PesajesController(AgroDbContext context, UserManager<Usuario> userManager, IPesajeWorkflowService workflow,
            CpeIngresoService cpeIngreso, ICpeExtractorIA extractorIA, IOptions<BalanzaOptions> opciones)
        {
            _context = context;
            _userManager = userManager;
            _workflow = workflow;
            _cpeIngreso = cpeIngreso;
            _extractorIA = extractorIA;
            _opciones = opciones.Value;
        }

        // GET: Pesajes — camiones en planta (todo lo que no registró salida)
        public async Task<IActionResult> Index()
        {
            var pesajes = await _context.Pesajes
                .Include(p => p.Proveedor)
                .Include(p => p.Producto)
                .AsNoTracking()
                .Where(p => p.Estado != EstadoPesaje.Completado)
                .OrderBy(p => p.FechaEntrada)
                .ToListAsync();

            return View(pesajes);
        }

        // GET: Pesajes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pesaje = await ObtenerPesajeCompleto(id.Value);
            if (pesaje == null) return NotFound();

            ViewBag.FueraDeTolerancia = _workflow.SuperaTolerancia(pesaje);
            ViewBag.Tolerancia = _opciones.ToleranciaDiferenciaPorcentaje;
            return View(pesaje);
        }

        // ===================== BALANZA DE ENTRADA =====================

        // GET: Pesajes/Create
        [Authorize(Roles = Roles.AdminOEntrada)]
        public async Task<IActionResult> Create()
        {
            await CargarListas();
            ViewBag.IADisponible = _extractorIA.Disponible;
            return View(new Pesaje());
        }

        // POST: Pesajes/Create
        // Solo se bindean los datos que carga el operario; estado, fechas y usuarios los asigna el servicio.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminOEntrada)]
        public async Task<IActionResult> Create(
            [Bind("NumeroCPE,PatenteCamion,PatenteAcoplado,NombreChofer,KilosDeclarados,PesoBruto,ProveedorId,ProductoId")] Pesaje pesaje)
        {
            var usuarioId = _userManager.GetUserId(User);
            if (usuarioId == null) return Challenge();

            ModelState.Remove(nameof(Pesaje.UsuarioBalanzaEntradaId));

            if (ModelState.IsValid)
            {
                var resultado = await _workflow.RegistrarEntradaAsync(pesaje, usuarioId);
                if (resultado.Ok)
                {
                    TempData["Exito"] = $"Ingreso registrado: CPE {pesaje.NumeroCPE}, camión {pesaje.PatenteCamion}. Quedó en espera de descarga.";
                    return RedirectToAction(nameof(Create));
                }
                foreach (var error in resultado.Errores) ModelState.AddModelError(string.Empty, error);
            }

            await CargarListas(pesaje.ProveedorId, pesaje.ProductoId);
            ViewBag.IADisponible = _extractorIA.Disponible;
            return View(pesaje);
        }

        // POST: Pesajes/InterpretarQr — recibe el texto leído por la cámara y devuelve los campos para precompletar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminOEntrada)]
        public async Task<IActionResult> InterpretarQr([FromForm] string? contenido)
        {
            var datos = CpeQrParser.Interpretar(contenido);
            return Json(await _cpeIngreso.CompletarAsync(datos));
        }

        // POST: Pesajes/LeerCpeConIA — foto de la CPE → datos extraídos por IA
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminOEntrada)]
        [RequestSizeLimit(TamanioMaximoImagen + 64 * 1024)]
        public async Task<IActionResult> LeerCpeConIA(IFormFile? imagen, CancellationToken ct)
        {
            if (imagen == null || imagen.Length == 0)
                return BadRequest(new { error = "No se recibió ninguna imagen." });
            if (imagen.Length > TamanioMaximoImagen)
                return BadRequest(new { error = "La imagen supera los 10 MB." });
            if (imagen.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
                return BadRequest(new { error = "Formato no soportado. Use JPG, PNG o WEBP." });
            if (!_extractorIA.Disponible)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "La lectura con IA no está configurada." });

            await using var stream = imagen.OpenReadStream();
            var datos = await _extractorIA.ExtraerAsync(stream, imagen.ContentType, ct);
            return Json(await _cpeIngreso.CompletarAsync(datos));
        }

        // ===================== BALANZA DE SALIDA =====================

        // GET: Pesajes/Salida — camiones listos para pesar tara
        [Authorize(Roles = Roles.AdminOSalida)]
        public async Task<IActionResult> Salida()
        {
            var listos = await _context.Pesajes
                .Include(p => p.Proveedor)
                .Include(p => p.Producto)
                .AsNoTracking()
                .Where(p => p.Estado == EstadoPesaje.ListoSalida)
                .OrderBy(p => p.FechaEntrada)
                .ToListAsync();

            return View(listos);
        }

        // GET: Pesajes/RegistrarSalida/5
        [Authorize(Roles = Roles.AdminOSalida)]
        public async Task<IActionResult> RegistrarSalida(int id)
        {
            var pesaje = await ObtenerPesajeCompleto(id);
            if (pesaje == null) return NotFound();
            if (pesaje.Estado != EstadoPesaje.ListoSalida)
            {
                TempData["Error"] = "Ese camión no está listo para salir.";
                return RedirectToAction(nameof(Salida));
            }

            return View(new RegistrarSalidaViewModel { PesajeId = id, Pesaje = pesaje });
        }

        // POST: Pesajes/RegistrarSalida/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdminOSalida)]
        public async Task<IActionResult> RegistrarSalida(int id, [Bind("PesoTara,Observaciones")] RegistrarSalidaViewModel modelo)
        {
            var usuarioId = _userManager.GetUserId(User);
            if (usuarioId == null) return Challenge();

            if (ModelState.IsValid)
            {
                var resultado = await _workflow.RegistrarSalidaAsync(id, modelo.PesoTara!.Value, usuarioId, modelo.Observaciones);
                if (resultado.Ok)
                {
                    TempData["Exito"] = $"Salida registrada. Peso neto: {resultado.Pesaje!.PesoNeto:N0} kg.";
                    return RedirectToAction(nameof(Ticket), new { id });
                }
                foreach (var error in resultado.Errores) ModelState.AddModelError(string.Empty, error);
            }

            modelo.PesajeId = id;
            modelo.Pesaje = await ObtenerPesajeCompleto(id);
            if (modelo.Pesaje == null) return NotFound();
            return View(modelo);
        }

        // GET: Pesajes/Ticket/5 — comprobante imprimible del pesaje
        public async Task<IActionResult> Ticket(int id)
        {
            var pesaje = await ObtenerPesajeCompleto(id);
            if (pesaje == null) return NotFound();

            return View(new SalidaRegistradaViewModel
            {
                Pesaje = pesaje,
                FueraDeTolerancia = _workflow.SuperaTolerancia(pesaje),
                Tolerancia = _opciones.ToleranciaDiferenciaPorcentaje
            });
        }

        // ===================== CORRECCIONES (solo Admin) =====================

        // GET: Pesajes/Edit/5
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pesaje = await _context.Pesajes.FindAsync(id);
            if (pesaje == null) return NotFound();

            await CargarListas(pesaje.ProveedorId, pesaje.ProductoId);
            return View(pesaje);
        }

        // POST: Pesajes/Edit/5
        // Corrección administrativa: se carga la entidad y solo se actualizan los campos editables.
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (id == null) return NotFound();

            var pesaje = await _context.Pesajes.FindAsync(id);
            if (pesaje == null) return NotFound();

            var actualizado = await TryUpdateModelAsync(pesaje, "",
                p => p.NumeroCPE, p => p.PatenteCamion, p => p.PatenteAcoplado, p => p.NombreChofer,
                p => p.KilosDeclarados, p => p.PesoBruto, p => p.PesoTara, p => p.Estado,
                p => p.ProveedorId, p => p.ProductoId, p => p.SiloDestino, p => p.Observaciones);

            if (actualizado && pesaje.PesoTara.HasValue && pesaje.PesoBruto.HasValue && pesaje.PesoTara >= pesaje.PesoBruto)
            {
                ModelState.AddModelError(nameof(Pesaje.PesoTara), "La tara debe ser menor que el peso bruto.");
                actualizado = false;
            }

            if (actualizado && await _context.Pesajes.AnyAsync(p => p.NumeroCPE == pesaje.NumeroCPE && p.Id != pesaje.Id))
            {
                ModelState.AddModelError(nameof(Pesaje.NumeroCPE), "Ya existe otro pesaje con esa CPE.");
                actualizado = false;
            }

            if (actualizado)
            {
                pesaje.PatenteCamion = Patente.Normalizar(pesaje.PatenteCamion);
                pesaje.PatenteAcoplado = string.IsNullOrWhiteSpace(pesaje.PatenteAcoplado) ? null : Patente.Normalizar(pesaje.PatenteAcoplado);
                try
                {
                    await _context.SaveChangesAsync();
                    TempData["Exito"] = "Pesaje actualizado.";
                    return RedirectToAction(nameof(Details), new { id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PesajeExists(pesaje.Id)) return NotFound();
                    throw;
                }
            }

            await CargarListas(pesaje.ProveedorId, pesaje.ProductoId);
            return View(pesaje);
        }

        // GET: Pesajes/Delete/5
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var pesaje = await ObtenerPesajeCompleto(id.Value);
            if (pesaje == null) return NotFound();

            return View(pesaje);
        }

        // POST: Pesajes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pesaje = await _context.Pesajes.FindAsync(id);
            if (pesaje != null)
            {
                _context.Pesajes.Remove(pesaje);
                await _context.SaveChangesAsync();
                TempData["Exito"] = $"Pesaje {pesaje.NumeroCPE} eliminado.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ===================== Auxiliares =====================

        private Task<Pesaje?> ObtenerPesajeCompleto(int id) =>
            _context.Pesajes
                .Include(p => p.Proveedor)
                .Include(p => p.Producto)
                .Include(p => p.UsuarioBalanzaEntrada)
                .Include(p => p.UsuarioPlayon)
                .Include(p => p.UsuarioBalanzaSalida)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

        private async Task CargarListas(int? proveedorId = null, int? productoId = null)
        {
            var proveedores = await _context.Proveedores.AsNoTracking().OrderBy(p => p.RazonSocial).ToListAsync();
            var productos = await _context.Productos.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();

            ViewData["ProveedorId"] = new SelectList(
                proveedores.Select(p => new { p.Id, Nombre = $"{p.RazonSocial} ({p.CUIT})" }),
                "Id", "Nombre", proveedorId);
            ViewData["ProductoId"] = new SelectList(
                productos.Select(p => new { p.Id, Nombre = string.IsNullOrWhiteSpace(p.Variedad) ? p.Nombre : $"{p.Nombre} ({p.Variedad})" }),
                "Id", "Nombre", productoId);
        }

        private bool PesajeExists(int id) => _context.Pesajes.Any(e => e.Id == id);
    }
}
