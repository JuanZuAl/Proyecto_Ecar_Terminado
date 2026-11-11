namespace ECAR.Shared.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime Expiration { get; set; }

    // Sin DebeCambiarPassword ni PasswordExpiraEl desde el 03/11/2026: este sistema no cambia contraseñas.

    // --- Solo en una respuesta fallida (403) del modo SgiIdentity, sin token ---

    /// <summary>
    /// Lo que SGI.Identity pide antes de dejar entrar (AccionesLogin): configurar la contraseña inicial o cambiar la
    /// vencida. Null en los demás casos.
    /// </summary>
    public string? AccionRequerida { get; set; }

    /// <summary>
    /// Pase para esa acción: vale unos minutos, solo para el usuario con el que se intentó entrar y solo en
    /// POST api/auth/password-inicial o api/auth/cambiar-password. No es una sesión.
    /// </summary>
    public string? PaseCambioPassword { get; set; }

    /// <summary>El usuario con el que se intentó entrar, para mostrarlo en la pantalla de la acción.</summary>
    public string? Usuario { get; set; }
}
