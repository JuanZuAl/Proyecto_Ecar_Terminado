using ECAR.Shared;

namespace ECAR.API.Exceptions;

public class CuentaBloqueadaException : Exception
{
    public CuentaBloqueadaException(DateTime bloqueadoHasta)
        : base(CrearMensaje(bloqueadoHasta))
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
