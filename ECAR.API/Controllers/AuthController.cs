using ECAR.API.Configuration;
using ECAR.API.Exceptions;
using ECAR.API.Services;
using ECAR.Infrastructure.Data;
using ECAR.Shared;
using ECAR.Shared.DTOs;
using ECAR.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ECAR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(LoginDto loginDto)
    {
        LoginResponseDto? result;
        try
        {
            result = await _authService.LoginAsync(loginDto);
        }
        catch (CuentaBloqueadaException ex)
        {
            return StatusCode(StatusCodes.Status423Locked, ApiResponse<LoginResponseDto>.ErrorResponse(ex.Message));
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
    /// Política de las contraseñas del alta y minutos de inactividad. Desde el 03/11/2026 no hay cambio ni
    /// restablecimiento de contraseñas en este sistema: los administra una API externa.
    /// </summary>
    [HttpGet("politica")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<PoliticaSeguridadDto>> GetPolitica([FromServices] IOptions<SeguridadOptions> opciones) =>
        Ok(ApiResponse<PoliticaSeguridadDto>.SuccessResponse(opciones.Value.ToDto()));

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
}
