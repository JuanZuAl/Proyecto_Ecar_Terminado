using System.ComponentModel.DataAnnotations;

namespace ECAR.Shared.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "El usuario o correo es requerido")]
    public string CorreoOrUsuarioAD { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es requerida")]
    public string Password { get; set; } = string.Empty;
}
