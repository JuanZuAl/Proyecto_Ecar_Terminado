namespace ECAR.Shared.DTOs;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime Expiration { get; set; }

    // Sin DebeCambiarPassword ni PasswordExpiraEl desde el 03/11/2026: el sistema ya no cambia contraseñas.
}