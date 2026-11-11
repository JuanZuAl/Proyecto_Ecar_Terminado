using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ECAR.API.Configuration;
using Microsoft.Extensions.Options;

namespace ECAR.API.Services.SgiIdentity;

/// <summary>Resultado del login de SGI.Identity, traducido para ECAR.</summary>
public enum EstadoLoginSgi
{
    /// <summary>status = Authenticated, isAuthenticated = true y con usuario: se puede crear la sesión de ECAR.</summary>
    Autenticado,
    /// <summary>RequiresInitialPasswordSetup: cuenta local nueva que debe configurar su contraseña.</summary>
    RequiereConfigurarPassword,
    /// <summary>InvalidCredentials: usuario o contraseña incorrectos.</summary>
    CredencialesInvalidas,
    /// <summary>UserLocked.</summary>
    UsuarioBloqueado,
    /// <summary>PasswordExpired.</summary>
    PasswordVencida,
    /// <summary>RequiresPasswordChange: SGI.Identity exige cambiar la contraseña antes de entrar.</summary>
    RequiereCambioPassword,
    /// <summary>UserInactive: la cuenta está inactiva en SGI.Identity.</summary>
    UsuarioInactivo,
    /// <summary>UserWithoutGroups: la cuenta no tiene ningún grupo en SGI.Identity.</summary>
    UsuarioSinGrupos,
    /// <summary>SGI.Identity no aceptó a ECAR como aplicación consumidora (ClientId o ClientSecret, 401/403 sin estado).</summary>
    ClienteRechazado,
    /// <summary>Sin respuesta, tiempo agotado, 5xx o una respuesta que no se reconoce: nunca se deja entrar.</summary>
    NoDisponible
}

/// <param name="Nombre">displayName: el nombre de la persona; null si SGI.Identity no lo envía.</param>
public sealed record UsuarioSgi(string UserName, string? UserId, string? Fuente, IReadOnlyList<string> Grupos,
    string? Nombre = null);

public sealed record ResultadoLoginSgi(EstadoLoginSgi Estado, string? Mensaje, UsuarioSgi? Usuario = null);

public enum ResultadoOperacionSgi
{
    Exito,
    /// <summary>SGI.Identity rechazó la operación (success = false): política, confirmación, usuario, reutilización…</summary>
    Rechazada,
    ClienteRechazado,
    NoDisponible
}

public sealed record RespuestaOperacionSgi(ResultadoOperacionSgi Resultado, string? Codigo, string? Mensaje,
    IReadOnlyList<string> Errores);

public interface ISgiIdentityClient
{
    Task<ResultadoLoginSgi> LoginAsync(string usuario, string password, CancellationToken cancellationToken = default);

    Task<RespuestaOperacionSgi> ConfigurarPasswordInicialAsync(string usuario, string numeroIdentificacion,
        string nuevaPassword, string confirmarPassword, CancellationToken cancellationToken = default);

