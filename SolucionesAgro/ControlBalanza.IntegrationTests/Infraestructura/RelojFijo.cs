namespace ControlBalanza.IntegrationTests.Infraestructura
{
    /// <summary>Reloj controlable para los tests (la app usa TimeProvider en lugar de DateTime.Now).</summary>
    public class RelojFijo : TimeProvider
    {
        private DateTimeOffset _ahora;

        public RelojFijo(DateTime ahora) => _ahora = new DateTimeOffset(ahora, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _ahora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public void Avanzar(TimeSpan tiempo) => _ahora = _ahora.Add(tiempo);
    }
}
