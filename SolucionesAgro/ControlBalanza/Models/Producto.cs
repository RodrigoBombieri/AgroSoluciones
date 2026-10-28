using System.ComponentModel.DataAnnotations;

namespace ControlBalanza.Models
{
    public class Producto
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Variedad { get; set; }
    }
}
