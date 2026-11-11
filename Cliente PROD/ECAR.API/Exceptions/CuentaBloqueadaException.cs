using ECAR.Shared;

namespace ECAR.API.Exceptions;

/// <summary>Cuenta local bloqueada por intentos fallidos en ECAR (423). Las de SGI.Identity las bloquea ese servicio.</summary>
public class CuentaBloqueadaException : AutenticacionRechazadaException
{
    public CuentaBloqueadaException(DateTime bloqueadoHasta)
        : base(StatusCodes.Status423Locked, CrearMensaje(bloqueadoHasta))
    {
        BloqueadoHasta = bloqueadoHasta;
    }

    public DateTime BloqueadoHasta { get; }

    private static string CrearMensaje(DateTime bloqueadoHasta)
    {
        var minutos = Math.Max(1, (int)Math.Ceiling((bloqueadoHasta - DateTime.UtcNow).TotalMinutes));
        return $"La cuenta está bloqueada por intentos fallidos. Intente de nuevo en {Plurales.Cantidad(minutos, "minuto", "minutos")} " +
               "o solicite el desbloqueo a un administrador";
    }
}
