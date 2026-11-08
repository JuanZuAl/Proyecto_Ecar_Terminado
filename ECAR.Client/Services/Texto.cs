using ECAR.Shared;

namespace ECAR.Client.Services;

/// <summary>
/// Textos con cantidad: elige singular o plural en vez de escribir «(s)» o «(es)». Usa <see cref="Plurales"/>, el mismo
/// de los mensajes del API.
/// </summary>
public static class Texto
{
    /// <summary>«1 registro», «0 registros», «2 registros».</summary>
    public static string Cantidad(int cantidad, string singular, string plural) =>
        Plurales.Cantidad(cantidad, singular, plural);

    /// <summary>Solo la palabra: «registro» o «registros».</summary>
    public static string Palabra(int cantidad, string singular, string plural) =>
        Plurales.Palabra(cantidad, singular, plural);
}
