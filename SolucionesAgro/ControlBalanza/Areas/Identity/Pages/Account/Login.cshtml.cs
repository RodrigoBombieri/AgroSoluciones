using System.ComponentModel.DataAnnotations;
using ControlBalanza.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ControlBalanza.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Reemplaza la página de login de Identity UI: en español y sin links de registro
    /// (los usuarios los da de alta un administrador).
    /// </summary>
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly SignInManager<Usuario> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<Usuario> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Ingrese su email")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Ingrese su contraseña")]
            [DataType(DataType.Password)]
            [Display(Name = "Contraseña")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Mantener la sesión iniciada")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            // Limpia cookies externas para un login limpio
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid) return Page();

            var resultado = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
            if (resultado.Succeeded)
            {
                _logger.LogInformation("Inicio de sesión: {Email}", Input.Email);
                return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/"));
            }

            ModelState.AddModelError(string.Empty, resultado.IsLockedOut
                ? "El usuario está desactivado o bloqueado por intentos fallidos. Consulte con el administrador."
                : "Email o contraseña incorrectos.");
            return Page();
        }
    }
}
