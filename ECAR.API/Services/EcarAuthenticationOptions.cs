namespace ECAR.API.Services;

public sealed class EcarAuthenticationOptions
{
    public const string SectionName = "ECARAuthentication";

    /// <summary>
    /// Valores admitidos: Local, ActiveDirectory e Hybrid.
    /// ActiveDirectory e Hybrid requieren un IActiveDirectoryAuthService configurado.
    /// </summary>
    public string Mode { get; set; } = nameof(EcarAuthenticationMode.Local);

    public EcarAuthenticationMode GetMode()
    {
        return Enum.TryParse<EcarAuthenticationMode>(Mode, ignoreCase: true, out var mode)
            ? mode
            : throw new InvalidOperationException(
                $"ECARAuthentication:Mode no admite el valor '{Mode}'. Use Local, ActiveDirectory o Hybrid.");
    }
}

public enum EcarAuthenticationMode
{
    Local,
    ActiveDirectory,
    Hybrid
}
