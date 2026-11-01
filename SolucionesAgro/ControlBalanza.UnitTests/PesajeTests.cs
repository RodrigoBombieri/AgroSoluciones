using ControlBalanza.Helpers;
using ControlBalanza.Models;

namespace ControlBalanza.UnitTests
{
    public class PesajeTests
    {
        private static Pesaje ConNeto(double declarado, double neto)
        {
            var p = new Pesaje { KilosDeclarados = declarado };
            // PesoNeto lo calcula SQL Server (setter privado): se simula para el test
            typeof(Pesaje).GetProperty(nameof(Pesaje.PesoNeto))!.SetValue(p, neto);
            return p;
        }

        [Fact]
        public void Diferencia_NetoMenosDeclarado()
        {
            var p = ConNeto(30000, 29700);
            Assert.Equal(-300, p.DiferenciaKg);
            Assert.Equal(-1.0, p.DiferenciaPorcentaje!.Value, precision: 6);
        }

        [Fact]
        public void Diferencia_SinNeto_EsNull()
        {
            var p = new Pesaje { KilosDeclarados = 30000 };
            Assert.Null(p.DiferenciaKg);
            Assert.Null(p.DiferenciaPorcentaje);
        }

        [Fact]
        public void DiferenciaPorcentaje_SinDeclarados_EsNull() => Assert.Null(ConNeto(0, 100).DiferenciaPorcentaje);

        [Theory]
        [InlineData(EstadoPesaje.EnEspera, "En espera")]
        [InlineData(EstadoPesaje.ListoSalida, "Listo para salir")]
        public void Estado_TextoParaMostrar(EstadoPesaje estado, string texto) => Assert.Equal(texto, VistaHelpers.Estado(estado));

        [Theory]
        [InlineData(45, "45 min")]
        [InlineData(95, "1 h 35 min")]
        public void Duracion(double minutos, string texto) => Assert.Equal(texto, VistaHelpers.Duracion(minutos));

        [Theory]
        [InlineData(45, "45m")]
        [InlineData(95, "1h 35m")]
        [InlineData(125, "2h 05m")]
        public void DuracionCorta(double minutos, string texto) => Assert.Equal(texto, VistaHelpers.DuracionCorta(minutos));
    }
}
