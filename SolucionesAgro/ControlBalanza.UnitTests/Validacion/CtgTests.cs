using ControlBalanza.Validacion;

namespace ControlBalanza.UnitTests.Validacion
{
    public class CtgTests
    {
        [Theory]
        [InlineData("10100014235")]  // automotor, tal como sale impreso y en el QR
        [InlineData("20200004110")]  // ferroviaria
        [InlineData("010100014235")] // formato de 12 dígitos del web service (con 0 inicial)
        public void EsValido_CtgCorrectos(string ctg) => Assert.True(Ctg.EsValido(ctg));

        [Theory]
        [InlineData("30712345671")] // un CUIT no es un CTG
        [InlineData("123")]
        [InlineData("")]
        [InlineData(null)]
        public void EsValido_RechazaInvalidos(string? ctg) => Assert.False(Ctg.EsValido(ctg));

        [Fact]
        public void Normalizar_QuitaElCeroInicialDelFormatoDe12Digitos() =>
            Assert.Equal("10100014235", Ctg.Normalizar("010100014235"));
    }
}
