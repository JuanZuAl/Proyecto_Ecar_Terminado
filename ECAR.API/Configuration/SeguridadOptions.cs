using ECAR.Shared.DTOs;

namespace ECAR.API.Configuration;

public class SeguridadOptions
{
    public const string SectionName = "Seguridad";

    public int LongitudMinimaPassword { get; set; } = 10;
    public bool RequiereLetrasYNumeros { get; set; } = true;

    // Sin caducidad ni historial de contraseñas desde el 03/11/2026: el cambio periódico y el restablecimiento
    // los hará una API externa. Siguen el bloqueo por intentos fallidos y el desbloqueo por el Administrador.
    public int MaxIntentosFallidos { get; set; } = 5;
    public int MinutosBloqueo { get; set; } = 30;
    public int MinutosInactividad { get; set; } = 15;

    public PoliticaSeguridadDto ToDto() => new()
    {
        LongitudMinimaPassword = LongitudMinimaPassword,
        RequiereLetrasYNumeros = RequiereLetrasYNumeros,
        MaxIntentosFallidos = MaxIntentosFallidos,
        MinutosBloqueo = MinutosBloqueo,
        MinutosInactividad = MinutosInactividad
    };
}

public class FirmaOptions
{
    public const string SectionName = "Firma";

    public const int LongitudMinimaClave = 32;

    public string ClaveHmac { get; set; } = string.Empty;
}
