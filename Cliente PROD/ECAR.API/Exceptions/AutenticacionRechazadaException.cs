using ECAR.Shared.DTOs;

namespace ECAR.API.Exceptions;

/// <summary>
/// No se pudo entrar o confirmar la identidad para firmar, por algo distinto de una contraseña incorrecta: cuenta
/// bloqueada (423), contraseña vencida o sin rol en ECAR (403), servicio de autenticación no disponible (503). Los
/// controladores responden con <see cref="CodigoHttp"/> y este mensaje, que se puede mostrar tal cual.
/// </summary>
/// <remarks>
/// Una contraseña incorrecta no lanza esta excepción: el login responde 401 y la firma 400, nunca 401, porque el
/// cliente trata un 401 con token como sesión vencida.
/// </remarks>
public class AutenticacionRechazadaException : Exception
{
    public AutenticacionRechazadaException(int codigoHttp, string mensaje, LoginResponseDto? datos = null)
        : base(mensaje)
    {
        CodigoHttp = codigoHttp;
        Datos = datos;
    }

    public int CodigoHttp { get; }

    /// <summary>Solo en el login: la acción que pide SGI.Identity y el pase para hacerla.</summary>
    public LoginResponseDto? Datos { get; }
}
