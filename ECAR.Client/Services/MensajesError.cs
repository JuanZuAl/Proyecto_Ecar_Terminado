using System.Text.Json;
using Microsoft.JSInterop;

namespace ECAR.Client.Services;

/// <summary>
/// Texto en español para una excepción que llega a la pantalla. Los mensajes de .NET y del navegador vienen en inglés
/// (por ejemplo «TypeError: Failed to fetch» o «A task was canceled.»), así que nunca se muestran tal cual: la pantalla
/// recibe una explicación en español y el detalle técnico queda en la consola del navegador.
/// </summary>
public static class MensajesError
{
    public const string Inesperado =
        "Ocurrió un error inesperado. Intente de nuevo y, si se repite, avise a Soporte T.I.";

    public const string TiempoAgotado = "El servidor tardó demasiado en responder. Intente de nuevo.";

    public const string RespuestaIlegible =
        "No se pudo leer la respuesta del servidor. Intente de nuevo y, si se repite, avise a Soporte T.I.";

    public static string Texto(Exception ex)
    {
        Console.WriteLine($"Error: {ex}");
        return ex switch
        {
            HttpRequestException => LectorRespuestaApi.MensajeSinConexion,
            OperationCanceledException => TiempoAgotado,
            JsonException => RespuestaIlegible,
            _ => Inesperado
        };
    }

    /// <summary>Por qué no se abrió la cámara del lector de QR, según el error que dio el navegador.</summary>
    public static string Camara(JSException ex)
    {
        Console.WriteLine($"Cámara: {ex}");
        var causa = ex.Message switch
        {
            var m when Contiene(m, "NotAllowedError", "Permission") => "El navegador no tiene permiso para usarla.",
            var m when Contiene(m, "NotFoundError", "OverconstrainedError", "device not found") =>
                "No se encontró una cámara en este equipo.",
            var m when Contiene(m, "NotReadableError", "Could not start video") => "Otra aplicación la está usando.",
            var m when Contiene(m, "not supported", "secure context", "NotSupportedError") =>
                "El navegador solo la permite si la aplicación se abre por HTTPS.",
            var m when Contiene(m, "No se cargó el lector") => "No se cargó el lector de códigos QR; recargue la página.",
            _ => "Revise que el navegador tenga permiso para usarla."
        };
        return $"No se pudo abrir la cámara. {causa} También puede escanear la etiqueta con la cámara del teléfono.";
    }

    private static bool Contiene(string texto, params string[] partes) =>
        partes.Any(p => texto.Contains(p, StringComparison.OrdinalIgnoreCase));
}
