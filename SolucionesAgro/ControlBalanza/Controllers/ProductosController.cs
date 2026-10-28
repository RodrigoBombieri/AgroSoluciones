using ControlBalanza.Data;
using ControlBalanza.Models;
using ControlBalanza.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class ProductosController : Controller
    {
        private readonly AgroDbContext _context;

        public ProductosController(AgroDbContext context) => _context = context;

        public async Task<IActionResult> Index() =>
            View(await _context.Productos.AsNoTracking().OrderBy(p => p.Nombre).ThenBy(p => p.Variedad).ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (producto == null) return NotFound();

            ViewData["CantidadPesajes"] = await _context.Pesajes.CountAsync(p => p.ProductoId == id);
            return View(producto);
        }

        public IActionResult Create() => View(new Producto());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Nombre,Variedad")] Producto producto)
        {
            if (!ModelState.IsValid) return View(producto);

            _context.Add(producto);
            await _context.SaveChangesAsync();
            TempData["Exito"] = $"Producto {producto.Nombre} creado.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Variedad")] Producto producto)
        {
            if (id != producto.Id) return NotFound();
            if (!ModelState.IsValid) return View(producto);

            try
            {
                _context.Update(producto);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Productos.AnyAsync(e => e.Id == id)) return NotFound();
                throw;
            }
            TempData["Exito"] = "Producto actualizado.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var producto = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (producto == null) return NotFound();

            ViewData["CantidadPesajes"] = await _context.Pesajes.CountAsync(p => p.ProductoId == id);
            return View(producto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (await _context.Pesajes.AnyAsync(p => p.ProductoId == id))
            {
                TempData["Error"] = "No se puede eliminar: el producto tiene pesajes registrados.";
                return RedirectToAction(nameof(Index));
            }

            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                TempData["Exito"] = $"Producto {producto.Nombre} eliminado.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
