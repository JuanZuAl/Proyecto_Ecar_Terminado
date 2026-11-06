namespace ECAR.Client.Services;

/// <summary>Textos con cantidad: elige singular o plural en vez de escribir «(s)» o «(es)».</summary>
public static class Texto
{
    /// <summary>«1 registro», «0 registros», «2 registros».</summary>
    public static string Cantidad(int cantidad, string singular, string plural) =>
        $"{cantidad} {(cantidad == 1 ? singular : plural)}";

    /// <summary>Solo la palabra: «registro» o «registros».</summary>
    public static string Palabra(int cantidad, string singular, string plural) =>
        cantidad == 1 ? singular : plural;
}
