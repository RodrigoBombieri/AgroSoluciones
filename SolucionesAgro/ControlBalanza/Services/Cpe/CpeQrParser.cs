using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ControlBalanza.Validacion;

namespace ControlBalanza.Services.Cpe
{
    /// <summary>
    /// Interpreta el contenido del QR impreso en la CPE.
    ///
    /// AFIP no publica un formato único y estable para este QR, así que el parser es tolerante:
    ///  1. Si el texto es JSON (o una URL con un parámetro en JSON / Base64-JSON), mapea las claves conocidas.
    ///  2. Si no, busca con expresiones regulares los datos reconocibles (CPE, CTG, patentes, CUIT, kilos).
    /// Cuando haya una CPE real a mano, conviene agregar el formato exacto en <see cref="ClavesConocidas"/>.
    /// </summary>
    public static partial class CpeQrParser
    {
        // Claves (normalizadas: minúsculas, sin guiones/guiones bajos) → campo de CpeDatos
        private static readonly Dictionary<string, string> ClavesConocidas = new()
        {
            ["nrocpe"] = "cpe", ["numerocpe"] = "cpe", ["cpe"] = "cpe", ["nrocartaporte"] = "cpe", ["cartadeporte"] = "cpe",
            ["ctg"] = "ctg", ["nroctg"] = "ctg", ["numeroctg"] = "ctg",
            ["patente"] = "patente", ["dominio"] = "patente", ["patentecamion"] = "patente", ["dominiocamion"] = "patente", ["patentechasis"] = "patente",
            ["patenteacoplado"] = "acoplado", ["dominioacoplado"] = "acoplado", ["acoplado"] = "acoplado",
            ["chofer"] = "chofer", ["nombrechofer"] = "chofer", ["conductor"] = "chofer",
            ["cuitremitente"] = "cuit", ["cuittitular"] = "cuit", ["cuitremitentecomercialproductor"] = "cuit", ["cuitproductor"] = "cuit", ["cuit"] = "cuit",
            ["razonsocial"] = "razon", ["remitente"] = "razon", ["titular"] = "razon", ["productor"] = "razon",
            ["grano"] = "producto", ["especie"] = "producto", ["producto"] = "producto", ["codgrano"] = "producto",
            ["kilos"] = "kilos", ["pesobruto"] = "kilos", ["kgnetos"] = "kilos", ["pesoneto"] = "kilos", ["kilosnetos"] = "kilos", ["kgdeclarados"] = "kilos", ["kilosdeclarados"] = "kilos"
        };

        [GeneratedRegex(@"\b(\d{5})-?(\d{8})\b")]
        private static partial Regex RegexCpe();

        [GeneratedRegex(@"\b(\d{11,12})\b")]
        private static partial Regex RegexCtg();

        [GeneratedRegex(@"\b(\d{2})-?(\d{8})-?(\d)\b")]
        private static partial Regex RegexCuit();

        [GeneratedRegex(@"\b([A-Za-z]{2}\s?\d{3}\s?[A-Za-z]{2}|[A-Za-z]{3}\s?\d{3})\b")]
        private static partial Regex RegexPatente();

        public static CpeDatos Interpretar(string? contenido)
        {
            var datos = new CpeDatos { Origen = "QR" };
            if (string.IsNullOrWhiteSpace(contenido))
            {
                datos.Advertencias.Add("El QR está vacío.");
                return datos;
            }

            var texto = contenido.Trim();

            if (!IntentarJson(texto, datos))
            {
                foreach (var candidato in CandidatosDesdeUrl(texto))
                {
                    if (IntentarJson(candidato, datos)) break;
                }
            }

            BuscarPorPatrones(texto, datos);

            if (!datos.TieneDatos)
                datos.Advertencias.Add("No se reconocieron datos en el QR. Complete el formulario manualmente o use la lectura por foto (IA).");
            else if (string.IsNullOrWhiteSpace(datos.NumeroCPE))
                datos.Advertencias.Add("No se encontró el número de CPE en el QR.");

            return datos;
        }

