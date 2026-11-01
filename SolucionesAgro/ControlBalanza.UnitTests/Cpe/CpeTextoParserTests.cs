using ControlBalanza.Services.Cpe;
using ControlBalanza.UnitTests.Fixtures;

namespace ControlBalanza.UnitTests.Cpe
{
    public class CpeTextoParserTests
    {
        [Fact]
        public void TextoDePdf_ExtraeTodosLosDatos()
        {
            var d = CpeTextoParser.Interpretar(Fixture.CpeTextoPdf, "PDF");

            Assert.Equal("PDF", d.Origen);
            Assert.Equal("10100018841", d.Ctg);
            Assert.Equal("00004-00012871", d.NumeroCPE);
            Assert.Equal("30-70912345-5", d.CuitProveedor);
            Assert.Equal("AGROPECUARIA LOS TALAS S.A.", d.RazonSocialProveedor);
            Assert.Equal("RAMON ACOSTA", d.NombreChofer);
            Assert.Equal("Soja", d.Producto);
            Assert.Equal("AF482KD", d.PatenteCamion);
            Assert.Equal("AF113PL", d.PatenteAcoplado);
            Assert.Equal(1782, d.PlantaDestino);
            Assert.Equal(new DateTime(2026, 11, 3, 18, 0, 0), d.Vencimiento);
            Assert.Empty(d.Advertencias);
        }

        [Fact]
        public void KilosDeclarados_SonElNetoDeCarga_NoLosDeLaSeccionDescarga()
        {
            var d = CpeTextoParser.Interpretar(Fixture.CpeTextoPdf, "PDF");
            Assert.Equal(29850, d.KilosDeclarados); // 44.760 - 14.910; la sección G trae 99999 / 11111
        }

        [Fact]
        public void SinNeto_CalculaBrutoMenosTara()
        {
            var texto = Fixture.CpeTextoPdf.Replace("Peso Neto (kg): 29.850\n", "");
            Assert.Equal(29850, CpeTextoParser.Interpretar(texto, "PDF").KilosDeclarados);
        }

        [Fact]
        public void PesosQueNoCierran_Avisa()
        {
            var texto = Fixture.CpeTextoPdf.Replace("Peso Neto (kg): 29.850", "Peso Neto (kg): 30.850");
            var d = CpeTextoParser.Interpretar(texto, "PDF");
            Assert.Contains(d.Advertencias, a => a.Contains("no cierran"));
        }

        [Fact]
        public void OcrDeCpeReal_ImagenAmpliada_LeeLosDatosPrincipales()
        {
            // Texto real devuelto por Tesseract para una CPE de ARCA (datos de homologación)
            var d = CpeTextoParser.Interpretar(Fixture.Leer("cpe-ocr-imagen-ampliada.txt"), "OCR");

            Assert.Equal("10100014235", d.Ctg);
            Assert.Equal("00001-00001665", d.NumeroCPE);
            Assert.Equal("Soja", d.Producto);
            Assert.Equal(100, d.KilosDeclarados);
            Assert.Equal("HOM003", d.PatenteCamion);
            Assert.Equal(1782, d.PlantaDestino);
            Assert.Equal("AgroRuralSistemas", d.RazonSocialProveedor);
        }

        [Fact]
        public void OcrDeCpeReal_CuitConVerificadorInvalido_SeMuestraPeroSeAdvierte()
        {
            var d = CpeTextoParser.Interpretar(Fixture.Leer("cpe-ocr-imagen-ampliada.txt"), "OCR");

            Assert.Equal("33-25478574-3", d.CuitProveedor);
            Assert.Contains(d.Advertencias, a => a.Contains("dígito verificador"));
        }

        [Fact]
        public void OcrDeCpeReal_SiempreAvisaRevisarLaPatente()
        {
            var d = CpeTextoParser.Interpretar(Fixture.Leer("cpe-ocr-imagen-ampliada.txt"), "OCR");
            Assert.Contains(d.Advertencias, a => a.StartsWith("Patente leída por OCR"));
        }

        [Fact]
        public void OcrRuidoso_NoInventaElCuit_YCorrigeLaPatente()
        {
            // Misma CPE sin preprocesar la imagen: el OCR lee "Ja254 7857/43" en lugar del CUIT
            var d = CpeTextoParser.Interpretar(Fixture.Leer("cpe-ocr-imagen-sin-procesar.txt"), "OCR");

            Assert.Equal("10100014235", d.Ctg);
            Assert.Null(d.CuitProveedor);
            Assert.Contains(d.Advertencias, a => a.Contains("titular"));
            Assert.Equal("HOM003", d.PatenteCamion); // "HOMOOE" corregido
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void TextoVacio_Avisa(string? texto)
        {
            var d = CpeTextoParser.Interpretar(texto, "OCR");
            Assert.False(d.TieneDatos);
            Assert.Contains(d.Advertencias, a => a.Contains("No se pudo leer texto"));
        }

        [Fact]
        public void TextoQueNoEsUnaCpe_Avisa()
        {
            var d = CpeTextoParser.Interpretar("Factura B 0001-00001234 Total $ 1.500", "OCR");
            Assert.False(d.TieneDatos);
            Assert.Contains(d.Advertencias, a => a.Contains("No se reconocieron datos"));
        }
    }
}
