using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECAR.API.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ECAR.API.Services.SgiIdentity;

/// <summary>
/// Pase para cambiar la contraseña vencida o configurar la inicial. Solo lo da el login cuando SGI.Identity responde
/// PasswordExpired o RequiresInitialPasswordSetup, y solo para el usuario con el que se intentó entrar: así esas
/// pantallas se abren únicamente cuando el servicio lo pide, y los dos endpoints no quedan abiertos a cualquiera.
/// </summary>
/// <remarks>
/// Es un JWT firmado con JWT:Secret, de pocos minutos, con otra audiencia ("ECAR-SgiPassword") y sin sello de
/// seguridad: no sirve como sesión, porque el API exige la audiencia ECAR-Users y el sello (SelloSeguridad).
/// </remarks>
public sealed class PaseCambioPassword(IConfiguration configuration, IOptions<SgiIdentityOptions> opciones)
{
    public const string Audiencia = "ECAR-SgiPassword";
    private const string ClaimUsuario = "ecar_pase_usuario";
    private const string ClaimAccion = "ecar_pase_accion";

    public string Crear(string usuario, string accion)
    {
        var credenciales = new SigningCredentials(Clave(), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Emisor,
            audience: Audiencia,
            claims: [new Claim(ClaimUsuario, usuario), new Claim(ClaimAccion, accion)],
            expires: DateTime.UtcNow.AddMinutes(opciones.Value.MinutosPaseCambioPassword),
            signingCredentials: credenciales);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>El usuario del pase si es válido, no venció y es para <paramref name="accion"/>; si no, null.</summary>
    public string? Validar(string? pase, string accion)
    {
        if (string.IsNullOrWhiteSpace(pase))
        {
            return null;
        }

        try
        {
            var manejador = new JwtSecurityTokenHandler { MapInboundClaims = false };
            var principal = manejador.ValidateToken(pase, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = Emisor,
                ValidateAudience = true,
                ValidAudience = Audiencia,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = Clave(),
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal.FindFirstValue(ClaimAccion) == accion &&
                   principal.FindFirstValue(ClaimUsuario) is { Length: > 0 } usuario
                ? usuario
                : null;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }

    private string Emisor => configuration["JWT:Issuer"] ?? "ECAR-Auditoria";

    private SymmetricSecurityKey Clave() => new(Encoding.UTF8.GetBytes(
        configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT:Secret no está configurado (user-secrets o variable de entorno JWT__Secret)")));
}