        /// <summary>Si el QR es una URL, devuelve los valores de sus parámetros (crudos y decodificados de Base64).</summary>
        private static IEnumerable<string> CandidatosDesdeUrl(string texto)
        {
            if (!Uri.TryCreate(texto, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Query)) yield break;

            foreach (var par in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var partes = par.Split('=', 2);
                if (partes.Length != 2) continue;
                var valor = Uri.UnescapeDataString(partes[1]);
                yield return valor;
                var decodificado = DecodificarBase64(valor);
                if (decodificado != null) yield return decodificado;
            }
        }

        private static string? DecodificarBase64(string valor)
        {
            try
            {
                var normalizado = valor.Replace('-', '+').Replace('_', '/');
                normalizado = normalizado.PadRight(normalizado.Length + (4 - normalizado.Length % 4) % 4, '=');
                return Encoding.UTF8.GetString(Convert.FromBase64String(normalizado));
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static bool IntentarJson(string texto, CpeDatos datos)
        {
            if (!texto.StartsWith('{')) return false;
            try
            {
                using var doc = JsonDocument.Parse(texto);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
                RecorrerJson(doc.RootElement, datos);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static void RecorrerJson(JsonElement objeto, CpeDatos datos)
        {
            foreach (var prop in objeto.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    RecorrerJson(prop.Value, datos);
                    continue;
                }

                var clave = new string(prop.Name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
                if (!ClavesConocidas.TryGetValue(clave, out var campo)) continue;

                var valor = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    _ => null
                };
                if (string.IsNullOrWhiteSpace(valor)) continue;

                Asignar(datos, campo, valor.Trim());
            }
        }

        private static void Asignar(CpeDatos datos, string campo, string valor)
        {
            switch (campo)
            {
                case "cpe": datos.NumeroCPE ??= valor; break;
                case "ctg": datos.Ctg ??= valor; break;
                case "patente": datos.PatenteCamion ??= Patente.Normalizar(valor); break;
                case "acoplado": datos.PatenteAcoplado ??= Patente.Normalizar(valor); break;
                case "chofer": datos.NombreChofer ??= valor; break;
                case "cuit": datos.CuitProveedor ??= Cuit.Formatear(valor); break;
                case "razon": datos.RazonSocialProveedor ??= valor; break;
                case "producto": datos.Producto ??= valor; break;
                case "kilos":
                    if (datos.KilosDeclarados == null && double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var kg))
                        datos.KilosDeclarados = kg;
                    break;
            }
        }

        /// <summary>Completa lo que falte buscando patrones en el texto crudo.</summary>
        private static void BuscarPorPatrones(string texto, CpeDatos datos)
        {
            if (datos.NumeroCPE == null)
            {
                var m = RegexCpe().Match(texto);
                if (m.Success) datos.NumeroCPE = $"{m.Groups[1].Value}-{m.Groups[2].Value}";
            }

            if (datos.CuitProveedor == null)
            {
                foreach (Match m in RegexCuit().Matches(texto))
                {
                    var cuit = m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value;
                    if (Cuit.EsValido(cuit)) { datos.CuitProveedor = Cuit.Formatear(cuit); break; }
                }
            }

            if (datos.Ctg == null)
            {
                foreach (Match m in RegexCtg().Matches(texto))
                {
                    // Evita confundir un CUIT con el CTG
                    if (Cuit.SoloDigitos(datos.CuitProveedor) == m.Value) continue;
                    datos.Ctg = m.Value;
                    break;
                }
            }

            // Patentes solo si el texto no es una URL (en URLs aparecen palabras que matchean el patrón)
            if (datos.PatenteCamion == null && !texto.Contains("://"))
            {
                var patentes = RegexPatente().Matches(texto)
                    .Select(m => Patente.Normalizar(m.Value))
                    .Where(Patente.EsValida)
                    .Distinct()
                    .ToList();
                if (patentes.Count > 0) datos.PatenteCamion = patentes[0];
                if (patentes.Count > 1 && datos.PatenteAcoplado == null) datos.PatenteAcoplado = patentes[1];
            }
        }
    }
}
