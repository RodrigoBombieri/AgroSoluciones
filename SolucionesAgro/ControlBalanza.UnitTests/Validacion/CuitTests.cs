using ControlBalanza.Validacion;

namespace ControlBalanza.UnitTests.Validacion
{
    public class CuitTests
    {
        [Theory]
        [InlineData("20-12345678-6")]
        [InlineData("20123456786")]
        [InlineData("30-71234567-1")]
        [InlineData(" 30 70912345 5 ")]
        public void EsValido_CuitConDigitoVerificadorCorrecto(string cuit) => Assert.True(Cuit.EsValido(cuit));

        [Theory]
        [InlineData("20-12345678-9")] // verificador incorrecto
        [InlineData("33254785743")]   // CUIT de homologación de ARCA, verificador inválido
        [InlineData("27876543210")]   // el cálculo da 10: no existe
        [InlineData("2012345678")]    // 10 dígitos
        [InlineData("")]
        [InlineData(null)]
        public void EsValido_RechazaCuitInvalidos(string? cuit) => Assert.False(Cuit.EsValido(cuit));

        [Fact]
        public void Formatear_AgregaGuiones() => Assert.Equal("30-71234567-1", Cuit.Formatear("30712345671"));

        [Fact]
        public void Formatear_SiNoTiene11Digitos_DevuelveElTextoOriginal() => Assert.Equal("123", Cuit.Formatear(" 123 "));

        [Fact]
        public void Atributo_AceptaVacioYRechazaInvalido()
        {
            var atributo = new CuitAttribute();
            Assert.True(atributo.IsValid(""));
            Assert.True(atributo.IsValid(null));
            Assert.True(atributo.IsValid("20-12345678-6"));
            Assert.False(atributo.IsValid("20-12345678-9"));
        }
    }
}
