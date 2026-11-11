namespace ECAR.Client.Services;

/// <summary>
/// Lo que el login recibió cuando SGI.Identity pidió cambiar la contraseña vencida o configurar la inicial: la acción,
/// el pase del API, el usuario y el mensaje. Vive solo en memoria (nunca en la URL ni en localStorage), así que las
/// pantallas /cambiar-password y /activar-cuenta solo se abren después de esa respuesta; si se entra a ellas de otra
/// forma, o se recarga la página, vuelven al login.
/// </summary>
public class CambioPasswordEstado
{
    public string? Accion { get; private set; }
    public string? Pase { get; private set; }
    public string? Usuario { get; private set; }
    public string? Mensaje { get; private set; }

    /// <summary>Página a la que volver después de iniciar sesión con la nueva contraseña.</summary>
    public string? ReturnUrl { get; private set; }

    public void Establecer(string accion, string pase, string? usuario, string? mensaje, string? returnUrl)
    {
        Accion = accion;
        Pase = pase;
        Usuario = usuario;
        Mensaje = mensaje;
        ReturnUrl = returnUrl;
    }

    public bool EsPara(string accion) => Accion == accion && !string.IsNullOrEmpty(Pase);

    public void Limpiar()
    {
        Accion = null;
        Pase = null;
        Usuario = null;
        Mensaje = null;
        ReturnUrl = null;
    }
}
