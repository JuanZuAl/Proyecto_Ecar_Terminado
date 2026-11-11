namespace ECAR.Shared;

/// <summary>
/// Textos con cantidad para el API y el cliente: elige singular o plural en vez de escribir «(s)» o «(es)» (B-01; desde
/// el 08/11/2026 también en los mensajes del API y el asunto del correo).
/// </summary>
public static class Plurales
{
    /// <summary>«1 registro», «0 registros», «2 registros».</summary>
    public static string Cantidad(int cantidad, string singular, string plural) =>
        $"{cantidad} {Palabra(cantidad, singular, plural)}";

    /// <summary>Solo la palabra: «registro» o «registros».</summary>
    public static string Palabra(int cantidad, string singular, string plural) =>
        cantidad == 1 ? singular : plural;
}
