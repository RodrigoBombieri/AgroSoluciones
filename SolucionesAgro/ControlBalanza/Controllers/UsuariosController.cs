using ControlBalanza.Models;
using ControlBalanza.Seguridad;
using ControlBalanza.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControlBalanza.Controllers
{
    /// <summary>ABM de usuarios y asignación de roles. Desactivar = bloquear el login (no se borran: quedan en el historial).</summary>
    [Authorize(Roles = Roles.Admin)]
    public class UsuariosController : Controller
    {
        private readonly UserManager<Usuario> _userManager;

        public UsuariosController(UserManager<Usuario> userManager) => _userManager = userManager;

        public async Task<IActionResult> Index()
        {
            var usuarios = await _userManager.Users.AsNoTracking().OrderBy(u => u.Apellido).ThenBy(u => u.Nombre).ToListAsync();
            var lista = new List<UsuarioListadoItem>();
            foreach (var u in usuarios)
            {
                lista.Add(new UsuarioListadoItem
                {
                    Id = u.Id,
                    NombreCompleto = u.NombreCompleto,
                    Email = u.Email ?? u.UserName ?? "",
                    Roles = await _userManager.GetRolesAsync(u),
                    Activo = !EstaBloqueado(u)
                });
            }
            return View(lista);
        }

        public IActionResult Create() => View(new UsuarioCrearViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCrearViewModel modelo)
        {
            ValidarRoles(modelo.RolesSeleccionados);
            if (!ModelState.IsValid) return View(modelo);

            var usuario = new Usuario
            {
                UserName = modelo.Email.Trim(),
                Email = modelo.Email.Trim(),
                EmailConfirmed = true,
                Nombre = modelo.Nombre.Trim(),
                Apellido = modelo.Apellido.Trim()
            };

            var resultado = await _userManager.CreateAsync(usuario, modelo.Password);
            if (resultado.Succeeded && modelo.RolesSeleccionados.Count > 0)
                resultado = await _userManager.AddToRolesAsync(usuario, modelo.RolesSeleccionados);

            if (!resultado.Succeeded)
            {
                AgregarErrores(resultado);
                return View(modelo);
            }

            TempData["Exito"] = $"Usuario {usuario.Email} creado.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound();

            return View(new UsuarioEditarViewModel
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email ?? "",
                RolesSeleccionados = (await _userManager.GetRolesAsync(usuario)).ToList(),
                Activo = !EstaBloqueado(usuario)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UsuarioEditarViewModel modelo)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound();

            ValidarRoles(modelo.RolesSeleccionados);

            // Un admin no puede quitarse a sí mismo el rol Admin ni desactivarse (se quedaría afuera)
            var esElMismo = usuario.Id == _userManager.GetUserId(User);
            if (esElMismo && !modelo.RolesSeleccionados.Contains(Roles.Admin))
                ModelState.AddModelError(nameof(modelo.RolesSeleccionados), "No puede quitarse su propio rol de Administrador.");
            if (esElMismo && !modelo.Activo)
                ModelState.AddModelError(nameof(modelo.Activo), "No puede desactivar su propio usuario.");

            if (!ModelState.IsValid)
            {
                modelo.Id = id;
                return View(modelo);
            }

            usuario.Nombre = modelo.Nombre.Trim();
            usuario.Apellido = modelo.Apellido.Trim();
            usuario.Email = modelo.Email.Trim();
            usuario.UserName = modelo.Email.Trim();

            var resultado = await _userManager.UpdateAsync(usuario);

            if (resultado.Succeeded)
            {
                var actuales = await _userManager.GetRolesAsync(usuario);
                var quitar = actuales.Except(modelo.RolesSeleccionados).ToList();
                var agregar = modelo.RolesSeleccionados.Except(actuales).ToList();
                if (quitar.Count > 0) resultado = await _userManager.RemoveFromRolesAsync(usuario, quitar);
                if (resultado.Succeeded && agregar.Count > 0) resultado = await _userManager.AddToRolesAsync(usuario, agregar);
            }

            if (resultado.Succeeded)
            {
                await _userManager.SetLockoutEnabledAsync(usuario, true);
                resultado = await _userManager.SetLockoutEndDateAsync(usuario, modelo.Activo ? null : DateTimeOffset.MaxValue);
            }

            if (resultado.Succeeded && !string.IsNullOrWhiteSpace(modelo.NuevaPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);
                resultado = await _userManager.ResetPasswordAsync(usuario, token, modelo.NuevaPassword);
            }

            if (!resultado.Succeeded)
            {
                AgregarErrores(resultado);
                modelo.Id = id;
                return View(modelo);
            }

            // Obliga a revalidar la cookie (roles nuevos / usuario desactivado)
            await _userManager.UpdateSecurityStampAsync(usuario);

            TempData["Exito"] = $"Usuario {usuario.Email} actualizado.";
            return RedirectToAction(nameof(Index));
        }

        private static bool EstaBloqueado(Usuario u) => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow;

        private void ValidarRoles(List<string> roles)
        {
            if (roles.Any(r => !Roles.Todos.Contains(r)))
                ModelState.AddModelError(nameof(UsuarioCrearViewModel.RolesSeleccionados), "Rol inválido.");
        }

        private void AgregarErrores(IdentityResult resultado)
        {
            foreach (var error in resultado.Errors) ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
