using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ControlBalanza.Models
{
    public class Proveedor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Razón Social")]
        public string RazonSocial { get; set; }

        [Required]
        [StringLength(20)]
        public string CUIT { get; set; }

        [StringLength(100)]
        public string Localidad { get; set; }

        [StringLength(50)]
        [DataType(DataType.PhoneNumber)]
        public string Telefono { get; set; }

        public ICollection<Pesaje> Pesajes { get; set; }
    }
}
