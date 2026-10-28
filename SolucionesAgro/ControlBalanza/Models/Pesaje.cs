using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ControlBalanza.Models
{
    public enum EstadoPesaje
    {
        [Display(Name = "En espera")] EnEspera,
        [Display(Name = "En descarga")] EnDescarga,
        [Display(Name = "Listo para salir")] ListoSalida,
        [Display(Name = "Completado")] Completado
    }

    public class Pesaje
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El número de CPE es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Número CPE")]
        public string NumeroCPE { get; set; } = string.Empty;

        [Required(ErrorMessage = "La patente del camión es obligatoria")]
        [StringLength(20)]
        [Display(Name = "Patente Camión")]
        public string PatenteCamion { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Patente Acoplado")]
        public string? PatenteAcoplado { get; set; }

        [StringLength(100)]
        [Display(Name = "Nombre Chofer")]
        public string? NombreChofer { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Kilos Declarados")]
        public double KilosDeclarados { get; set; } // Obtenidos de la CPE

        [Range(0, double.MaxValue)]
        [Display(Name = "Peso Bruto")]
        public double? PesoBruto { get; set; } // Se registra en la entrada

        [Range(0, double.MaxValue)]
        [Display(Name = "Peso Tara")]
        public double? PesoTara { get; set; } // Se registra en la salida

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Display(Name = "Peso Neto")]
        public double? PesoNeto { get; private set; } // Computado por la BD

        [Required]
        public EstadoPesaje Estado { get; set; } = EstadoPesaje.EnEspera;

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Entrada")]
        public DateTime FechaEntrada { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Fecha Salida")]
        public DateTime? FechaSalida { get; set; }

        // --- Usuarios que intervienen en cada etapa (FK a AspNetUsers, Id string) ---
        // Se asignan en el servidor a partir del usuario logueado, nunca desde el formulario.

        [Display(Name = "Usuario Balanza Entrada")]
        public string UsuarioBalanzaEntradaId { get; set; } = string.Empty;

        [ValidateNever]
        [Display(Name = "Usuario Balanza Entrada")]
        public Usuario? UsuarioBalanzaEntrada { get; set; }

        [Display(Name = "Usuario Playón")]
        public string? UsuarioPlayonId { get; set; }

        [ValidateNever]
        [Display(Name = "Usuario Playón")]
        public Usuario? UsuarioPlayon { get; set; }

        [Display(Name = "Usuario Balanza Salida")]
        public string? UsuarioBalanzaSalidaId { get; set; }

        [ValidateNever]
        [Display(Name = "Usuario Balanza Salida")]
        public Usuario? UsuarioBalanzaSalida { get; set; }

        // --- Proveedor y producto ---

        [Required(ErrorMessage = "Seleccione un proveedor")]
        [Display(Name = "Proveedor")]
        public int ProveedorId { get; set; }

        [ValidateNever]
        public Proveedor? Proveedor { get; set; }

        [Required(ErrorMessage = "Seleccione un producto")]
        [Display(Name = "Producto")]
        public int ProductoId { get; set; }

        [ValidateNever]
        public Producto? Producto { get; set; }
    }
}
