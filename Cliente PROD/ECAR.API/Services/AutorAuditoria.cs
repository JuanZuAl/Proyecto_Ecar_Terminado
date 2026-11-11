namespace ECAR.API.Services;

/// <summary>
/// Autor de los cambios de una petición que todavía no tiene token: el inicio de sesión con SGI.Identity registra al
/// usuario o le ajusta los roles antes de tener sesión, y sin esto la auditoría diría «Anónimo».
/// ContextoAuditoriaHttp lo usa cuando la petición no está autenticada.
/// </summary>
public sealed class AutorAuditoria
{
    public string? Usuario { get; private set; }

    public long? IdUsuario { get; private set; }

    public void Establecer(string usuario, long? idUsuario)
    {
        Usuario = usuario;
        IdUsuario = idUsuario;
    }
}
