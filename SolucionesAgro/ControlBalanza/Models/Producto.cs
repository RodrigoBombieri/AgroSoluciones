using System.ComponentModel.DataAnnotations;

namespace ControlBalanza.Models
{
    public class Producto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(100)]
        public string? Variedad { get; set; }

    }
}
