namespace ControlBalanza.Services
{
    /// <summary>
    /// Parámetros operativos de la balanza (sección "Balanza" de appsettings.json).
    /// </summary>
    public class BalanzaOptions
    {
        public const string Seccion = "Balanza";

        /// <summary>
        /// Diferencia máxima tolerada (en %) entre el peso neto real y los kilos declarados en la CPE.
        /// Por encima de este valor el pesaje se marca con alerta.
        /// </summary>
        public double ToleranciaDiferenciaPorcentaje { get; set; } = 0.5;

        /// <summary>
        /// N° de planta (RUCA) de esta planta en ARCA. Si se configura, al leer una CPE se avisa
        /// cuando su destino es otra planta (camión equivocado). Null = no se verifica.
        /// </summary>
        public int? NumeroPlantaRuca { get; set; }

        /// <summary>Peso bruto máximo aceptado (kg). Sirve para detectar errores de tipeo.</summary>
        public double PesoMaximoKg { get; set; } = 75000;
    }
}
