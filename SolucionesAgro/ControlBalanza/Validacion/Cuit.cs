using System.ComponentModel.DataAnnotations;

namespace ControlBalanza.Validacion
{
    /// <summary>
    /// Utilidades para CUIT/CUIL argentinos (11 dígitos, dígito verificador módulo 11).
    /// </summary>
    public static class Cuit
    {
        private static readonly int[] Pesos = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };

        /// <summary>Devuelve solo los dígitos.</summary>
        public static string SoloDigitos(string? valor) =>
            new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());

        public static bool EsValido(string? valor)
        {
            var d = SoloDigitos(valor);
            if (d.Length != 11) return false;

            var suma = 0;
            for (var i = 0; i < 10; i++) suma += (d[i] - '0') * Pesos[i];

            var verificador = 11 - (suma % 11);
            if (verificador == 11) verificador = 0;
            if (verificador == 10) return false; // No existe CUIT con verificador 10

            return verificador == d[10] - '0';
        }

        /// <summary>Formatea como XX-XXXXXXXX-X. Si no tiene 11 dígitos, devuelve el valor original recortado.</summary>
        public static string Formatear(string? valor)
        {
            var d = SoloDigitos(valor);
            return d.Length == 11 ? $"{d[..2]}-{d[2..10]}-{d[10]}" : (valor ?? string.Empty).Trim();
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class CuitAttribute : ValidationAttribute
    {
        public CuitAttribute() : base("El CUIT no es válido (verifique los 11 dígitos y el dígito verificador)") { }

        public override bool IsValid(object? value)
        {
            var texto = value as string;
            return string.IsNullOrWhiteSpace(texto) || Cuit.EsValido(texto);
        }
    }
}
