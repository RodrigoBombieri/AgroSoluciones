using ControlBalanza.Data;
using ControlBalanza.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Controllers
{
    [Authorize] // Se necesita el usuario logueado para registrar quién hizo cada etapa
    public class PesajesController : Controller
    {
        private readonly AgroDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public PesajesController(AgroDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Pesajes
        public async Task<IActionResult> Index()
        {
            var pesajes = await _context.Pesajes
                .Include(p => p.Proveedor)
                .Include(p => p.Producto)
                .AsNoTracking()
                .OrderByDescending(p => p.FechaEntrada)
                .ToListAsync();

            return View(pesajes);
        }

        // GET: Pesajes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pesaje = await ObtenerPesajeCompleto(id.Value);
            if (pesaje == null) return NotFound();

            return View(pesaje);
        }

        // GET: Pesajes/Create
        public async Task<IActionResult> Create()
        {
            await CargarListas();
            return View(new Pesaje { FechaEntrada = DateTime.Now });
        }

        // POST: Pesajes/Create
        // Solo se bindean los datos que carga el operario de balanza de entrada.
        // Estado, fechas y usuarios se asignan en el servidor.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("NumeroCPE,PatenteCamion,PatenteAcoplado,NombreChofer,KilosDeclarados,PesoBruto,ProveedorId,ProductoId")] Pesaje pesaje)
        {
            var usuarioId = _userManager.GetUserId(User);
            if (usuarioId == null) return Challenge();

            pesaje.UsuarioBalanzaEntradaId = usuarioId;
            pesaje.FechaEntrada = DateTime.Now;
            pesaje.Estado = EstadoPesaje.EnEspera;
            ModelState.Remove(nameof(Pesaje.UsuarioBalanzaEntradaId));

            if (ModelState.IsValid)
            {
                _context.Add(pesaje);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await CargarListas(pesaje.ProveedorId, pesaje.ProductoId);
            return View(pesaje);
        }

        // GET: Pesajes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var pesaje = await _context.Pesajes.FindAsync(id);
            if (pesaje == null) return NotFound();

            await CargarListas(pesaje.ProveedorId, pesaje.ProductoId);
            return View(pesaje);
        }

        // POST: Pesajes/Edit/5
        // Se carga la entidad de la BD y solo se actualizan los campos editables,
        // así no se pisan usuarios/fechas ni se acepta overposting.
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (id == null) return NotFound();

            var pesaje = await _context.Pesajes.FindAsync(id);
            if (pesaje == null) return NotFound();

            var actualizado = await TryUpdateModelAsync(pesaje, "",
                p => p.NumeroCPE, p => p.PatenteCamion, p => p.PatenteAcoplado, p => p.NombreChofer,
                p => p.KilosDeclarados, p => p.PesoBruto, p => p.PesoTara, p => p.Estado,
                p => p.ProveedorId, p => p.ProductoId);

            if (actualizado)
            {
                try
                {
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pesaje = await _context.Pesajes.FindAsync(id);
            if (pesaje != null)
            {
                _context.Pesajes.Remove(pesaje);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

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

            ViewData["ProveedorId"] = new SelectList(proveedores, nameof(Proveedor.Id), nameof(Proveedor.RazonSocial), proveedorId);
            ViewData["ProductoId"] = new SelectList(
                productos.Select(p => new { p.Id, Nombre = string.IsNullOrWhiteSpace(p.Variedad) ? p.Nombre : $"{p.Nombre} ({p.Variedad})" }),
                "Id", "Nombre", productoId);
        }

        private bool PesajeExists(int id) => _context.Pesajes.Any(e => e.Id == id);
    }
}
