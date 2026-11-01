using ControlBalanza.Validacion;

namespace ControlBalanza.UnitTests.Validacion
{
    public class PatenteTests
    {
        [Theory]
        [InlineData("AB123CD")]   // Mercosur
        [InlineData("ABC123")]    // formato anterior
        [InlineData("abc 123")]   // se normaliza
        [InlineData("ab-123-cd")]
        public void EsValida_FormatosArgentinos(string patente) => Assert.True(Patente.EsValida(patente));

        [Theory]
        [InlineData("A1B2C3")]
        [InlineData("AB1234")]
        [InlineData("ABCD123")]
        [InlineData("")]
        public void EsValida_RechazaFormatosIncorrectos(string patente) => Assert.False(Patente.EsValida(patente));

        [Fact]
        public void Normalizar_MayusculasSinSeparadores() => Assert.Equal("AB123CD", Patente.Normalizar(" ab-123 cd "));

        [Theory]
        [InlineData("HOMOOE", "HOM003")]  // lectura real del OCR: O por 0 y E por 3
        [InlineData("HOMOO3", "HOM003")]
        [InlineData("HOMO003", "HOM003")] // carácter duplicado
        [InlineData("HOMO03", "HOM003")]
        [InlineData("AB12OCD", "AB120CD")]
        [InlineData("A8123CD", "AB123CD")] // 8 en posición de letra
        [InlineData("AF482KD", "AF482KD")] // ya válida: no se toca
        public void CorregirOcr_CorrigeConfusionesTipicas(string leida, string esperada) =>
            Assert.Equal(esperada, Patente.CorregirOcr(leida));

        [Theory]
        [InlineData("XYZ")]
        [InlineData("ABCDEFGH")]
        [InlineData("")]
        public void CorregirOcr_SiNoHayFormatoPosible_DevuelveNull(string leida) => Assert.Null(Patente.CorregirOcr(leida));

        [Fact]
        public void CorregirOcr_NoQuitaCaracteresAlAzar()
        {
            // Sacar la "H" daría "OMO003" (formato válido) pero no es un duplicado: no debe elegirse
            Assert.Equal("HOM003", Patente.CorregirOcr("HOMO003"));
        }
    }
}
