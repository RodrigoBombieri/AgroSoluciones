namespace ControlBalanza.UnitTests.Fixtures
{
    internal static class Fixture
    {
        public static string Leer(string nombre) =>
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", nombre));

        /// <summary>
        /// Texto de una CPE tal como lo arma pdf.js a partir del PDF original de ARCA (un renglón por línea).
        /// Datos ficticios con el formato real del documento.
        /// </summary>
        public const string CpeTextoPdf =
            "Carta de Porte Electrónica Automotor Fecha: 31/10/2026\n" +
            "N° CPE: 00004-00012871\n" +
            "CTG: 10100018841 Vencimiento:\n" +
            "03/11/2026 18:00\n" +
            "A - INTERVINIENTES\n" +
            "Titular Carta de Porte: 30709123455 - AGROPECUARIA LOS TALAS S.A.\n" +
            "Remitente Comercial Productor:\n" +
            "Destinatario: 30711112224 - ACOPIO DEMO S.A.\n" +
            "Empresa Transportista: 30654321092 - TRANSPORTE EL CEIBO S.R.L.\n" +
            "Chofer : 20288776650 - RAMON ACOSTA\n" +
            "B - GRANO / ESPECIE\n" +
            "Grano / especie: Soja Tipo: Soja Peso Bruto (kg): 44.760\n" +
            "Peso Tara (kg): 14.910\n" +
            "Peso Neto (kg): 29.850\n" +
            "C - PROCEDENCIA\n" +
            "Es un campo: Si N° Planta (RUCA):\n" +
            "D - DESTINO DE LA MERCADERÍA\n" +
            "Es un campo: No N° Planta (RUCA): 1782 Dirección: Dirección genérica\n" +
            "E - DATOS DEL TRANSPORTE\n" +
            "Dominios: AF482KD - AF113PL\n" +
            "F - CONTINGENCIAS\n" +
            "G - DESCARGA\n" +
            "Fecha Arribo: Peso Bruto (kg): 99999\n" +
            "Peso Tara (kg): 11111\n";
    }
}
