using ECAR.API.Services.SgiIdentity;

namespace ECAR.API.Configuration;

/// <summary>
/// Conexión con SGI.Identity, el servicio corporativo de autenticación (guía de integración v1.0). Solo se usa con
/// ECARAuthentication:Mode = SgiIdentity.
/// </summary>
/// <remarks>
/// BaseUrl puede ir en appsettings. ClientId y ClientSecret son las credenciales técnicas de ECAR (guía §4.3, §5.1
/// y §13) y se tratan igual: nunca en appsettings; user-secrets en desarrollo y, en el servidor, las variables de
/// entorno SgiIdentity__ClientId y SgiIdentity__ClientSecret. Solo salen de este API, en el encabezado
/// Authorization: Basic de cada llamada; nunca llegan al navegador ni a los registros.
/// </remarks>
public sealed class SgiIdentityOptions
{
    public const string SectionName = "SgiIdentity";

    public const int LongitudMaximaDominioCorreo = 40;

    /// <summary>URL del servicio en ese ambiente, tal como la publica TI. Debe ser HTTPS, salvo <see cref="PermitirHttp"/>.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Admite una BaseUrl http:// fuera de localhost. Por HTTP, las contraseñas de los usuarios y las credenciales de
    /// ECAR viajan sin cifrar por la red: solo mientras TI no publique el servicio por HTTPS (su documento de
    /// arquitectura §10.3 lo deja pendiente). Cuando lo haga, cambie la URL y quite esta opción.
    /// </summary>
    public bool PermitirHttp { get; set; }

    /// <summary>La BaseUrl es http:// y no es de este mismo equipo: el tráfico con SGI.Identity va sin cifrar.</summary>
    public bool UsaHttpSinCifrar =>
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttp && !url.IsLoopback;

    /// <summary>
    /// Identificador técnico de ECAR en SGI.Identity (lo entrega TI por aplicación y ambiente). Confidencial, como
    /// el secreto: user-secrets o variable de entorno SgiIdentity__ClientId.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Secreto técnico de ECAR. Fuera de Git: user-secrets o variable de entorno SgiIdentity__ClientSecret.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Tiempo máximo de espera de cada llamada; pasado, se responde «servicio no disponible».</summary>
    public int TimeoutSegundos { get; set; } = 15;

    /// <summary>
    /// Si las cuentas locales (las que tienen contraseña guardada en ECAR, como admin@ecar.com) pueden seguir
    /// entrando. Sirve de acceso de respaldo mientras se valida la integración (guía §12); TI puede apagarlo después.
    /// </summary>
    public bool PermitirCuentasLocales { get; set; } = true;

    /// <summary>
    /// Si el primer inicio de sesión de un usuario validado por SGI.Identity lo registra en ECAR. Con false, el
    /// Administrador debe registrarlo antes en Usuarios (con su usuario de SGI.Identity).
    /// </summary>
    public bool CrearUsuariosAlEntrar { get; set; } = true;

    /// <summary>
    /// Dominio del correo de un usuario registrado al entrar (SGI.Identity no devuelve el correo): T0000 queda como
    /// T0000@dominio hasta que el Administrador lo corrija. Si el usuario ya es un correo, se usa tal cual.
    /// </summary>
    public string DominioCorreo { get; set; } = "sgi.local";

    /// <summary>Minutos que vale el pase para cambiar la contraseña vencida o configurar la inicial.</summary>
    public int MinutosPaseCambioPassword { get; set; } = 10;

    /// <summary>
    /// Grupo de SGI.Identity → rol de ECAR (Administrador, Supervisor, Estándar u Operador). Un usuario recibe los
    /// roles de todos sus grupos que estén aquí. Si se deja vacío, un grupo que se llame como un rol (sin distinguir
    /// mayúsculas ni tildes) da ese rol.
    /// </summary>
    public Dictionary<string, string> Grupos { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Errores de configuración; vacío si se puede arrancar en modo SgiIdentity.</summary>
    public List<string> Validar()
    {
        var errores = new List<string>();
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var url) ||
            (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp))
        {
            errores.Add("SgiIdentity:BaseUrl debe ser la URL del servicio (https://…)");
        }
        else if (UsaHttpSinCifrar && !PermitirHttp)
        {
            errores.Add("SgiIdentity:BaseUrl usa http: las contraseñas viajarían sin cifrar. Use la URL https del servicio o, " +
                        "mientras TI no la publique, ponga SgiIdentity:PermitirHttp en true");
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            errores.Add("SgiIdentity:ClientId es obligatorio (user-secrets o variable de entorno SgiIdentity__ClientId, nunca en appsettings)");
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            errores.Add("SgiIdentity:ClientSecret es obligatorio (user-secrets o variable de entorno SgiIdentity__ClientSecret, nunca en appsettings)");
        }

        if (TimeoutSegundos is < 1 or > 120)
        {
            errores.Add("SgiIdentity:TimeoutSegundos debe estar entre 1 y 120");
        }

        if (MinutosPaseCambioPassword is < 1 or > 60)
        {
            errores.Add("SgiIdentity:MinutosPaseCambioPassword debe estar entre 1 y 60");
        }

        if (string.IsNullOrWhiteSpace(DominioCorreo) || DominioCorreo.Trim().Length > LongitudMaximaDominioCorreo ||
            DominioCorreo.Contains('@') || DominioCorreo.Contains(' '))
        {
            errores.Add($"SgiIdentity:DominioCorreo debe ser un dominio sin @ ni espacios, de hasta {LongitudMaximaDominioCorreo} caracteres");
        }

        foreach (var (grupo, rol) in Grupos)
        {
            if (MapeoGruposRoles.RolDelSistema(rol) is null)
            {
                errores.Add($"SgiIdentity:Grupos: el grupo «{grupo}» apunta a «{rol}», que no es un rol de ECAR (Administrador, Supervisor, Estándar u Operador)");
            }
        }

        return errores;
    }
}
