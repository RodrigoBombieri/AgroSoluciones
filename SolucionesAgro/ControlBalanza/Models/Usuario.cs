using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ControlBalanza.Models
{
    public class Usuario : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Apellido { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }

        public string? ImagenUrlPerfil { get; set; }

        public string NombreCompleto => $"{Nombre} {Apellido}";
    }
}
