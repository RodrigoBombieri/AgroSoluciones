using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ControlBalanza.Models
{
    public class Proveedor
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "La razón social es obligatoria")]
        [StringLength(200)]
        [Display(Name = "Razón Social")]
        public string RazonSocial { get; set; } = string.Empty;

        [Required(ErrorMessage = "El CUIT es obligatorio")]
        [StringLength(20)]
        public string CUIT { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Localidad { get; set; }

        [StringLength(50)]
        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [ValidateNever]
        public ICollection<Pesaje> Pesajes { get; set; } = new List<Pesaje>();
    }
}
