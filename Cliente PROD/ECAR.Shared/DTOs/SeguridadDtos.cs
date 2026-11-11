namespace ECAR.Shared.DTOs;

/// <summary>
/// GET /api/auth/politica (endpoint 24, anónimo): las reglas de la contraseña del alta y los minutos de
/// inactividad. Desde el 03/11/2026 no hay caducidad ni historial (el cambio lo hará una API externa). Los valores por defecto son los del plan
/// (§3.2): el cliente los usa mientras el API no publique el endpoint.
/// </summary>
public class PoliticaSeguridadDto
{
    public int LongitudMinimaPassword { get; set; } = 10;
    public bool RequiereLetrasYNumeros { get; set; } = true;
    public int MaxIntentosFallidos { get; set; } = 5;
    public int MinutosBloqueo { get; set; } = 30;
    public int MinutosInactividad { get; set; } = 15;

    /// <summary>ECARAuthentication:Mode (ModosAutenticacion). Con SgiIdentity las contraseñas y los roles vienen de SGI.Identity.</summary>
    public string ModoAutenticacion { get; set; } = ModosAutenticacion.Local;

    /// <summary>
    /// En modo SgiIdentity, si las cuentas locales (con contraseña guardada en ECAR, como admin@ecar.com) pueden
    /// entrar. En los demás modos, siempre true.
    /// </summary>
    public bool CuentasLocales { get; set; } = true;
}
