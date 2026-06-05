
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlBalanza.Models;
using ControlBalanza.Data;

public class ProveedoresController : Controller
{
    private readonly AgroDbContext _context;

    public ProveedoresController(AgroDbContext context)
    {
        _context = context;
    }

    // GET: PROVEEDORS
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Proveedores.ToListAsync());
    }

    // GET: PROVEEDORS/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var proveedor = await _context.Proveedores
            .FirstOrDefaultAsync(m => m.Id == id);
        if (proveedor == null)
        {
            return NotFound();
        }

        return View(proveedor);
    }

    // GET: PROVEEDORS/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PROVEEDORS/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,RazonSocial,CUIT,Localidad,Telefono,Pesajes")] Proveedor proveedor)
    {
        if (ModelState.IsValid)
        {
            _context.Add(proveedor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(proveedor);
    }

    // GET: PROVEEDORS/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var proveedor = await _context.Proveedores.FindAsync(id);
        if (proveedor == null)
        {
            return NotFound();
        }
        return View(proveedor);
    }

    // POST: PROVEEDORS/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,RazonSocial,CUIT,Localidad,Telefono,Pesajes")] Proveedor proveedor)
    {
        if (id != proveedor.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(proveedor);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProveedorExists(proveedor.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(proveedor);
    }

    // GET: PROVEEDORS/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var proveedor = await _context.Proveedores
            .FirstOrDefaultAsync(m => m.Id == id);
        if (proveedor == null)
        {
            return NotFound();
        }

        return View(proveedor);
    }

    // POST: PROVEEDORS/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var proveedor = await _context.Proveedores.FindAsync(id);
        if (proveedor != null)
        {
            _context.Proveedores.Remove(proveedor);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ProveedorExists(int? id)
    {
        return _context.Proveedores.Any(e => e.Id == id);
    }
}
