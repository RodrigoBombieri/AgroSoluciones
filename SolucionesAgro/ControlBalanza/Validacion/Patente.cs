using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ControlBalanza.Validacion
{
    /// <summary>
    /// Patentes argentinas: formato viejo AAA999 y Mercosur AA999AA (camiones y acoplados).
    /// </summary>
    public static partial class Patente
    {
        [GeneratedRegex("^([A-Z]{3}[0-9]{3}|[A-Z]{2}[0-9]{3}[A-Z]{2})$")]
        private static partial Regex Formato();

        /// <summary>Mayúsculas y sin espacios ni guiones.</summary>
        public static string Normalizar(string? valor) =>
            new string((valor ?? string.Empty).ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

        public static bool EsValida(string? valor) => Formato().IsMatch(Normalizar(valor));
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PatenteAttribute : ValidationAttribute
    {
        public PatenteAttribute() : base("La patente no es válida (formatos AAA999 o AA999AA)") { }

        public override bool IsValid(object? value)
        {
            var texto = value as string;
            return string.IsNullOrWhiteSpace(texto) || Patente.EsValida(texto);
        }
    }
}