    Task<RespuestaOperacionSgi> CambiarPasswordAsync(string usuario, string passwordActual, string nuevaPassword,
        string confirmarPassword, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cliente de SGI.Identity (guía de integración v1.0, §7). Cada llamada se identifica con
/// <c>Authorization: Basic base64(ClientId:ClientSecret)</c> y lleva los datos del usuario en el cuerpo.
/// </summary>
/// <remarks>
/// Las decisiones salen de <c>status</c> e <c>isAuthenticated</c> (o de <c>success</c>), nunca del texto de
/// <c>message</c> ni del <c>statusCode</c> del JSON (§7.1 y §8, regla 6). Ante cualquier respuesta que no se
/// reconozca, no se deja entrar. Nunca se registran el cuerpo, las contraseñas, el ClientId, el ClientSecret ni el
/// encabezado Authorization; el HttpClient tiene ese encabezado oculto en sus registros (Program.cs).
/// </remarks>
public sealed class SgiIdentityClient(HttpClient http, IOptions<SgiIdentityOptions> opciones, ILogger<SgiIdentityClient> logger)
    : ISgiIdentityClient
{
    public const string RutaLogin = "api/authentication/login";
    public const string RutaPasswordInicial = "api/authentication/initial-password";
    public const string RutaCambioPassword = "api/authentication/change-password";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    public async Task<ResultadoLoginSgi> LoginAsync(string usuario, string password, CancellationToken cancellationToken = default)
    {
        if (await EnviarAsync(RutaLogin, new { username = usuario, password }, cancellationToken) is not { } respuesta)
        {
            return new ResultadoLoginSgi(EstadoLoginSgi.NoDisponible, null);
        }

        var codigo = respuesta.Codigo;
        var cuerpo = Leer<RespuestaLogin>(respuesta.Texto);
        if (cuerpo?.Status is { Length: > 0 } estado)
        {
            switch (estado.Trim())
            {
                case var s when Es(s, "Authenticated"):
                    if (respuesta.Exito && cuerpo.IsAuthenticated == true &&
                        cuerpo.User?.UserName is { Length: > 0 } nombre)
                    {
                        var grupos = (cuerpo.User.Groups ?? [])
                            .Where(g => !string.IsNullOrWhiteSpace(g))
                            .Select(g => g.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();
                        return new ResultadoLoginSgi(EstadoLoginSgi.Autenticado, cuerpo.Message,
                            new UsuarioSgi(nombre.Trim(), Texto(cuerpo.User.UserId), cuerpo.User.AuthenticationSource, grupos,
                                string.IsNullOrWhiteSpace(cuerpo.User.DisplayName) ? null : cuerpo.User.DisplayName.Trim()));
                    }

                    break;
                case var s when Es(s, "RequiresInitialPasswordSetup"):
                    return new ResultadoLoginSgi(EstadoLoginSgi.RequiereConfigurarPassword, cuerpo.Message);
                case var s when Es(s, "InvalidCredentials"):
                    return new ResultadoLoginSgi(EstadoLoginSgi.CredencialesInvalidas, cuerpo.Message);
                case var s when Es(s, "UserLocked"):
                    return new ResultadoLoginSgi(EstadoLoginSgi.UsuarioBloqueado, cuerpo.Message);
                case var s when Es(s, "PasswordExpired"):
                    return new ResultadoLoginSgi(EstadoLoginSgi.PasswordVencida, cuerpo.Message);
                case var s when Es(s, "RequiresPasswordChange"):
                    return new ResultadoLoginSgi(EstadoLoginSgi.RequiereCambioPassword, cuerpo.Message);
                case var s when Es(s, "UserInactive"):
                    return new ResultadoLoginSgi(EstadoLoginSgi.UsuarioInactivo, cuerpo.Message);
                case var s when Es(s, "UserWithoutGroups"):
                    return new ResultadoLoginSgi(EstadoLoginSgi.UsuarioSinGrupos, cuerpo.Message);
            }
        }

        // Un 401/403 sin estado del login es el de la autenticación Basic: rechazó a ECAR, no al usuario (§10).
        if (codigo is 401 or 403)
        {
            RegistrarClienteRechazado(codigo);
            return new ResultadoLoginSgi(EstadoLoginSgi.ClienteRechazado, null);
        }

        logger.LogWarning("SGI.Identity respondió al login con HTTP {Codigo} y un estado que ECAR no reconoce ({Estado}); no se deja entrar",
            codigo, cuerpo?.Status ?? "sin estado");
        return new ResultadoLoginSgi(EstadoLoginSgi.NoDisponible, null);
    }

    public Task<RespuestaOperacionSgi> ConfigurarPasswordInicialAsync(string usuario, string numeroIdentificacion,
        string nuevaPassword, string confirmarPassword, CancellationToken cancellationToken = default) =>
        OperacionAsync(RutaPasswordInicial, new
        {
            username = usuario,
            identificationNumber = numeroIdentificacion,
            newPassword = nuevaPassword,
            confirmPassword = confirmarPassword
        }, cancellationToken);

    public Task<RespuestaOperacionSgi> CambiarPasswordAsync(string usuario, string passwordActual, string nuevaPassword,
        string confirmarPassword, CancellationToken cancellationToken = default) =>
        OperacionAsync(RutaCambioPassword, new
        {
            username = usuario,
            currentPassword = passwordActual,
            newPassword = nuevaPassword,
            confirmPassword = confirmarPassword
        }, cancellationToken);

    private async Task<RespuestaOperacionSgi> OperacionAsync(string ruta, object cuerpoPeticion, CancellationToken cancellationToken)
    {
        if (await EnviarAsync(ruta, cuerpoPeticion, cancellationToken) is not { } respuesta)
        {
            return new RespuestaOperacionSgi(ResultadoOperacionSgi.NoDisponible, null, null, []);
        }

        var codigo = respuesta.Codigo;
        var texto = respuesta.Texto;
        var cuerpo = Leer<RespuestaOperacion>(texto);
        if (cuerpo?.Success is { } exito)
        {
            var errores = (cuerpo.Errors ?? []).Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList();
            return exito && respuesta.Exito
                ? new RespuestaOperacionSgi(ResultadoOperacionSgi.Exito, cuerpo.Code, cuerpo.Message, [])
                : new RespuestaOperacionSgi(ResultadoOperacionSgi.Rechazada, cuerpo.Code, cuerpo.Message, errores);
        }

        if (codigo is 401 or 403)
        {
            RegistrarClienteRechazado(codigo);
            return new RespuestaOperacionSgi(ResultadoOperacionSgi.ClienteRechazado, null, null, []);
        }

        // Un 400 de la validación automática (ProblemDetails, guía §15.6) no trae «success» pero sí los errores.
        if (codigo == 400 && ErroresDeValidacion(texto) is { Count: > 0 } validacion)
        {
            return new RespuestaOperacionSgi(ResultadoOperacionSgi.Rechazada, "VALIDATION_ERROR",
                "La solicitud no cumple los requisitos del servicio de autenticación.", validacion);
        }

        logger.LogWarning("SGI.Identity respondió a {Ruta} con HTTP {Codigo} sin un resultado reconocible", ruta, codigo);
        return new RespuestaOperacionSgi(ResultadoOperacionSgi.NoDisponible, null, null, []);
    }

    private sealed record RespuestaHttp(int Codigo, bool Exito, string Texto);

    /// <summary>El código y el cuerpo de la respuesta, o null si el servicio no respondió a tiempo.</summary>
    private async Task<RespuestaHttp?> EnviarAsync(string ruta, object cuerpo, CancellationToken cancellationToken)
    {
        var configuracion = opciones.Value;
        using var peticion = new HttpRequestMessage(HttpMethod.Post, ruta)
        {
            Content = JsonContent.Create(cuerpo, options: OpcionesJson)
        };
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{configuracion.ClientId}:{configuracion.ClientSecret}")));
        peticion.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var respuesta = await http.SendAsync(peticion, cancellationToken);
            var texto = await respuesta.Content.ReadAsStringAsync(cancellationToken);
            return new RespuestaHttp((int)respuesta.StatusCode, respuesta.IsSuccessStatusCode, texto);
        }
        catch (HttpRequestException ex)
        {
            // El mensaje de la excepción no lleva el cuerpo ni los encabezados.
            logger.LogWarning("SGI.Identity no respondió en {Ruta}: {Mensaje}", ruta, ex.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("SGI.Identity no respondió en {Ruta} en {Segundos} s", ruta, configuracion.TimeoutSegundos);
        }

        return null;
    }

    // Ni el ClientId ni el ClientSecret se escriben en el registro: son las credenciales técnicas de ECAR (guía §4.3).
    private void RegistrarClienteRechazado(int codigo) =>
        logger.LogError("SGI.Identity rechazó a ECAR como aplicación consumidora (HTTP {Codigo}). Revise SgiIdentity:ClientId " +
                        "y SgiIdentity:ClientSecret del ambiente", codigo);

    private static bool Es(string estado, string esperado) => string.Equals(estado, esperado, StringComparison.OrdinalIgnoreCase);

    private static string? Texto(JsonElement? valor) => valor switch
    {
        { ValueKind: JsonValueKind.String } v => v.GetString(),
        { ValueKind: JsonValueKind.Number } v => v.GetRawText(),
        _ => null
    };

    private static T? Leer<T>(string? texto) where T : class
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(texto, OpcionesJson);
        }
        catch (JsonException)
        {
            // Un error de la infraestructura puede no ser JSON (guía §10).
            return null;
        }
    }

    private static List<string> ErroresDeValidacion(string? texto)
    {
        var lista = new List<string>();
        if (string.IsNullOrWhiteSpace(texto))
        {
            return lista;
        }

        try
        {
            using var documento = JsonDocument.Parse(texto);
            if (documento.RootElement.ValueKind == JsonValueKind.Object &&
                documento.RootElement.TryGetProperty("errors", out var errores) && errores.ValueKind == JsonValueKind.Object)
            {
                foreach (var campo in errores.EnumerateObject())
                {
                    if (campo.Value.ValueKind == JsonValueKind.Array)
                    {
                        lista.AddRange(campo.Value.EnumerateArray()
                            .Where(e => e.ValueKind == JsonValueKind.String)
                            .Select(e => e.GetString()!)
                            .Where(e => e.Length > 0));
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        return lista;
    }

    private sealed class RespuestaLogin
    {
        public string? Status { get; set; }
        public bool? IsAuthenticated { get; set; }
        public string? Message { get; set; }
        public UsuarioRespuesta? User { get; set; }
    }

    private sealed class UsuarioRespuesta
    {
        public string? UserName { get; set; }
        public string? DisplayName { get; set; }
        public JsonElement? UserId { get; set; }
        public string? AuthenticationSource { get; set; }
        public List<string>? Groups { get; set; }
    }

    private sealed class RespuestaOperacion
    {
        public bool? Success { get; set; }
        public string? Code { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
    }
}
