using System.ClientModel;
using System.Globalization;
using System.Text.Json;
using Azure.AI.OpenAI;
using ControlBalanza.Validacion;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace ControlBalanza.Services.Cpe
{
    /// <summary>Configuración de Azure OpenAI (sección "AzureOpenAI" de appsettings / user-secrets).</summary>
    public class AzureOpenAIOptions
    {
        public const string Seccion = "AzureOpenAI";

        /// <summary>Ej: https://mi-recurso.openai.azure.com/</summary>
        public string? Endpoint { get; set; }
        public string? ApiKey { get; set; }
        /// <summary>Nombre del deployment de un modelo con visión (ej: gpt-4o-mini).</summary>
        public string? Deployment { get; set; }

        public bool Configurado =>
            !string.IsNullOrWhiteSpace(Endpoint) && !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Deployment);
    }

    public interface ICpeExtractorIA
    {
        bool Disponible { get; }
        Task<CpeDatos> ExtraerAsync(Stream imagen, string contentType, CancellationToken ct = default);
    }

    /// <summary>
    /// Lectura de la CPE desde una foto: envía la imagen a un modelo con visión y pide los datos en JSON.
    /// Es el plan B cuando el QR no se puede leer.
    /// </summary>
    public class CpeExtractorIA : ICpeExtractorIA
    {
        private const string Instrucciones = """
            Sos un asistente que lee Cartas de Porte Electrónicas (CPE) de granos de Argentina.
            Extraé los datos de la imagen y respondé SOLO con un objeto JSON con estas claves
            (usá null si un dato no se ve con claridad, no inventes):
            {
              "numeroCPE": "número de la carta de porte, tal cual figura",
              "ctg": "código de trazabilidad de granos (CTG)",
              "patenteCamion": "dominio del camión / chasis",
              "patenteAcoplado": "dominio del acoplado",
              "nombreChofer": "nombre y apellido del chofer",
              "cuitProveedor": "CUIT del titular / remitente comercial productor",
              "razonSocialProveedor": "razón social del titular / remitente",
              "producto": "grano o especie (ej: Soja, Maíz, Trigo)",
              "kilosDeclarados": número en kilogramos (peso neto o bruto declarado), sin separadores de miles
            }
            """;

        private readonly AzureOpenAIOptions _opciones;
        private readonly ILogger<CpeExtractorIA> _logger;

        public CpeExtractorIA(IOptions<AzureOpenAIOptions> opciones, ILogger<CpeExtractorIA> logger)
        {
            _opciones = opciones.Value;
            _logger = logger;
        }

        public bool Disponible => _opciones.Configurado;

        public async Task<CpeDatos> ExtraerAsync(Stream imagen, string contentType, CancellationToken ct = default)
        {
            var datos = new CpeDatos { Origen = "IA" };
            if (!Disponible)
            {
                datos.Advertencias.Add("La lectura con IA no está configurada (falta la sección AzureOpenAI en appsettings).");
                return datos;
            }

            var cliente = new AzureOpenAIClient(new Uri(_opciones.Endpoint!), new ApiKeyCredential(_opciones.ApiKey!));
            var chat = cliente.GetChatClient(_opciones.Deployment!);

            var bytes = await BinaryData.FromStreamAsync(imagen, ct);
            var mensajes = new List<ChatMessage>
            {
                new SystemChatMessage(Instrucciones),
                new UserChatMessage(
                    ChatMessageContentPart.CreateTextPart("Extraé los datos de esta carta de porte."),
                    ChatMessageContentPart.CreateImagePart(bytes, contentType, ChatImageDetailLevel.High))
            };
            var opciones = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
                Temperature = 0
            };

            try
            {
                ChatCompletion respuesta = await chat.CompleteChatAsync(mensajes, opciones, ct);
                var json = respuesta.Content.Count > 0 ? respuesta.Content[0].Text : null;
                Mapear(json, datos);
            }
            catch (Exception ex) when (ex is ClientResultException or HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Error consultando Azure OpenAI para leer la CPE");
                datos.Advertencias.Add("No se pudo consultar el servicio de IA (¿sin conexión?). Complete los datos manualmente.");
            }

            if (!datos.TieneDatos && datos.Advertencias.Count == 0)
                datos.Advertencias.Add("La IA no pudo leer datos de la imagen. Pruebe con una foto más nítida.");

            return datos;
        }

        /// <summary>Convierte la respuesta JSON del modelo en CpeDatos. Público para poder probarlo sin IA.</summary>
        public static void Mapear(string? json, CpeDatos datos)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch (JsonException)
            {
                datos.Advertencias.Add("La respuesta de la IA no tuvo un formato válido.");
                return;
            }

            using (doc)
            {
                var r = doc.RootElement;
                string? Texto(string clave) =>
                    r.TryGetProperty(clave, out var v) && v.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(v.GetString())
                        ? v.GetString()!.Trim()
                        : null;

                datos.NumeroCPE = Texto("numeroCPE");
                datos.Ctg = Texto("ctg");
                datos.PatenteCamion = Texto("patenteCamion") is { } pc ? Patente.Normalizar(pc) : null;
                datos.PatenteAcoplado = Texto("patenteAcoplado") is { } pa ? Patente.Normalizar(pa) : null;
                datos.NombreChofer = Texto("nombreChofer");
                datos.CuitProveedor = Texto("cuitProveedor") is { } c ? Cuit.Formatear(c) : null;
                datos.RazonSocialProveedor = Texto("razonSocialProveedor");
                datos.Producto = Texto("producto");

                if (r.TryGetProperty("kilosDeclarados", out var k))
                {
                    if (k.ValueKind == JsonValueKind.Number && k.TryGetDouble(out var kg)) datos.KilosDeclarados = kg;
                    else if (k.ValueKind == JsonValueKind.String &&
                             double.TryParse(k.GetString()?.Replace(".", "").Replace(",", "."), NumberStyles.Float, CultureInfo.InvariantCulture, out var kgTexto))
                        datos.KilosDeclarados = kgTexto;
                }
            }

            if (datos.CuitProveedor != null && !Cuit.EsValido(datos.CuitProveedor))
                datos.Advertencias.Add($"El CUIT leído ({datos.CuitProveedor}) no es válido; revíselo.");
            if (datos.PatenteCamion != null && !Patente.EsValida(datos.PatenteCamion))
                datos.Advertencias.Add($"La patente leída ({datos.PatenteCamion}) no tiene un formato válido; revísela.");
            datos.Advertencias.Add("Datos leídos por IA: verifíquelos contra la CPE antes de registrar.");
        }
    }
}
