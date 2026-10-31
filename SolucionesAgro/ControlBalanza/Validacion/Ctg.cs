using System.ComponentModel.DataAnnotations;

namespace ControlBalanza.Validacion
{
    /// <summary>
    /// CTG (Código de Trazabilidad de Granos) de una CPE. Según el manual WSCPE de ARCA es un número
    /// de 12 dígitos XXYYZZZZZZZZ (XX = 01 automotor / 02 ferroviaria); como empieza con 0,
    /// en la CPE impresa y en su QR aparece con 11 dígitos (ej: 10100014235).
    /// </summary>
    public static class Ctg
    {
        /// <summary>Solo dígitos y sin el 0 inicial (forma impresa de 11 dígitos).</summary>
        public static string Normalizar(string? valor)
        {
            var d = new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
            return d.Length == 12 && d[0] == '0' ? d[1..] : d;
        }

        public static bool EsValido(string? valor)
        {
            var d = Normalizar(valor);
            return d.Length == 11 && (d[0] == '1' || d[0] == '2');
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CtgAttribute : ValidationAttribute
    {
        public CtgAttribute() : base("El CTG no es válido (11 dígitos, empieza con 1 automotor o 2 ferroviaria)") { }

        public override bool IsValid(object? value)
        {
            var texto = value as string;
            return string.IsNullOrWhiteSpace(texto) || Ctg.EsValido(texto);
        }
    }
}
