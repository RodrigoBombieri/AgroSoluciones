using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlBalanza.Models
{
    public enum EstadoPesaje { EnEspera, EnDescarga, ListoSalida, Completado }
    public class Pesaje
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Número CPE")]
        public string NumeroCPE { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Patente Camión")]
        public string PatenteCamion { get; set; }

        [StringLength(20)]
        [Display(Name = "Patente Acoplado")]
        public string PatenteAcoplado { get; set; }

        [StringLength(100)]
        [Display(Name = "Nombre Chofer")]
        public string NombreChofer { get; set; }

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

        [Required]
        [Display(Name = "Usuario Balanza Entrada Id")]
        public int UsuarioBalanzaEntradaId { get; set; } // FK al usuario que registró la entrada

        [Display(Name = "Usuario Playón Id")]
        public int? UsuarioPlayonId { get; set; } // FK al usuario que registró la descarga

        [Display(Name = "Usuario Balanza Salida Id")]
        public int? UsuarioBalanzaSalidaId { get; set; } // FK al usuario que registró la salida 

        [Required]
        public int ProveedorId { get; set; }

        [Required]
        public Proveedor Proveedor { get; set; }

        [Required]
        public int ProductoId { get; set; }

        [Required]
        public Producto Producto { get; set; }
    }
}
