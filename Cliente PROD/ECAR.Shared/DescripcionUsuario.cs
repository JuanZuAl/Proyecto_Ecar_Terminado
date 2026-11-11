namespace ECAR.Shared;

/// <summary>
/// Cómo se nombra a una persona en la auditoría, al confirmar la firma y en el filtro de usuarios de los reportes:
/// «Nombre (usuario)», por ejemplo «Carlos Mejía (cmejia)». ECAR identifica a su gente por el usuario (el de
/// SGI.Identity), no por el correo, que para quien entra por SGI.Identity es provisional.
/// </summary>
/// <remarks>
/// La firma electrónica no usa este texto: guarda solo el nombre (FirmaNombre), que es lo que entra en el sello.
/// </remarks>
public static class DescripcionUsuario
{
    /// <summary>Claim del token de sesión con el usuario de la persona (Usuarios.UsuarioAD).</summary>
    public const string ClaimUsuario = "ecar_usuario";

    /// <summary>
    /// «Nombre (usuario)». Sin usuario (una cuenta local que solo tiene correo), «Nombre (correo)». Si el nombre
    /// todavía es el mismo usuario (primer ingreso, antes de que el Administrador lo complete), solo el usuario.
    /// </summary>
    public static string Texto(string? nombre, string? usuario, string? correo = null)
    {
        var elNombre = nombre?.Trim() ?? string.Empty;
        var identificador = !string.IsNullOrWhiteSpace(usuario) ? usuario.Trim() : correo?.Trim() ?? string.Empty;
        if (identificador.Length == 0 || string.Equals(elNombre, identificador, StringComparison.OrdinalIgnoreCase))
        {
            return elNombre;
        }

        return elNombre.Length == 0 ? identificador : $"{elNombre} ({identificador})";
    }
}
