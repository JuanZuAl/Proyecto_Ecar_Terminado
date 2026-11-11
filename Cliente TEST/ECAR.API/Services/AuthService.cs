using BCrypt.Net;
using ECAR.API.Configuration;
using ECAR.API.Exceptions;
using ECAR.API.Services.SgiIdentity;
using ECAR.Infrastructure.Data;
using ECAR.Infrastructure.Entities;
using ECAR.Shared;
using ECAR.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ECAR.API.Services;

/// <summary>
/// Inicio de sesión y confirmación de identidad para firmar. Según ECARAuthentication:Mode, la contraseña se comprueba
/// en ECAR (Local), en Active Directory (ActiveDirectory, Hybrid) o en SGI.Identity (SgiIdentity).
/// </summary>
/// <remarks>
/// En modo SgiIdentity, SGI.Identity valida la contraseña, la bloquea, la hace caducar y la cambia; ECAR crea su
/// propia sesión (el JWT) y decide los permisos con los grupos que devuelve el servicio (guía §4.3 y §4.4). Las
/// cuentas locales (con contraseña guardada en ECAR, como admin@ecar.com) siguen entrando como antes mientras
/// SgiIdentity:PermitirCuentasLocales sea true.
/// </remarks>
public class AuthService : IAuthService
{
    public const string OperacionLogin = "InicioSesion";
    public const string OperacionFirma = "Firma";

    public const string FuenteSgi = "SgiIdentity";

    public const string MensajeServicioNoDisponible =
        "El servicio de autenticación (SGI.Identity) no está disponible en este momento. Intente de nuevo en unos minutos.";

    public const string MensajeClienteRechazado =
        "No fue posible validar sus credenciales: el servicio de autenticación rechazó la conexión de ECAR. Avise a Soporte T.I.";

    public const string MensajeSinGrupoConRol =
        "Su usuario se validó en SGI.Identity, pero no pertenece a ningún grupo con rol en ECAR. " +
        "Solicite a Soporte T.I. que lo agreguen al grupo que corresponda.";

    public const string MensajeCuentasLocalesDeshabilitadas =
        "Las cuentas locales de ECAR están deshabilitadas: entre con su usuario corporativo.";

    private readonly ECARDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IActiveDirectoryAuthService _activeDirectoryAuthService;
    private readonly EcarAuthenticationOptions _authenticationOptions;
    private readonly PoliticaPasswords _politica;
    private readonly ISgiIdentityClient? _sgi;
    private readonly SgiIdentityOptions _sgiOpciones;
    private readonly PaseCambioPassword? _pase;
    private readonly IMotivoCambio? _motivoCambio;
    private readonly AutorAuditoria? _autor;

    public AuthService(
        ECARDbContext context,
        IConfiguration configuration,
        IActiveDirectoryAuthService activeDirectoryAuthService,
        IOptions<EcarAuthenticationOptions> authenticationOptions,
        PoliticaPasswords? politica = null,
        ISgiIdentityClient? sgi = null,
        IOptions<SgiIdentityOptions>? sgiOpciones = null,
        PaseCambioPassword? pase = null,
        IMotivoCambio? motivoCambio = null,
        AutorAuditoria? autor = null)
    {
        _context = context;
        _configuration = configuration;
        _activeDirectoryAuthService = activeDirectoryAuthService;
        _authenticationOptions = authenticationOptions.Value;
        _politica = politica ?? new PoliticaPasswords(Options.Create(new SeguridadOptions()));
        _sgi = sgi;
        _sgiOpciones = sgiOpciones?.Value ?? new SgiIdentityOptions();
        _pase = pase;
        _motivoCambio = motivoCambio;
        _autor = autor;
    }

    private bool ModoSgi => _authenticationOptions.GetMode() == EcarAuthenticationMode.SgiIdentity;

    /// <summary>Una cuenta con contraseña guardada en ECAR (no la valida SGI.Identity).</summary>
    private static bool EsCuentaLocal(Usuario? usuario) => !string.IsNullOrWhiteSpace(usuario?.PasswordHash);

