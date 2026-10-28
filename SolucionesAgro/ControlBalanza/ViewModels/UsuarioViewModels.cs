using System.ComponentModel.DataAnnotations;

namespace ControlBalanza.ViewModels
{
    public class UsuarioListadoItem
    {
        public string Id { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public IList<string> Roles { get; set; } = new List<string>();
        public bool Activo { get; set; }
    }

    public class UsuarioCrearViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email (usuario de ingreso)")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Roles")]
        public List<string> RolesSeleccionados { get; set; } = new();
    }

    public class UsuarioEditarViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email (usuario de ingreso)")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Roles")]
        public List<string> RolesSeleccionados { get; set; } = new();

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mínimo 6 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña (dejar vacío para no cambiarla)")]
        public string? NuevaPassword { get; set; }
    }
}
