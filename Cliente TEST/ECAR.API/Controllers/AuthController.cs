using ECAR.API.Configuration;
using ECAR.API.Exceptions;
using ECAR.API.Services;
using ECAR.API.Services.SgiIdentity;
using ECAR.Infrastructure.Data;
using ECAR.Shared;
using ECAR.Shared.DTOs;
using ECAR.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECAR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    public const string MensajePaseVencido =
        "La solicitud venció o no es válida. Vuelva a iniciar sesión para continuar.";

    public const string MensajeSoloSgi =
        "Las contraseñas no se cambian en este sistema: se administran en el servicio de autenticación de la empresa.";

    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Inicio de sesión. 200 con la sesión; 401 usuario o contraseña incorrectos; 423 cuenta bloqueada; 403 cuando no
    /// se puede entrar todavía (en modo SgiIdentity, con <c>Data.AccionRequerida</c> y su pase si SGI.Identity pide
    /// cambiar la contraseña vencida o configurar la inicial); 503 si el servicio de autenticación no responde.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(LoginDto loginDto)
    {
        LoginResponseDto? result;
        try
        {
            result = await _authService.LoginAsync(loginDto);
        }
        catch (AutenticacionRechazadaException ex)
        {
            return StatusCode(ex.CodigoHttp, new ApiResponse<LoginResponseDto>
            {
                Success = false,
                Message = ex.Message,
                Data = ex.Datos
            });
        }

        if (result == null)
        {
            return Unauthorized(ApiResponse<LoginResponseDto>.ErrorResponse("Credenciales inválidas"));
        }

        return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(result, "Login exitoso"));
    }

    [HttpPost("validate")]
    public async Task<ActionResult<ApiResponse<bool>>> ValidateToken([FromBody] string token)
    {
        var isValid = await _authService.ValidateTokenAsync(token);

        if (!isValid)
        {
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Token inválido"));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Token válido"));
    }

    /// <summary>
    /// Política de las contraseñas del alta, minutos de inactividad y modo de autenticación. Las contraseñas no se
    /// cambian en este sistema salvo en modo SgiIdentity, y entonces las cambia SGI.Identity (endpoints de abajo).
    /// </summary>
    [HttpGet("politica")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<PoliticaSeguridadDto>> GetPolitica(
        [FromServices] IOptions<SeguridadOptions> opciones,
        [FromServices] IOptions<EcarAuthenticationOptions> autenticacion,
        [FromServices] IOptions<SgiIdentityOptions> sgi)
    {
        var politica = opciones.Value.ToDto();
        var modo = autenticacion.Value.GetMode();
        politica.ModoAutenticacion = modo.ToString();
        politica.CuentasLocales = modo != EcarAuthenticationMode.SgiIdentity || sgi.Value.PermitirCuentasLocales;
        return Ok(ApiResponse<PoliticaSeguridadDto>.SuccessResponse(politica));
    }

    /// <summary>
    /// Activa una cuenta local de SGI.Identity que pide configurar su contraseña (guía §7.2). Solo con el pase que dio
    /// el login al recibir RequiresInitialPasswordSetup. No crea sesión: después se vuelve a iniciar sesión.
    /// </summary>
    [HttpPost("password-inicial")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> ConfigurarPasswordInicial(
        ConfigurarPasswordInicialDto dto,
        [FromServices] IOptions<EcarAuthenticationOptions> autenticacion,
        [FromServices] PaseCambioPassword pase,
        [FromServices] ISgiIdentityClient sgi,
        [FromServices] ECARDbContext context)
    {
        if (autenticacion.Value.GetMode() != EcarAuthenticationMode.SgiIdentity)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(MensajeSoloSgi));
        }

        if (pase.Validar(dto.Pase, AccionesLogin.ConfigurarPasswordInicial) is not { } usuario)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(MensajePaseVencido));
        }

        var resultado = await sgi.ConfigurarPasswordInicialAsync(usuario, dto.NumeroIdentificacion.Trim(),
            dto.NuevaPassword, dto.ConfirmarPassword, HttpContext.RequestAborted);
        await RegistrarAsync(context, usuario, "Contraseña inicial", resultado);

        return Responder(resultado, "Su contraseña quedó configurada. Inicie sesión con ella.",
            "No fue posible configurar la contraseña.");
    }

    /// <summary>
    /// Cambia la contraseña de una cuenta local de SGI.Identity (guía §7.3). Solo con el pase que dio el login al recibir
    /// PasswordExpired o RequiresPasswordChange. No crea sesión: después se inicia sesión con la nueva contraseña.
    /// </summary>
    [HttpPost("cambiar-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<bool>>> CambiarPassword(
        CambiarPasswordDto dto,
        [FromServices] IOptions<EcarAuthenticationOptions> autenticacion,
        [FromServices] PaseCambioPassword pase,
        [FromServices] ISgiIdentityClient sgi,
        [FromServices] ECARDbContext context)
    {
        if (autenticacion.Value.GetMode() != EcarAuthenticationMode.SgiIdentity)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse(MensajeSoloSgi));
        }

        if (pase.Validar(dto.Pase, AccionesLogin.CambiarPassword) is not { } usuario)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(MensajePaseVencido));
        }

        var resultado = await sgi.CambiarPasswordAsync(usuario, dto.PasswordActual, dto.NuevaPassword,
            dto.ConfirmarPassword, HttpContext.RequestAborted);
        await RegistrarAsync(context, usuario, "Contraseña vencida", resultado);

        return Responder(resultado, "Su contraseña se cambió. Inicie sesión con la nueva contraseña.",
            "No fue posible cambiar la contraseña.");
    }

    /// <summary>
    /// Deja constancia del cierre de sesión (evento CierreSesion de §3.3): el cliente lo llama al
    /// salir y al cerrar por inactividad.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<bool>>> Logout(
        [FromServices] ECARDbContext context, [FromServices] ICurrentUser currentUser,
        [FromQuery] bool porInactividad = false)
    {
        await context.RegistrarEventoAsync(AuditoriaAcciones.CierreSesion, currentUser.IdUsuario,
            new { PorInactividad = porInactividad });
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Sesión cerrada"));
    }

    private ActionResult<ApiResponse<bool>> Responder(RespuestaOperacionSgi resultado, string mensajeExito, string mensajeRechazo) =>
        resultado.Resultado switch
        {
            ResultadoOperacionSgi.Exito => Ok(ApiResponse<bool>.SuccessResponse(true, mensajeExito)),
            // Los mensajes y la lista de errores de SGI.Identity se muestran tal cual (guía §7.2 y §7.3).
            ResultadoOperacionSgi.Rechazada => BadRequest(ApiResponse<bool>.ErrorResponse(
                string.IsNullOrWhiteSpace(resultado.Mensaje) ? mensajeRechazo : resultado.Mensaje,
                resultado.Errores.Count > 0 ? resultado.Errores.ToList() : null)),
            ResultadoOperacionSgi.ClienteRechazado => StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<bool>.ErrorResponse(AuthService.MensajeClienteRechazado)),
            _ => StatusCode(StatusCodes.Status503ServiceUnavailable,
                ApiResponse<bool>.ErrorResponse(AuthService.MensajeServicioNoDisponible))
        };

    /// <summary>El intento queda en la auditoría con su resultado, nunca con las contraseñas ni la identificación.</summary>
    private static async Task RegistrarAsync(ECARDbContext context, string usuarioSgi, string tipo, RespuestaOperacionSgi resultado)
    {
        var local = await context.Usuarios.AsNoTracking()
            .Where(u => u.UsuarioAD == usuarioSgi)
            .Select(u => new { u.IdUsuario, u.Nombre, u.Correo })
            .FirstOrDefaultAsync();
        await context.RegistrarEventoAsync(AuditoriaAcciones.CambioPassword, local?.IdUsuario ?? 0,
            new
            {
                Tipo = tipo,
                Fuente = AuthService.FuenteSgi,
                Usuario = usuarioSgi,
                Resultado = resultado.Resultado.ToString(),
                resultado.Codigo
            },
            usuario: local is null ? $"{usuarioSgi} (SGI.Identity)" : DescripcionUsuario.Texto(local.Nombre, usuarioSgi, local.Correo),
            idUsuario: local?.IdUsuario);
    }
}