    public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var identity = loginDto.CorreoOrUsuarioAD.Trim();

        // Un usuario puede iniciar sesión con su correo o con su usuario de Active Directory (o de SGI.Identity).
        var usuario = await _context.Usuarios
            .Include(u => u.UsuarioRoles)
            .ThenInclude(ur => ur.Rol)
            .FirstOrDefaultAsync(u =>
                u.Correo == identity ||
                u.UsuarioAD == identity);

        if (ModoSgi)
        {
            if (!EsCuentaLocal(usuario))
            {
                // Un usuario ya registrado puede escribir su correo: a SGI.Identity se le envía su usuario de SGI.
                var usuarioSgi = usuario is { UsuarioAD: { Length: > 0 } registrado } ? registrado : identity;
                return await LoginSgiAsync(usuarioSgi, loginDto.Password);
            }

            if (!_sgiOpciones.PermitirCuentasLocales)
            {
                await _context.RegistrarEventoAsync(AuditoriaAcciones.LoginFallido, usuario!.IdUsuario,
                    new { Operacion = OperacionLogin, Detalle = "Cuenta local con las cuentas locales deshabilitadas" },
                    usuario: Describir(usuario), idUsuario: usuario.IdUsuario);
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden, MensajeCuentasLocalesDeshabilitadas);
            }
        }

        if (usuario == null || !usuario.Activo)
        {
            await _context.RegistrarEventoAsync(AuditoriaAcciones.LoginFallido, usuario?.IdUsuario ?? 0,
                new { Operacion = OperacionLogin, Identificador = identity, Detalle = "Usuario inexistente o inactivo" },
                usuario: identity, idUsuario: usuario?.IdUsuario);
            return null;
        }

        var autenticacion = await VerificarAsync(usuario, loginDto.Password, OperacionLogin);
        if (autenticacion == MetodoAutenticacion.Ninguno)
        {
            return null;
        }

        // Desde el 03/11/2026 no se obliga a cambiar la contraseña al entrar (ni temporal ni caducada): el cambio
        // periódico y el restablecimiento los hace una API externa (SGI.Identity). DebeCambiarPassword ya no se usa.
        usuario.UltimoAcceso = DateTime.UtcNow;
        _context.RegistrarEvento(AuditoriaAcciones.LoginExitoso, usuario.IdUsuario,
            new { Metodo = autenticacion.ToString() },
            usuario: Describir(usuario), idUsuario: usuario.IdUsuario);
        await _context.SaveChangesAsync();

        return CrearSesion(usuario, usuario.UsuarioRoles.Select(ur => ur.Rol.Nombre).ToList());
    }

    public async Task<bool> VerificarPasswordAsync(long idUsuario, string? password, string operacion,
        IReadOnlyCollection<string>? rolesPermitidos = null)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
        if (usuario == null || !usuario.Activo)
        {
            return false;
        }

        if (ModoSgi)
        {
            if (!EsCuentaLocal(usuario))
            {
                return await VerificarConSgiAsync(usuario, password ?? string.Empty, operacion, rolesPermitidos);
            }

            if (!_sgiOpciones.PermitirCuentasLocales)
            {
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden, MensajeCuentasLocalesDeshabilitadas);
            }
        }

        return await VerificarAsync(usuario, password, operacion) != MetodoAutenticacion.Ninguno;
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtSecret = _configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT:Secret no está configurado (user-secrets o variable de entorno JWT__Secret)");
            var key = Encoding.UTF8.GetBytes(jwtSecret);

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    // ---------------------------------------------------------------- SGI.Identity

    private ISgiIdentityClient Sgi => _sgi ?? throw new InvalidOperationException("SGI.Identity no está configurado");

    private async Task<LoginResponseDto?> LoginSgiAsync(string identificador, string password)
    {
        if (identificador.Length == 0 || identificador.Length > 100)
        {
            return null;
        }

        var resultado = await Sgi.LoginAsync(identificador, password);
        var usuarioSgi = resultado.Usuario?.UserName ?? identificador;
        var local = await BuscarUsuarioSgiAsync(usuarioSgi);

        switch (resultado.Estado)
        {
            case EstadoLoginSgi.Autenticado:
                return await CompletarLoginSgiAsync(resultado.Usuario!, local);

            case EstadoLoginSgi.CredencialesInvalidas:
                await RegistrarFalloSgiAsync(OperacionLogin, identificador, local, "InvalidCredentials");
                return null;

            case EstadoLoginSgi.UsuarioBloqueado:
                await RegistrarFalloSgiAsync(OperacionLogin, identificador, local, "UserLocked");
                throw new AutenticacionRechazadaException(StatusCodes.Status423Locked, resultado.Mensaje is { Length: > 0 } bloqueo
                    ? bloqueo
                    : "El usuario se encuentra bloqueado. Por favor cambie su contraseña o contacte a Soporte T.I.");

            case EstadoLoginSgi.PasswordVencida:
                await RegistrarFalloSgiAsync(OperacionLogin, identificador, local, "PasswordExpired");
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                    resultado.Mensaje is { Length: > 0 } vencida ? vencida : "La contraseña se encuentra vencida.",
                    PedirAccion(identificador, AccionesLogin.CambiarPassword));

            case EstadoLoginSgi.RequiereCambioPassword:
                // Igual que la vencida: se abre el cambio de contraseña, porque SGI.Identity lo pide.
                await RegistrarFalloSgiAsync(OperacionLogin, identificador, local, "RequiresPasswordChange");
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                    resultado.Mensaje is { Length: > 0 } cambio ? cambio : "Debe cambiar su contraseña para continuar.",
                    PedirAccion(identificador, AccionesLogin.CambiarPassword));

            case EstadoLoginSgi.RequiereConfigurarPassword:
                await RegistrarFalloSgiAsync(OperacionLogin, identificador, local, "RequiresInitialPasswordSetup");
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                    resultado.Mensaje is { Length: > 0 } inicial ? inicial : "Debe configurar su contraseña para continuar.",
                    PedirAccion(identificador, AccionesLogin.ConfigurarPasswordInicial));

            case EstadoLoginSgi.UsuarioInactivo:
                await RegistrarFalloSgiAsync(OperacionLogin, identificador, local, "UserInactive");
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                    resultado.Mensaje is { Length: > 0 } inactivo ? inactivo : "El usuario se encuentra inactivo. Contacte a Soporte T.I.");

            case EstadoLoginSgi.UsuarioSinGrupos:
                await RegistrarFalloSgiAsync(OperacionLogin, identificador, local, "UserWithoutGroups");
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden, MensajeSinGrupoConRol);

            case EstadoLoginSgi.ClienteRechazado:
                await RegistrarFalloSgiAsync(OperacionLogin, identificador, local, "ClienteRechazado");
                throw new AutenticacionRechazadaException(StatusCodes.Status503ServiceUnavailable, MensajeClienteRechazado);

            default:
                await RegistrarFalloSgiAsync(OperacionLogin, identificador, local, "NoDisponible");
                throw new AutenticacionRechazadaException(StatusCodes.Status503ServiceUnavailable, MensajeServicioNoDisponible);
        }
    }

    /// <summary>
    /// SGI.Identity validó al usuario: se le dan los roles de sus grupos, se registra en ECAR si es su primera vez y
    /// se crea la sesión. Sin un grupo con rol en ECAR no entra (guía §8, reglas 4 y 5).
    /// </summary>
    private async Task<LoginResponseDto> CompletarLoginSgiAsync(UsuarioSgi sgi, Usuario? local)
    {
        _autor?.Establecer($"{sgi.UserName} (SGI.Identity)", local?.IdUsuario);
        var roles = MapeoGruposRoles.Roles(sgi.Grupos, _sgiOpciones.Grupos);

        if (roles.Count == 0)
        {
            await RegistrarFalloSgiAsync(OperacionLogin, sgi.UserName, local, "SinRolEnEcar", sgi.Grupos);
            throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden, MensajeSinGrupoConRol);
        }

        // El nombre de la persona lo envía SGI.Identity (displayName): es el que queda en sus firmas. Si no llega,
        // se conserva el que haya (para un usuario nuevo, su usuario, hasta que el Administrador lo complete).
        var nombreSgi = sgi.Nombre is { Length: > 0 } enviado ? enviado[..Math.Min(enviado.Length, 200)].Trim() : null;
        var motivos = new List<string>();
        if (local == null)
        {
            if (!_sgiOpciones.CrearUsuariosAlEntrar)
            {
                await RegistrarFalloSgiAsync(OperacionLogin, sgi.UserName, null, "NoRegistradoEnEcar", sgi.Grupos);
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                    "Su usuario no está registrado en ECAR. Solicite el alta al Administrador.");
            }

            var correo = CorreoPara(sgi.UserName);
            if (await _context.Usuarios.AnyAsync(u => u.Correo == correo))
            {
                await RegistrarFalloSgiAsync(OperacionLogin, sgi.UserName, null, "CorreoEnUso", sgi.Grupos);
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                    $"Ya hay en ECAR otro usuario con el correo {correo}. Solicite al Administrador que le asigne el usuario «{sgi.UserName}».");
            }

            local = new Usuario
            {
                Nombre = nombreSgi ?? sgi.UserName,
                Correo = correo,
                UsuarioAD = sgi.UserName,
                PasswordHash = string.Empty,
                Activo = true
            };
            _context.Usuarios.Add(local);
            motivos.Add("Alta automática: primer inicio de sesión validado por SGI.Identity");
        }
        else if (!local.Activo)
        {
            await RegistrarFalloSgiAsync(OperacionLogin, sgi.UserName, local, "DesactivadoEnEcar", sgi.Grupos);
            throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                "Su usuario está desactivado en ECAR. Comuníquese con el Administrador.");
        }

        // SGI.Identity manda también en el nombre: en cada inicio de sesión queda el que envía.
        if (nombreSgi != null && local.IdUsuario != 0 && !string.Equals(local.Nombre, nombreSgi, StringComparison.Ordinal))
        {
            local.Nombre = nombreSgi;
            motivos.Add("Nombre según SGI.Identity");
        }

        // Los grupos de SGI.Identity mandan: en cada inicio de sesión los roles quedan como dicen ellos.
        var actuales = local.UsuarioRoles.Select(ur => ur.Rol?.Nombre).OfType<string>().ToList();
        if (!actuales.ToHashSet().SetEquals(roles))
        {
            var rolesBase = await _context.Roles.Where(r => roles.Contains(r.Nombre)).ToListAsync();
            _context.UsuarioRoles.RemoveRange(local.UsuarioRoles.Where(ur => ur.Rol == null || !roles.Contains(ur.Rol.Nombre)).ToList());
            foreach (var rol in rolesBase.Where(r => !actuales.Contains(r.Nombre)))
            {
                local.UsuarioRoles.Add(new UsuarioRol { Usuario = local, Rol = rol, IdRol = rol.IdRol });
            }

            if (local.IdUsuario != 0)
            {
                // Las sesiones abiertas con los roles anteriores dejan de valer (SelloSeguridad).
                local.SecurityStamp = Usuario.NuevoSecurityStamp();
                motivos.Add($"Roles según los grupos de SGI.Identity: {string.Join(", ", roles)}");
            }
        }

        local.UltimoAcceso = DateTime.UtcNow;
        if (motivos.Count > 0)
        {
            _motivoCambio?.Establecer(string.Join(". ", motivos));
            await _context.SaveChangesAsync();
            _motivoCambio?.Establecer(null);
        }

        _autor?.Establecer($"{sgi.UserName} (SGI.Identity)", local.IdUsuario);
        _context.RegistrarEvento(AuditoriaAcciones.LoginExitoso, local.IdUsuario,
            new { Metodo = FuenteSgi, sgi.Fuente, Grupos = sgi.Grupos, Roles = roles },
            usuario: Describir(local), idUsuario: local.IdUsuario);
        await _context.SaveChangesAsync();

        return CrearSesion(local, roles);
    }

    /// <summary>
    /// Confirmación de identidad para firmar una inspección o resolver una novedad (Parte 11 §11.200): la contraseña
    /// la valida SGI.Identity en ese momento, y sus grupos deben seguir dando un rol que permita la firma.
    /// </summary>
    private async Task<bool> VerificarConSgiAsync(Usuario usuario, string password, string operacion,
        IReadOnlyCollection<string>? rolesPermitidos)
    {
        if (string.IsNullOrWhiteSpace(usuario.UsuarioAD))
        {
            throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                "Su usuario de ECAR no tiene un usuario de SGI.Identity asociado. Comuníquese con el Administrador.");
        }

        var resultado = await Sgi.LoginAsync(usuario.UsuarioAD, password);
        switch (resultado.Estado)
        {
            case EstadoLoginSgi.Autenticado:
                if (rolesPermitidos is { Count: > 0 } &&
                    !MapeoGruposRoles.Roles(resultado.Usuario!.Grupos, _sgiOpciones.Grupos).Any(rolesPermitidos.Contains))
                {
                    await RegistrarFalloSgiAsync(operacion, usuario.UsuarioAD, usuario, "SinRolParaFirmar", resultado.Usuario!.Grupos);
                    throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                        "Sus grupos de SGI.Identity ya no le dan un rol que permita esta firma. Cierre sesión y vuelva a " +
                        "entrar, o consulte a Soporte T.I.");
                }

                return true;

            case EstadoLoginSgi.CredencialesInvalidas:
                await RegistrarFalloSgiAsync(operacion, usuario.UsuarioAD, usuario, "InvalidCredentials");
                return false;

            case EstadoLoginSgi.UsuarioBloqueado:
                await RegistrarFalloSgiAsync(operacion, usuario.UsuarioAD, usuario, "UserLocked");
                throw new AutenticacionRechazadaException(StatusCodes.Status423Locked, resultado.Mensaje is { Length: > 0 } bloqueo
                    ? bloqueo
                    : "El usuario se encuentra bloqueado. Contacte a Soporte T.I.");

            case EstadoLoginSgi.PasswordVencida:
                await RegistrarFalloSgiAsync(operacion, usuario.UsuarioAD, usuario, "PasswordExpired");
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                    "Su contraseña está vencida. Cierre sesión y vuelva a entrar: el sistema le pedirá cambiarla antes de firmar.");

            case EstadoLoginSgi.RequiereCambioPassword:
                await RegistrarFalloSgiAsync(operacion, usuario.UsuarioAD, usuario, "RequiresPasswordChange");
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                    "SGI.Identity pide cambiar su contraseña. Cierre sesión y vuelva a entrar: el sistema le pedirá cambiarla antes de firmar.");

            case EstadoLoginSgi.RequiereConfigurarPassword:
                await RegistrarFalloSgiAsync(operacion, usuario.UsuarioAD, usuario, "RequiresInitialPasswordSetup");
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                    "SGI.Identity pide configurar la contraseña de su cuenta. Cierre sesión y vuelva a entrar para hacerlo.");

            case EstadoLoginSgi.UsuarioInactivo:
                await RegistrarFalloSgiAsync(operacion, usuario.UsuarioAD, usuario, "UserInactive");
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                    "Su usuario está inactivo en SGI.Identity. No se firmó nada. Contacte a Soporte T.I.");

            case EstadoLoginSgi.UsuarioSinGrupos:
                await RegistrarFalloSgiAsync(operacion, usuario.UsuarioAD, usuario, "UserWithoutGroups");
                throw new AutenticacionRechazadaException(StatusCodes.Status403Forbidden,
                    "Sus grupos de SGI.Identity ya no le dan un rol que permita esta firma. Cierre sesión y vuelva a " +
                    "entrar, o consulte a Soporte T.I.");

            case EstadoLoginSgi.ClienteRechazado:
                await RegistrarFalloSgiAsync(operacion, usuario.UsuarioAD, usuario, "ClienteRechazado");
                throw new AutenticacionRechazadaException(StatusCodes.Status503ServiceUnavailable,
                    "No fue posible verificar su contraseña: el servicio de autenticación rechazó la conexión de ECAR. " +
                    "No se firmó nada. Avise a Soporte T.I.");

            default:
                await RegistrarFalloSgiAsync(operacion, usuario.UsuarioAD, usuario, "NoDisponible");
                throw new AutenticacionRechazadaException(StatusCodes.Status503ServiceUnavailable,
                    "No fue posible verificar su contraseña: el servicio de autenticación no está disponible. " +
                    "No se firmó nada; intente de nuevo en unos minutos.");
        }
    }

    /// <summary>El usuario de ECAR enlazado con este usuario de SGI.Identity (sin contraseña local).</summary>
    private Task<Usuario?> BuscarUsuarioSgiAsync(string usuarioSgi) =>
        _context.Usuarios
            .Include(u => u.UsuarioRoles)
            .ThenInclude(ur => ur.Rol)
            .Where(u => u.PasswordHash == null || u.PasswordHash == string.Empty)
            .FirstOrDefaultAsync(u => u.UsuarioAD == usuarioSgi || u.Correo == usuarioSgi);

    private LoginResponseDto PedirAccion(string usuario, string accion) => new()
    {
        AccionRequerida = accion,
        PaseCambioPassword = _pase?.Crear(usuario, accion),
        Usuario = usuario
    };

    private string CorreoPara(string usuarioSgi) =>
        new EmailAddressAttribute().IsValid(usuarioSgi) && usuarioSgi.Length <= 150
            ? usuarioSgi
            : $"{usuarioSgi}@{_sgiOpciones.DominioCorreo.Trim()}";

    /// <summary>
    /// Un intento fallido contra SGI.Identity queda en la auditoría con el estado del servicio, nunca con la
    /// contraseña. El bloqueo por intentos lo lleva SGI.Identity, no ECAR.
    /// </summary>
    private Task RegistrarFalloSgiAsync(string operacion, string identificador, Usuario? local, string estado,
        IReadOnlyList<string>? grupos = null) =>
        _context.RegistrarEventoAsync(AuditoriaAcciones.LoginFallido, local?.IdUsuario ?? 0,
            new { Operacion = operacion, Identificador = identificador, Fuente = FuenteSgi, Estado = estado, Grupos = grupos },
            usuario: local is null ? identificador : Describir(local), idUsuario: local?.IdUsuario);

    // ---------------------------------------------------------------- Cuentas locales y Active Directory

    private async Task<MetodoAutenticacion> VerificarAsync(Usuario usuario, string? password, string operacion)
    {
        var ahora = DateTime.UtcNow;
        if (usuario.BloqueadoHasta is { } bloqueadoHasta && bloqueadoHasta > ahora)
        {
            await _context.RegistrarEventoAsync(AuditoriaAcciones.LoginFallido, usuario.IdUsuario,
                new { Operacion = operacion, Detalle = "Cuenta bloqueada" },
                usuario: Describir(usuario), idUsuario: usuario.IdUsuario);
            throw new CuentaBloqueadaException(bloqueadoHasta);
        }

        var metodo = await AutenticarAsync(usuario, password ?? string.Empty);
        if (metodo != MetodoAutenticacion.Ninguno)
        {
            if (usuario.IntentosFallidos != 0 || usuario.BloqueadoHasta != null)
            {
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = null;
                await _context.SaveChangesAsync();
            }

            return metodo;
        }

        var opciones = _politica.Opciones;
        usuario.IntentosFallidos++;
        _context.RegistrarEvento(AuditoriaAcciones.LoginFallido, usuario.IdUsuario,
            new { Operacion = operacion, Intento = usuario.IntentosFallidos, Maximo = opciones.MaxIntentosFallidos },
            usuario: Describir(usuario), idUsuario: usuario.IdUsuario);

        if (usuario.IntentosFallidos < opciones.MaxIntentosFallidos)
        {
            await _context.SaveChangesAsync();
            return MetodoAutenticacion.Ninguno;
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = ahora.AddMinutes(opciones.MinutosBloqueo);
        _context.RegistrarEvento(AuditoriaAcciones.CuentaBloqueada, usuario.IdUsuario,
            new { Operacion = operacion, usuario.BloqueadoHasta, Intentos = opciones.MaxIntentosFallidos },
            usuario: Describir(usuario), idUsuario: usuario.IdUsuario);
        await _context.SaveChangesAsync();
        throw new CuentaBloqueadaException(usuario.BloqueadoHasta.Value);
    }

    private async Task<MetodoAutenticacion> AutenticarAsync(Usuario usuario, string password)
    {
        var mode = _authenticationOptions.GetMode();

        // En SgiIdentity solo llegan aquí las cuentas locales (con contraseña guardada en ECAR).
        if (mode is EcarAuthenticationMode.Local or EcarAuthenticationMode.Hybrid or EcarAuthenticationMode.SgiIdentity)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(usuario.PasswordHash) &&
                    BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
                {
                    return MetodoAutenticacion.Local;
                }
            }
            catch (SaltParseException)
            {
                // Un hash antiguo dañado debe fallar la autenticación en lugar de devolver HTTP 500.
            }
        }

        if (mode is EcarAuthenticationMode.ActiveDirectory or EcarAuthenticationMode.Hybrid &&
            !string.IsNullOrWhiteSpace(usuario.UsuarioAD) &&
            await _activeDirectoryAuthService.AuthenticateAsync(usuario.UsuarioAD, password))
        {
            return MetodoAutenticacion.ActiveDirectory;
        }

        return MetodoAutenticacion.Ninguno;
    }

    // ---------------------------------------------------------------- Sesión de ECAR

    private LoginResponseDto CrearSesion(Usuario usuario, List<string> roles) => new()
    {
        Token = GenerateJwtToken(usuario, roles),
        Correo = usuario.Correo,
        Nombre = usuario.Nombre,
        Roles = roles,
        Expiration = DateTime.UtcNow.AddHours(GetJwtExpirationHours())
    };

    private string GenerateJwtToken(Usuario usuario, List<string> roles)
    {
        var jwtSecret = _configuration["JWT:Secret"] ?? throw new InvalidOperationException("JWT:Secret no está configurado (user-secrets o variable de entorno JWT__Secret)");
        var key = Encoding.UTF8.GetBytes(jwtSecret);
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Email, usuario.Correo),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            // Si el sello cambia (desactivación, cambio de contraseña o de roles por SGI.Identity), el token deja de valer.
            new Claim(SelloSeguridad.TipoClaim, usuario.SecurityStamp)
        };

        // El usuario de la persona: con él la auditoría y el diálogo de firma dicen «Nombre (usuario)».
        if (!string.IsNullOrWhiteSpace(usuario.UsuarioAD))
        {
            claims.Add(new Claim(DescripcionUsuario.ClaimUsuario, usuario.UsuarioAD));
        }

        // ASP.NET lee estos claims cuando un endpoint exige un rol.
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:Issuer"],
            audience: _configuration["JWT:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(GetJwtExpirationHours()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private int GetJwtExpirationHours()
    {
        // 8 h, un turno (PLAN_FASE4_TAREAS §3.8); el cierre por inactividad lo hace el cliente.
        return int.TryParse(_configuration["JWT:ExpirationHours"], out var hours) ? hours : 8;
    }

    private static string Describir(Usuario usuario) =>
        DescripcionUsuario.Texto(usuario.Nombre, usuario.UsuarioAD, usuario.Correo);

    private enum MetodoAutenticacion
    {
        Ninguno,
        Local,
        ActiveDirectory
    }
}
