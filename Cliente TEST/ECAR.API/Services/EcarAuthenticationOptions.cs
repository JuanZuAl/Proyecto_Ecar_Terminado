namespace ECAR.API.Services;

public sealed class EcarAuthenticationOptions
{
    public const string SectionName = "ECARAuthentication";

    /// <summary>
    /// Valores admitidos: Local, ActiveDirectory, Hybrid y SgiIdentity.
    /// ActiveDirectory e Hybrid requieren un IActiveDirectoryAuthService configurado; SgiIdentity, la sección
    /// SgiIdentity (URL, ClientId y ClientSecret del servicio corporativo de autenticación).
    /// </summary>
    public string Mode { get; set; } = nameof(EcarAuthenticationMode.Local);

    public EcarAuthenticationMode GetMode()
    {
        return Enum.TryParse<EcarAuthenticationMode>(Mode, ignoreCase: true, out var mode)
            ? mode
            : throw new InvalidOperationException(
                $"ECARAuthentication:Mode no admite el valor '{Mode}'. Use Local, ActiveDirectory, Hybrid o SgiIdentity.");
    }
}

public enum EcarAuthenticationMode
{
    Local,
    ActiveDirectory,
    Hybrid,

    /// <summary>Las contraseñas las valida, bloquea, hace caducar y cambia SGI.Identity; los roles salen de sus grupos.</summary>
    SgiIdentity
}
