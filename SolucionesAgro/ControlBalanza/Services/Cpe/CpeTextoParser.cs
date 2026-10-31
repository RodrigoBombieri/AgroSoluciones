using System.Globalization;
using System.Text.RegularExpressions;
using ControlBalanza.Validacion;

namespace ControlBalanza.Services.Cpe
{
    /// <summary>
    /// Extrae los datos de una CPE Automotor a partir de su texto: el del PDF original de ARCA,
    /// o el que devuelve el OCR de una foto/escaneo. Busca por las etiquetas del formulario
    /// ("CTG:", "Titular Carta de Porte:", "Dominios:", etc.) y tolera los errores típicos del OCR.
    /// Todo lo extraído se valida (CUIT con dígito verificador, formato de patente y CTG).
    /// </summary>
    public static partial class CpeTextoParser
    {
        // Patrones tolerantes: el OCR confunde o/0, e/o, etc. en las etiquetas
        [GeneratedRegex(@"CTG\s*[:;.]?\s*([0-9OoIl|]{11,13})")]
        private static partial Regex RegexCtg();

        [GeneratedRegex(@"\b(\d{5})\s*-\s*(\d{8})\b")]
        private static partial Regex RegexNumeroCpe();

        [GeneratedRegex(@"^\s*(?<etiqueta>[^:\n]{3,45}?)\s*:\s*(?<cuit>[0-9OoIl|\s\-/]{11,20}?)\s*-\s*(?<nombre>[^\n]+)$", RegexOptions.Multiline)]
        private static partial Regex RegexInterviniente();

        [GeneratedRegex(@"Grano\s*[/!|l1]\s*esp\w*\s*[:;]\s*(?<grano>[A-Za-zÁÉÍÓÚáéíóúÑñ ]+?)\s+(Tipo|T[il]po)\b", RegexOptions.IgnoreCase)]
        private static partial Regex RegexGrano();

        [GeneratedRegex(@"P[eo]s[oa]\s*(?<tipo>Bruto|Tara|Neto)\s*\(?\s*k\w\s*\)?\s*[:;]?[\s\-—_–.]*(?<valor>[0-9][0-9.,]*)", RegexOptions.IgnoreCase)]
        private static partial Regex RegexPeso();

        [GeneratedRegex(@"Dominios?\s*[:;]\s*(?<valor>[^\n]+)", RegexOptions.IgnoreCase)]
        private static partial Regex RegexDominios();

        [GeneratedRegex(@"Planta\s*\(RUCA\)\s*[:;]\s*(?<planta>\d{1,6})", RegexOptions.IgnoreCase)]
        private static partial Regex RegexPlanta();

        [GeneratedRegex(@"Vencimiento\s*[:;]?\s*(?<fecha>\d{2}/\d{2}/\d{4})(\s+(?<hora>\d{2}:\d{2}))?", RegexOptions.IgnoreCase)]
        private static partial Regex RegexVencimiento();

