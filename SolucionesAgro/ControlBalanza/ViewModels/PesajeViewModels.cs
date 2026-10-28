using System.ComponentModel.DataAnnotations;
using ControlBalanza.Models;

namespace ControlBalanza.ViewModels
{
    /// <summary>Formulario de la balanza de salida: se registra la tara del camión vacío.</summary>
    public class RegistrarSalidaViewModel
    {
        public int PesajeId { get; set; }

        [Required(ErrorMessage = "Ingrese la tara")]
        [Range(1, 100000, ErrorMessage = "La tara debe estar entre 1 y 100.000 kg")]
        [Display(Name = "Peso Tara (kg)")]
        public double? PesoTara { get; set; }

        [StringLength(200)]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        // Solo lectura, para mostrar
        public Pesaje? Pesaje { get; set; }
    }

    /// <summary>Resultado mostrado tras registrar la salida (ticket).</summary>
    public class SalidaRegistradaViewModel
    {
        public Pesaje Pesaje { get; set; } = null!;
        public bool FueraDeTolerancia { get; set; }
        public double Tolerancia { get; set; }
    }
}
