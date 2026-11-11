using System.Security.Claims;
using ECAR.Infrastructure.Data;
using ECAR.Shared;

namespace ECAR.API.Services;

/// <summary>
/// La auditoría (§3.3 y Parte 11 §11.10(e) y (h)) toma de la petición en curso quién hace el
/// cambio, desde qué IP y con qué navegador, y el motivo que fijó el controlador. Fuera de una
/// petición (la siembra al arrancar) el autor es "Sistema". En una petición sin token, el que fijó
/// <see cref="AutorAuditoria"/> (el inicio de sesión con SGI.Identity) o, si no hay, "Anónimo".
/// </summary>
public sealed class ContextoAuditoriaHttp(IHttpContextAccessor httpContextAccessor, IMotivoCambio motivoCambio,
    AutorAuditoria? autor = null)
    : IContextoAuditoria
{
    private HttpContext? Peticion => httpContextAccessor.HttpContext;

    private ClaimsPrincipal? Autenticado =>
        Peticion?.User.Identity?.IsAuthenticated == true ? Peticion.User : null;

    public long? IdUsuario =>
        long.TryParse(Autenticado?.FindFirstValue(ClaimTypes.NameIdentifier), out var idUsuario)
            ? idUsuario
            : Autenticado is null ? autor?.IdUsuario : null;

    public string Usuario
    {
        get
        {
            if (Peticion is null)
            {
                return "Sistema";
            }

            if (Autenticado is not { } principal)
            {
                return string.IsNullOrWhiteSpace(autor?.Usuario) ? "Anónimo" : autor.Usuario;
            }

            // «Nombre (usuario)»; con un token anterior a este cambio (sin el claim del usuario), «Nombre (correo)».
            return DescripcionUsuario.Texto(principal.FindFirstValue(ClaimTypes.Name),
                principal.FindFirstValue(DescripcionUsuario.ClaimUsuario), principal.FindFirstValue(ClaimTypes.Email));
        }
    }

    public string? DireccionIp => Peticion?.Connection.RemoteIpAddress?.ToString();

    public string? AgenteUsuario
    {
        get
        {
            var agente = Peticion?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(agente) ? null : agente;
        }
    }

    public string? Motivo => motivoCambio.Motivo;
}
