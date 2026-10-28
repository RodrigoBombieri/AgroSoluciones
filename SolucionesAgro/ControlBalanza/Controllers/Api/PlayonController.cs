using System.ComponentModel.DataAnnotations;
using ControlBalanza.Data;
using ControlBalanza.Models;
using ControlBalanza.Seguridad;
using ControlBalanza.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Controllers.Api
{
    /// <summary>Camión tal como lo ve el panel del playón.</summary>
    public record CamionPlayonDto(
        int Id,
        string NumeroCPE,
        string PatenteCamion,
        string? PatenteAcoplado,
        string? NombreChofer,
        string? Proveedor,
        string? Producto,
        double KilosDeclarados,
        double? PesoBruto,
        string Estado,
        DateTime FechaEntrada,
        int MinutosEnPlanta,
        string? SiloDestino,
        string? Observaciones,
        string? UsuarioPlayon);

    public class IniciarDescargaRequest
    {
        [StringLength(50)]
        public string? SiloDestino { get; set; }
    }

    public class FinalizarDescargaRequest
    {
        [StringLength(200)]
        public string? Observaciones { get; set; }
    }

    /// <summary>
    /// API del panel del playón (Angular). Usa la misma cookie de login que el resto del sitio;
    /// los POST exigen el token antiforgery en el header X-XSRF-TOKEN.
    /// </summary>
    [ApiController]
    [Route("api/playon")]
    [Authorize(Roles = Roles.AdminOPlayon)]
    public class PlayonController : ControllerBase
    {
        private readonly AgroDbContext _context;
        private readonly IPesajeWorkflowService _workflow;
        private readonly UserManager<Usuario> _userManager;
        private readonly TimeProvider _reloj;

        public PlayonController(AgroDbContext context, IPesajeWorkflowService workflow, UserManager<Usuario> userManager, TimeProvider reloj)
        {
            _context = context;
            _workflow = workflow;
            _userManager = userManager;
            _reloj = reloj;
        }

        // GET api/playon/camiones — camiones en espera y descargando, en orden de llegada
        [HttpGet("camiones")]
        public async Task<ActionResult<IEnumerable<CamionPlayonDto>>> GetCamiones()
        {
            var pesajes = await _context.Pesajes.AsNoTracking()
                .Include(p => p.Proveedor)
                .Include(p => p.Producto)
                .Include(p => p.UsuarioPlayon)
                .Where(p => p.Estado == EstadoPesaje.EnEspera || p.Estado == EstadoPesaje.EnDescarga)
                .OrderBy(p => p.FechaEntrada)
                .ToListAsync();

            return Ok(pesajes.Select(ADto));
        }

        // POST api/playon/camiones/5/iniciar-descarga
        [HttpPost("camiones/{id:int}/iniciar-descarga")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<CamionPlayonDto>> IniciarDescarga(int id, [FromBody] IniciarDescargaRequest? request)
        {
            var usuarioId = _userManager.GetUserId(User)!;
            var resultado = await _workflow.IniciarDescargaAsync(id, usuarioId, request?.SiloDestino);
            return await Responder(id, resultado);
        }

        // POST api/playon/camiones/5/finalizar-descarga
        [HttpPost("camiones/{id:int}/finalizar-descarga")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult<CamionPlayonDto>> FinalizarDescarga(int id, [FromBody] FinalizarDescargaRequest? request)
        {
            var usuarioId = _userManager.GetUserId(User)!;
            var resultado = await _workflow.FinalizarDescargaAsync(id, usuarioId, request?.Observaciones);
            return await Responder(id, resultado);
        }

        private async Task<ActionResult<CamionPlayonDto>> Responder(int id, ResultadoOperacion resultado)
        {
            if (!resultado.Ok)
            {
                // 409: el camión cambió de estado (por ejemplo, otro operario ya lo movió)
                return Conflict(new ProblemDetails { Title = "No se pudo completar la operación", Detail = resultado.MensajeError });
            }

            var pesaje = await _context.Pesajes.AsNoTracking()
                .Include(p => p.Proveedor).Include(p => p.Producto).Include(p => p.UsuarioPlayon)
                .FirstAsync(p => p.Id == id);
            return Ok(ADto(pesaje));
        }

        private CamionPlayonDto ADto(Pesaje p) => new(
            p.Id,
            p.NumeroCPE,
            p.PatenteCamion,
            p.PatenteAcoplado,
            p.NombreChofer,
            p.Proveedor?.RazonSocial,
            p.Producto == null ? null : string.IsNullOrWhiteSpace(p.Producto.Variedad) ? p.Producto.Nombre : $"{p.Producto.Nombre} {p.Producto.Variedad}",
            p.KilosDeclarados,
            p.PesoBruto,
            p.Estado.ToString(),
            p.FechaEntrada,
            (int)Math.Max(0, (_reloj.GetLocalNow().DateTime - p.FechaEntrada).TotalMinutes),
            p.SiloDestino,
            p.Observaciones,
            p.UsuarioPlayon?.NombreCompleto);
    }
}
