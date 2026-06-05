
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlBalanza.Models;
using ControlBalanza.Data;

public class PesajesController : Controller
{
    private readonly AgroDbContext _context;

    public PesajesController(AgroDbContext context)
    {
        _context = context;
    }

    // GET: PESAJES
    public async Task<IActionResult> Index()    
    {
        return View(await _context.Pesajes.ToListAsync());
    }

    // GET: PESAJES/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var pesaje = await _context.Pesajes
            .FirstOrDefaultAsync(m => m.Id == id);
        if (pesaje == null)
        {
            return NotFound();
        }

        return View(pesaje);
    }

    // GET: PESAJES/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: PESAJES/Create
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,NumeroCPE,PatenteCamion,PatenteAcoplado,NombreChofer,KilosDeclarados,PesoBruto,PesoTara,PesoNeto,Estado,FechaEntrada,FechaSalida,UsuarioBalanzaEntradaId,UsuarioPlayonId,UsuarioBalanzaSalidaId,ProveedorId,Proveedor,ProductoId,Producto")] Pesaje pesaje)
    {
        if (ModelState.IsValid)
        {
            _context.Add(pesaje);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(pesaje);
    }

    // GET: PESAJES/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var pesaje = await _context.Pesajes.FindAsync(id);
        if (pesaje == null)
        {
            return NotFound();
        }
        return View(pesaje);
    }

    // POST: PESAJES/Edit/5
    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, [Bind("Id,NumeroCPE,PatenteCamion,PatenteAcoplado,NombreChofer,KilosDeclarados,PesoBruto,PesoTara,PesoNeto,Estado,FechaEntrada,FechaSalida,UsuarioBalanzaEntradaId,UsuarioPlayonId,UsuarioBalanzaSalidaId,ProveedorId,Proveedor,ProductoId,Producto")] Pesaje pesaje)
    {
        if (id != pesaje.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(pesaje);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PesajeExists(pesaje.Id))
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
        return View(pesaje);
    }

    // GET: PESAJES/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var pesaje = await _context.Pesajes
            .FirstOrDefaultAsync(m => m.Id == id);
        if (pesaje == null)
        {
            return NotFound();
        }

        return View(pesaje);
    }

    // POST: PESAJES/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        var pesaje = await _context.Pesajes.FindAsync(id);
        if (pesaje != null)
        {
            _context.Pesajes.Remove(pesaje);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool PesajeExists(int? id)
    {
        return _context.Pesajes.Any(e => e.Id == id);
    }
}
