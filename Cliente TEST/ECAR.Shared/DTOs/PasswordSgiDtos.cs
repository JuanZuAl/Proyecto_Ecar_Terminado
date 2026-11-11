using System.ComponentModel.DataAnnotations;

namespace ECAR.Shared.DTOs;

/// <summary>
/// POST api/auth/password-inicial: activa una cuenta local de SGI.Identity que pide configurar su contraseña. El
/// usuario sale del pase que dio el login; las reglas de la contraseña las valida SGI.Identity.
/// </summary>
public class ConfigurarPasswordInicialDto
{
    [Required(ErrorMessage = "La solicitud no es válida: vuelva a iniciar sesión")]
    public string Pase { get; set; } = string.Empty;

    [Required(ErrorMessage = "El número de identificación es requerido")]
    [MaxLength(50, ErrorMessage = "El número de identificación no puede exceder 50 caracteres")]
    public string NumeroIdentificacion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [MaxLength(256, ErrorMessage = "La contraseña no puede exceder 256 caracteres")]
    public string NuevaPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de la contraseña es requerida")]
    [MaxLength(256, ErrorMessage = "La contraseña no puede exceder 256 caracteres")]
    public string ConfirmarPassword { get; set; } = string.Empty;
}

/// <summary>
/// POST api/auth/cambiar-password: cambia la contraseña vencida de una cuenta local de SGI.Identity. Las reglas, el
/// historial y la reutilización los valida SGI.Identity.
/// </summary>
public class CambiarPasswordDto
{
    [Required(ErrorMessage = "La solicitud no es válida: vuelva a iniciar sesión")]
    public string Pase { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña actual es requerida")]
    [MaxLength(256, ErrorMessage = "La contraseña no puede exceder 256 caracteres")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es requerida")]
    [MaxLength(256, ErrorMessage = "La contraseña no puede exceder 256 caracteres")]
    public string NuevaPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de la contraseña es requerida")]
    [MaxLength(256, ErrorMessage = "La contraseña no puede exceder 256 caracteres")]
    public string ConfirmarPassword { get; set; } = string.Empty;
}