        public static CpeDatos Interpretar(string? texto, string origen)
        {
            var datos = new CpeDatos { Origen = origen };
            if (string.IsNullOrWhiteSpace(texto))
            {
                datos.Advertencias.Add("No se pudo leer texto del documento.");
                return datos;
            }

            texto = texto.Replace("\r", "");

            // --- Encabezado ---
            var mCtg = RegexCtg().Match(texto);
            if (mCtg.Success)
            {
                var ctg = Ctg.Normalizar(DigitosOcr(mCtg.Groups[1].Value));
                if (Ctg.EsValido(ctg)) datos.Ctg = ctg;
                else datos.Advertencias.Add($"El CTG leído ({mCtg.Groups[1].Value.Trim()}) no es válido; revíselo.");
            }

            var mCpe = RegexNumeroCpe().Match(texto);
            if (mCpe.Success) datos.NumeroCPE = $"{mCpe.Groups[1].Value}-{mCpe.Groups[2].Value}";

            var mVto = RegexVencimiento().Match(texto);
            if (mVto.Success && DateTime.TryParseExact(
                    $"{mVto.Groups["fecha"].Value} {(mVto.Groups["hora"].Success ? mVto.Groups["hora"].Value : "23:59")}",
                    "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var vto))
                datos.Vencimiento = vto;

            // --- A: Intervinientes ---
            foreach (Match m in RegexInterviniente().Matches(Seccion(texto, "INTERVINIENTES", "GRANO")))
            {
                var etiqueta = m.Groups["etiqueta"].Value.ToLowerInvariant();
                var cuit = DigitosOcr(m.Groups["cuit"].Value);
                var nombre = m.Groups["nombre"].Value.Trim();

                if (etiqueta.Contains("titular") && datos.CuitProveedor == null && cuit.Length == 11)
                {
                    datos.CuitProveedor = Cuit.Formatear(cuit);
                    datos.RazonSocialProveedor = nombre;
                    if (!Cuit.EsValido(cuit))
                        datos.Advertencias.Add($"⚠ El CUIT del titular leído ({datos.CuitProveedor} - {nombre}) tiene el dígito verificador inválido: puede ser un error de lectura. Verifíquelo contra la CPE.");
                }
                else if (etiqueta.StartsWith("ch") && etiqueta.Length <= 8) // "Chofer" (el OCR a veces lee "Choter")
                {
                    datos.NombreChofer = nombre;
                }
            }

            if (datos.CuitProveedor == null && texto.Contains("Titular", StringComparison.OrdinalIgnoreCase))
                datos.Advertencias.Add("No se pudo leer el CUIT del titular de la CPE; elija el proveedor manualmente.");

            // --- B: Grano y pesos de carga (solo la sección B: la G repite las etiquetas para la descarga) ---
            var seccionB = Seccion(texto, "GRANO", "PROCEDENCIA");
            var mGrano = RegexGrano().Match(seccionB);
            if (mGrano.Success) datos.Producto = mGrano.Groups["grano"].Value.Trim();

            double? bruto = null, tara = null, neto = null;
            foreach (Match m in RegexPeso().Matches(seccionB))
            {
                if (!double.TryParse(m.Groups["valor"].Value.Replace(".", "").Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out var kg))
                    continue;
                switch (m.Groups["tipo"].Value.ToLowerInvariant())
                {
                    case "bruto": bruto ??= kg; break;
                    case "tara": tara ??= kg; break;
                    case "neto": neto ??= kg; break;
                }
            }
            if (neto.HasValue && bruto.HasValue && tara.HasValue && Math.Abs(bruto.Value - tara.Value - neto.Value) > 1)
                datos.Advertencias.Add($"Los pesos de carga leídos no cierran (bruto {bruto:N0} - tara {tara:N0} ≠ neto {neto:N0}); revise los kilos declarados.");
            datos.KilosDeclarados = neto ?? (bruto.HasValue && tara.HasValue ? bruto - tara : null);

            // --- D: Destino ---
            var mPlanta = RegexPlanta().Match(Seccion(texto, "DESTINO DE LA", "TRANSPORTE"));
            if (mPlanta.Success) datos.PlantaDestino = int.Parse(mPlanta.Groups["planta"].Value);

            // --- E: Transporte ---
            var mDom = RegexDominios().Match(Seccion(texto, "TRANSPORTE", "CONTINGENCIAS"));
            if (mDom.Success)
            {
                var patentes = mDom.Groups["valor"].Value
                    .Split(new[] { '-', '–', ',', '/', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(Patente.CorregirOcr)
                    .Where(p => p != null)
                    .Distinct()
                    .ToList();
                if (patentes.Count > 0) datos.PatenteCamion = patentes[0];
                if (patentes.Count > 0 && origen == "OCR")
                    datos.Advertencias.Add($"Patente leída por OCR ({string.Join(" / ", patentes.Take(2))}): confírmela mirando el camión o la CPE (el OCR puede confundir 3/5/8, O/0).");
                if (patentes.Count > 1) datos.PatenteAcoplado = patentes[1];
                if (patentes.Count == 0) datos.Advertencias.Add($"No se pudo leer la patente ({mDom.Groups["valor"].Value.Trim()}).");
            }

            if (!datos.TieneDatos)
                datos.Advertencias.Add("No se reconocieron datos de una CPE en el documento. Complete el formulario manualmente.");
            else if (datos.NumeroCPE == null)
                datos.Advertencias.Add("No se encontró el N° de CPE; cárguelo desde el papel.");

            return datos;
        }

        /// <summary>Corrige letras que el OCR confunde con números y deja solo dígitos.</summary>
        private static string DigitosOcr(string valor) =>
            new string(valor.Select(c => c switch { 'O' or 'o' => '0', 'I' or 'l' or '|' => '1', _ => c })
                .Where(char.IsDigit).ToArray());

        /// <summary>Texto entre dos títulos de sección (si no los encuentra, devuelve el texto completo).</summary>
        private static string Seccion(string texto, string desde, string hasta)
        {
            var i = texto.IndexOf(desde, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return texto;
            var j = texto.IndexOf(hasta, i + desde.Length, StringComparison.OrdinalIgnoreCase);
            return j < 0 ? texto[i..] : texto[i..j];
        }
    }
}
