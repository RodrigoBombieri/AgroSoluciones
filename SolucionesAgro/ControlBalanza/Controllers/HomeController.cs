using System.Diagnostics;
using ControlBalanza.Models;
using ControlBalanza.Seguridad;
using ControlBalanza.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlBalanza.Controllers
{
    public class HomeController : Controller
    {
        private readonly ReportesService _reportes;

        public HomeController(ReportesService reportes) => _reportes = reportes;

        // Dashboard con el estado de la planta
        public async Task<IActionResult> Index() => View(await _reportes.ObtenerDashboardAsync());

        // Panel del playón (app Angular)
        [Authorize(Roles = Roles.AdminOPlayon)]
        public IActionResult Playon() => View();

        [AllowAnonymous]
        public IActionResult Privacy() => View();

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
