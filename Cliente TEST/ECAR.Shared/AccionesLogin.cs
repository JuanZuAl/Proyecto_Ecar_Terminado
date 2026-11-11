namespace ECAR.Shared;

/// <summary>
/// Lo que pide SGI.Identity antes de dejar entrar a un usuario (LoginResponseDto.AccionRequerida). El cliente abre la
/// pantalla de cambio de contraseña o la de activación de la cuenta solo cuando el API responde con una de estas
/// acciones, es decir, solo cuando SGI.Identity respondió «PasswordExpired» o «RequiresInitialPasswordSetup».
/// </summary>
public static class AccionesLogin
{
    /// <summary>Cuenta local nueva de SGI.Identity: se configura la contraseña inicial con el número de identificación.</summary>
    public const string ConfigurarPasswordInicial = "ConfigurarPasswordInicial";

    /// <summary>La contraseña venció: se cambia con la actual.</summary>
    public const string CambiarPassword = "CambiarPassword";
}

/// <summary>Valores de ECARAuthentication:Mode, que también publica GET api/auth/politica.</summary>
public static class ModosAutenticacion
{
    public const string Local = "Local";
    public const string ActiveDirectory = "ActiveDirectory";
    public const string Hybrid = "Hybrid";

    /// <summary>
    /// Las credenciales las valida el servicio corporativo SGI.Identity, y los roles salen de los grupos que devuelve.
    /// </summary>
    public const string SgiIdentity = "SgiIdentity";
}
