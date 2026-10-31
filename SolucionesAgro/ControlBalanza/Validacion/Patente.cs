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

        // Confusiones típicas del OCR entre letras y números
        private static readonly Dictionary<char, char> ALetra = new() { ['0'] = 'O', ['1'] = 'I', ['2'] = 'Z', ['5'] = 'S', ['6'] = 'G', ['8'] = 'B' };
        private static readonly Dictionary<char, char> ANumero = new()
        {
            ['O'] = '0', ['D'] = '0', ['Q'] = '0', ['U'] = '0', ['I'] = '1', ['L'] = '1', ['T'] = '7',
            ['Z'] = '2', ['S'] = '5', ['B'] = '8', ['G'] = '6', ['E'] = '3', ['A'] = '4'
        };

        /// <summary>
        /// Intenta convertir una patente leída por OCR a un formato válido corrigiendo letras/números confundidos
        /// (ej: "HOMOOE" → "HOM003"). Devuelve null si no se puede.
        /// </summary>
        public static string? CorregirOcr(string? valor)
        {
            var p = Normalizar(valor);
            if (EsValida(p)) return p;

            var candidatos = new List<string> { p };
            // El OCR a veces duplica un carácter (ej: "HOMO003"): se prueba quitando uno que sea
            // igual (o confundible) con su vecino, nunca uno cualquiera.
            for (var i = 0; i < p.Length; i++)
            {
                var duplicado = (i > 0 && Equivalentes(p[i], p[i - 1])) || (i < p.Length - 1 && Equivalentes(p[i], p[i + 1]));
                if (duplicado) candidatos.Add(p.Remove(i, 1));
            }

            // Formatos: L = letra, N = número
            foreach (var formato in new[] { "LLLNNN", "LLNNNLL" })
            {
                foreach (var c in candidatos.Where(c => c.Length == formato.Length))
                {
                    var resultado = AplicarFormato(c, formato);
                    if (resultado != null && EsValida(resultado)) return resultado;
                }
            }
            return null;
        }

        private static string? AplicarFormato(string texto, string formato)
        {
            var chars = new char[texto.Length];
            for (var i = 0; i < texto.Length; i++)
            {
                var ch = texto[i];
                if (formato[i] == 'L')
                {
                    if (char.IsLetter(ch)) chars[i] = ch;
                    else if (ALetra.TryGetValue(ch, out var letra)) chars[i] = letra;
                    else return null;
                }
                else
                {
                    if (char.IsDigit(ch)) chars[i] = ch;
                    else if (ANumero.TryGetValue(ch, out var numero)) chars[i] = numero;
                    else return null;
                }
            }
            return new string(chars);
        }

        private static bool Equivalentes(char a, char b) =>
            a == b || (ANumero.TryGetValue(a, out var na) && na == b) || (ANumero.TryGetValue(b, out var nb) && nb == a);
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
