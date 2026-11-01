using ControlBalanza.Services.Cpe;

namespace ControlBalanza.UnitTests.Cpe
{
    public class CpeQrParserTests
    {
        [Fact]
        public void QrReal_SoloTraeElCtg()
        {
            // Contenido exacto del QR de una CPE real de ARCA
            var datos = CpeQrParser.Interpretar("10200001245");

            Assert.Equal("10200001245", datos.Ctg);
            Assert.Null(datos.NumeroCPE);
            Assert.Null(datos.CuitProveedor);
            Assert.Equal("QR", datos.Origen);
            Assert.Contains(datos.Advertencias, a => a.Contains("solo contiene el CTG"));
        }

        [Fact]
        public void QrNumericoQueNoEsCtg_Avisa()
        {
            var datos = CpeQrParser.Interpretar("12345");
            Assert.Null(datos.Ctg);
            Assert.Contains(datos.Advertencias, a => a.Contains("no tiene el formato de un CTG"));
        }

        [Fact]
        public void QrJson_MapeaLasClavesConocidas()
        {
            var datos = CpeQrParser.Interpretar(
                "{\"nroCPE\":\"10001-00012345\",\"patente\":\"ab123cd\",\"cuitRemitente\":\"30712345671\",\"grano\":\"Soja\",\"kgNetos\":30500}");

            Assert.Equal("10001-00012345", datos.NumeroCPE);
            Assert.Equal("AB123CD", datos.PatenteCamion);
            Assert.Equal("30-71234567-1", datos.CuitProveedor);
            Assert.Equal("Soja", datos.Producto);
            Assert.Equal(30500, datos.KilosDeclarados);
        }

        [Fact]
        public void QrUrlConParametroBase64_Decodifica()
        {
            var json = "{\"ctg\":\"10123456789\",\"dominio\":\"AAA111\"}";
            var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json)).TrimEnd('=');

            var datos = CpeQrParser.Interpretar("https://ejemplo.gob.ar/cpe/qr?p=" + Uri.EscapeDataString(b64));

            Assert.Equal("10123456789", datos.Ctg);
            Assert.Equal("AAA111", datos.PatenteCamion);
        }

        [Fact]
        public void QrTextoLibre_BuscaPorPatrones()
        {
            var datos = CpeQrParser.Interpretar("CPE 10001-00099999 CAMION AC456FG ACOPLADO ZZZ999 CUIT 20-12345678-6");

            Assert.Equal("10001-00099999", datos.NumeroCPE);
            Assert.Equal("AC456FG", datos.PatenteCamion);
            Assert.Equal("ZZZ999", datos.PatenteAcoplado);
            Assert.Equal("20-12345678-6", datos.CuitProveedor);
            Assert.Null(datos.Ctg); // el CUIT no se confunde con un CTG
        }

        [Theory]
        [InlineData("")]
        [InlineData("hola mundo")]
        public void QrSinDatos_Avisa(string contenido)
        {
            var datos = CpeQrParser.Interpretar(contenido);
            Assert.False(datos.TieneDatos);
            Assert.Single(datos.Advertencias);
        }
    }
}
