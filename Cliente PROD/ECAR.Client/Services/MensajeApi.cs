using ECAR.Shared.Responses;

namespace ECAR.Client.Services;

/// <summary>
/// El mensaje que se muestra cuando el API rechaza una petición (M-13). Con datos inválidos el API
/// responde un mensaje genérico en <c>message</c> («La solicitud contiene datos inválidos») y el
/// detalle de qué corregir en <c>errors</c>; mostrar solo <c>message</c> deja al usuario sin saber qué falló.
/// </summary>
public static class MensajeApi
{
    /// <summary>
    /// Un solo error: es el texto (ya dice qué corregir). Varios: el mensaje del API y la lista separada por «·».
    /// Sin detalle: el mensaje del API, o <paramref name="porDefecto"/> si no hubo respuesta.
    /// </summary>
    public static string Detalle<T>(ApiResponse<T>? respuesta, string porDefecto)
    {
        if (respuesta is null)
        {
            return porDefecto;
        }

        var mensaje = string.IsNullOrWhiteSpace(respuesta.Message) ? porDefecto : respuesta.Message;
        var errores = (respuesta.Errors ?? []).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList();
        return errores.Count switch
        {
            0 => mensaje,
            1 => errores[0],
            _ => $"{mensaje}: {string.Join(" · ", errores)}"
        };
    }
}
