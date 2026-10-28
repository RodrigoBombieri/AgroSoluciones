namespace ControlBalanza.Seguridad
{
    /// <summary>
    /// Roles del sistema. Cada etapa del camión la opera un rol distinto; Admin puede todo.
    /// </summary>
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string BalanzaEntrada = "BalanzaEntrada";
        public const string Playon = "Playon";
        public const string BalanzaSalida = "BalanzaSalida";

        // Combinaciones para [Authorize(Roles = ...)]
        public const string AdminOEntrada = Admin + "," + BalanzaEntrada;
        public const string AdminOPlayon = Admin + "," + Playon;
        public const string AdminOSalida = Admin + "," + BalanzaSalida;

        public static readonly string[] Todos = { Admin, BalanzaEntrada, Playon, BalanzaSalida };

        public static string Descripcion(string rol) => rol switch
        {
            Admin => "Administrador",
            BalanzaEntrada => "Balanza de entrada",
            Playon => "Playón / Descarga",
            BalanzaSalida => "Balanza de salida",
            _ => rol
        };
    }
}
