using ControlBalanza.Data;
using ControlBalanza.Models;
using ControlBalanza.Seguridad;
using ControlBalanza.Validacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class ProveedoresController : Controller
    {
        private readonly AgroDbContext _context;

        public ProveedoresController(AgroDbContext context) => _context = context;

        public async Task<IActionResult> Index(string? buscar)
        {
            var q = _context.Proveedores.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();
                q = q.Where(p => p.RazonSocial.Contains(texto) || p.CUIT.Contains(texto) || (p.Localidad != null && p.Localidad.Contains(texto)));
            }
            ViewData["Buscar"] = buscar;
            return View(await q.OrderBy(p => p.RazonSocial).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var proveedor = await _context.Proveedores.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (proveedor == null) return NotFound();

            ViewData["CantidadPesajes"] = await _context.Pesajes.CountAsync(p => p.ProveedorId == id);
            return View(proveedor);
        }

        public IActionResult Create() => View(new Proveedor());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("RazonSocial,CUIT,Localidad,Telefono")] Proveedor proveedor)
        {
            await NormalizarYValidar(proveedor);
            if (!ModelState.IsValid) return View(proveedor);

            _context.Add(proveedor);
            await _context.SaveChangesAsync();
            TempData["Exito"] = $"Proveedor {proveedor.RazonSocial} creado.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();
            return View(proveedor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,RazonSocial,CUIT,Localidad,Telefono")] Proveedor proveedor)
        {
            if (id != proveedor.Id) return NotFound();

            await NormalizarYValidar(proveedor);
            if (!ModelState.IsValid) return View(proveedor);

            try
            {
                _context.Update(proveedor);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Proveedores.AnyAsync(e => e.Id == id)) return NotFound();
                throw;
            }
            TempData["Exito"] = "Proveedor actualizado.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var proveedor = await _context.Proveedores.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (proveedor == null) return NotFound();

            ViewData["CantidadPesajes"] = await _context.Pesajes.CountAsync(p => p.ProveedorId == id);
            return View(proveedor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // No se borran proveedores con historial: se perdería la trazabilidad de los pesajes
            if (await _context.Pesajes.AnyAsync(p => p.ProveedorId == id))
            {
                TempData["Error"] = "No se puede eliminar: el proveedor tiene pesajes registrados.";
                return RedirectToAction(nameof(Index));
            }

            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor != null)
            {
                _context.Proveedores.Remove(proveedor);
                await _context.SaveChangesAsync();
                TempData["Exito"] = $"Proveedor {proveedor.RazonSocial} eliminado.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task NormalizarYValidar(Proveedor proveedor)
        {
            proveedor.RazonSocial = proveedor.RazonSocial?.Trim() ?? "";
            if (Cuit.EsValido(proveedor.CUIT))
            {
                // Se guarda siempre como XX-XXXXXXXX-X
                proveedor.CUIT = Cuit.Formatear(proveedor.CUIT);

                var digitos = Cuit.SoloDigitos(proveedor.CUIT);
                var otros = await _context.Proveedores.AsNoTracking()
                    .Where(p => p.Id != proveedor.Id)
                    .Select(p => p.CUIT)
                    .ToListAsync();
                if (otros.Any(c => Cuit.SoloDigitos(c) == digitos))
                    ModelState.AddModelError(nameof(Proveedor.CUIT), "Ya existe un proveedor con ese CUIT.");
            }
        }
    }
}
