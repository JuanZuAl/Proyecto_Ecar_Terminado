using ECAR.Shared.DTOs;

namespace ECAR.API.Services;

public interface IAuthService
{
    /// <summary>
    /// La sesión, o null si el usuario o la contraseña no son válidos (401). Lanza
    /// <see cref="ECAR.API.Exceptions.AutenticacionRechazadaException"/> cuando no se entra por otra razón: cuenta
    /// bloqueada, contraseña vencida o inicial pendiente (con la acción y el pase), sin rol, servicio no disponible.
    /// </summary>
    Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);

    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Confirma la identidad para firmar: false si la contraseña es incorrecta. En modo SgiIdentity, si se indican
    /// <paramref name="rolesPermitidos"/>, los grupos actuales del usuario deben dar alguno de ellos. Lanza
    /// <see cref="ECAR.API.Exceptions.AutenticacionRechazadaException"/> en los demás casos.
    /// </summary>
    Task<bool> VerificarPasswordAsync(long idUsuario, string? password, string operacion,
        IReadOnlyCollection<string>? rolesPermitidos = null);
}
