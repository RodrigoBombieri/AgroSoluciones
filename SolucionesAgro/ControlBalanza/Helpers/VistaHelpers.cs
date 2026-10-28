using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ControlBalanza.Models;

namespace ControlBalanza.Helpers
{
    public static class VistaHelpers
    {
        /// <summary>Texto del enum según su [Display(Name)].</summary>
        public static string Estado(EstadoPesaje estado) =>
            typeof(EstadoPesaje).GetField(estado.ToString())?.GetCustomAttribute<DisplayAttribute>()?.Name ?? estado.ToString();

        public static string Kg(double? valor) => valor.HasValue ? $"{valor.Value:N0} kg" : "—";

        public static string Fecha(DateTime? fecha) => fecha?.ToString("dd/MM/yyyy HH:mm") ?? "—";

        /// <summary>"2 h 15 min" a partir de minutos.</summary>
        public static string Duracion(double? minutos)
        {
            if (!minutos.HasValue) return "—";
            var m = (int)Math.Round(minutos.Value);
            return m >= 60 ? $"{m / 60} h {m % 60} min" : $"{m} min";
        }
    }
}
