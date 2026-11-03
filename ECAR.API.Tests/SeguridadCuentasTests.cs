using System.IdentityModel.Tokens.Jwt;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ECAR.API.Configuration;
using ECAR.API.Controllers;
using ECAR.API.Exceptions;
using ECAR.API.Services;
using ECAR.Infrastructure.Data;
using ECAR.Infrastructure.Entities;
using ECAR.Shared;
using ECAR.Shared.DTOs;
using ECAR.Shared.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECAR.API.Tests;

public class SeguridadCuentasTests
{
    private const string Correo = "cuenta@ecar.test";

    private sealed class ContextoPruebas(IMotivoCambio motivo) : IContextoAuditoria
    {
        public long? IdUsuario => 1;
        public string Usuario => "Administrador (admin@ecar.test)";
        public string? DireccionIp => "127.0.0.1";
        public string? AgenteUsuario => "pruebas";
        public string? Motivo => motivo.Motivo;
    }

    private static (ECARDbContext Context, MotivoCambio Motivo) CrearContexto()
    {
        var motivo = new MotivoCambio();
        var options = new DbContextOptionsBuilder<ECARDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return (new ECARDbContext(options, new ContextoPruebas(motivo)), motivo);
    }

    private static async Task<Usuario> CrearUsuarioAsync(ECARDbContext context)
    {
        var rol = new Rol { Nombre = "Operador" };
        var usuario = new Usuario
        {
            Nombre = "Cuenta de Prueba",
            Correo = Correo,
            PasswordHash = FirmaPruebas.PasswordHash,
            FechaCambioPassword = DateTime.UtcNow,
            Activo = true
        };
        usuario.UsuarioRoles.Add(new UsuarioRol { Rol = rol });
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();
        return usuario;
    }

    private static LoginDto Login(string password) => new() { CorreoOrUsuarioAD = Correo, Password = password };

    private static UsuariosController ControladorUsuarios(ECARDbContext context) =>
        new(context, new PoliticaPasswords(Options.Create(new SeguridadOptions())));

    [Fact]
    public async Task CincoIntentosFallidos_BloqueanLaCuentaYDevuelven423()
    {
        var (context, _) = CrearContexto();
        await using var _ = context;
        var usuario = await CrearUsuarioAsync(context);
        var controller = new AuthController(FirmaPruebas.CrearAuthService(context));

        for (var intento = 1; intento <= 4; intento++)
        {
            var fallido = await controller.Login(Login("incorrecta" + intento));
            Assert.IsType<UnauthorizedObjectResult>(fallido.Result);
        }

        var quinto = await controller.Login(Login("incorrecta5"));
        Assert.Equal(StatusCodes.Status423Locked, Assert.IsType<ObjectResult>(quinto.Result).StatusCode);

        var correcta = await controller.Login(Login(FirmaPruebas.Password));
        Assert.Equal(StatusCodes.Status423Locked, Assert.IsType<ObjectResult>(correcta.Result).StatusCode);

        var guardado = await context.Usuarios.AsNoTracking().SingleAsync(u => u.IdUsuario == usuario.IdUsuario);
        Assert.True(guardado.BloqueadoHasta > DateTime.UtcNow.AddMinutes(29));

        var acciones = await context.Auditoria.AsNoTracking().Select(a => a.Accion).ToListAsync();
        Assert.Equal(6, acciones.Count(a => a == AuditoriaAcciones.LoginFallido));
        Assert.Single(acciones, AuditoriaAcciones.CuentaBloqueada);
    }

    [Fact]
    public async Task UnIngresoCorrecto_ReiniciaLosIntentosFallidos()
    {
        var (context, _) = CrearContexto();
        await using var _ = context;
        await CrearUsuarioAsync(context);
        var servicio = FirmaPruebas.CrearAuthService(context);

        for (var intento = 0; intento < 4; intento++)
        {
            Assert.Null(await servicio.LoginAsync(Login("incorrecta")));
        }

        Assert.NotNull(await servicio.LoginAsync(Login(FirmaPruebas.Password)));
        Assert.Null(await servicio.LoginAsync(Login("incorrecta")));
        Assert.Equal(1, (await context.Usuarios.AsNoTracking().SingleAsync()).IntentosFallidos);
    }

    [Fact]
    public async Task Desbloquear_ExigeMotivoQuedaAuditadoYPermiteEntrar()
    {
        var (context, motivo) = CrearContexto();
        await using var _ = context;
        var usuario = await CrearUsuarioAsync(context);
        var servicio = FirmaPruebas.CrearAuthService(context);
        for (var intento = 0; intento < 4; intento++)
        {
            await servicio.LoginAsync(Login("incorrecta"));
        }

        await Assert.ThrowsAsync<CuentaBloqueadaException>(() => servicio.LoginAsync(Login("incorrecta")));
        context.ChangeTracker.Clear();

        var resultado = await ControladorUsuarios(context).DesbloquearUsuario(usuario.IdUsuario,
            new MotivoDto { Motivo = "El usuario confirmó su identidad por teléfono" }, motivo);

        Assert.IsType<OkObjectResult>(resultado.Result);
        var fila = await context.Auditoria.AsNoTracking()
            .SingleAsync(a => a.Tabla == "Usuarios" && a.Accion == AuditoriaAcciones.Modificar && a.Motivo != null);
        Assert.Equal("El usuario confirmó su identidad por teléfono", fila.Motivo);
        Assert.Contains(nameof(Usuario.BloqueadoHasta), fila.ValorNuevo);

        context.ChangeTracker.Clear();
        Assert.NotNull(await servicio.LoginAsync(Login(FirmaPruebas.Password)));
    }

    [Fact]
    public async Task Desbloquear_UnaCuentaSinBloqueoDevuelve400()
    {
        var (context, motivo) = CrearContexto();
        await using var _ = context;
        var usuario = await CrearUsuarioAsync(context);

        var resultado = await ControladorUsuarios(context).DesbloquearUsuario(usuario.IdUsuario,
            new MotivoDto { Motivo = "Solicitud del área de calidad" }, motivo);

        Assert.IsType<BadRequestObjectResult>(resultado.Result);
    }

    [Fact]
    public void LosDtoDeUsuario_NoImponenLaReglaAntiguaDeSeisCaracteres()
    {
        var crear = typeof(CreateUsuarioDto).GetProperty(nameof(CreateUsuarioDto.Password))!;
        var actualizar = typeof(UpdateUsuarioDto).GetProperty(nameof(UpdateUsuarioDto.Password))!;

        Assert.DoesNotContain(crear.GetCustomAttributes(true), atributo => atributo is MinLengthAttribute);
        Assert.DoesNotContain(actualizar.GetCustomAttributes(true), atributo => atributo is MinLengthAttribute);
    }

    /// <summary>
    /// 03/11/2026: el cambio y el restablecimiento de contraseñas los hará una API externa. Solo queda el
    /// desbloqueo por el Administrador.
    /// </summary>
    [Fact]
    public void No_hay_rutas_para_cambiar_ni_restablecer_contrasenas()
    {
        var rutas = typeof(AuthController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t))
            .SelectMany(t => t.GetMethods())
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>(inherit: true))
            .Select(a => a.Template ?? string.Empty)
            .ToList();

        Assert.DoesNotContain(rutas, r => r.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("{id}/desbloquear", rutas);
    }

    [Fact]
    public async Task Una_password_antigua_o_marcada_como_temporal_entra_sin_pedir_cambiarla()
    {
        var (context, _) = CrearContexto();
        await using var _ = context;
        var usuario = await CrearUsuarioAsync(context);
        // Como quedó una cuenta restablecida antes del cambio: temporal y de hace más de un año.
        usuario.DebeCambiarPassword = true;
        usuario.FechaCambioPassword = DateTime.UtcNow.AddDays(-400);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var login = await FirmaPruebas.CrearAuthService(context).LoginAsync(Login(FirmaPruebas.Password));

        Assert.NotNull(login);
        // El token no lleva la marca de cambio pendiente que antes dejaba todo en 403.
        var token = new JwtSecurityTokenHandler().ReadJwtToken(login!.Token);
        Assert.DoesNotContain(token.Claims, c => c.Type == "ecar_cambio_password");
    }

    [Fact]
    public async Task Editar_un_usuario_no_cambia_su_password()
    {
        var (context, _) = CrearContexto();
        await using var _ = context;
        var usuario = await CrearUsuarioAsync(context);

        var resultado = await ControladorUsuarios(context).UpdateUsuario(usuario.IdUsuario,
            new UpdateUsuarioDto { Password = "OtraClave2026" });

        var error = Assert.IsType<ApiResponse<UsuarioDto>>(Assert.IsType<BadRequestObjectResult>(resultado.Result).Value);
        Assert.Equal(UsuariosController.MensajeSinCambioPassword, error.Message);
        Assert.Equal(FirmaPruebas.PasswordHash, (await context.Usuarios.AsNoTracking().SingleAsync()).PasswordHash);
    }

    [Fact]
    public async Task CrearUsuario_con_password_no_la_deja_temporal_y_entra_sin_cambiarla()
    {
        var (context, _) = CrearContexto();
        await using var _ = context;

        var resultado = await ControladorUsuarios(context).CreateUsuario(new CreateUsuarioDto
        {
            Nombre = "Nuevo",
            Correo = "nuevo@ecar.test",
            Password = "ClaveInicial2026"
        });

        Assert.IsType<CreatedAtActionResult>(resultado.Result);
        var guardado = await context.Usuarios.AsNoTracking().SingleAsync(u => u.Correo == "nuevo@ecar.test");
        Assert.False(guardado.DebeCambiarPassword);

        context.ChangeTracker.Clear();
        var login = await FirmaPruebas.CrearAuthService(context).LoginAsync(new LoginDto
        {
            CorreoOrUsuarioAD = "nuevo@ecar.test",
            Password = "ClaveInicial2026"
        });
        Assert.NotNull(login);
    }

    [Fact]
    public async Task Desactivar_InvalidaLasSesionesAnteriores()
    {
        var (context, _) = CrearContexto();
        await using var _ = context;
        var usuario = await CrearUsuarioAsync(context);
        var admin = new Usuario { Nombre = "Admin", Correo = "admin@ecar.test", PasswordHash = "x", Activo = true };
        admin.UsuarioRoles.Add(new UsuarioRol { Rol = new Rol { Nombre = "Administrador" } });
        context.Usuarios.Add(admin);
        await context.SaveChangesAsync();
        var selloAnterior = usuario.SecurityStamp;

        await ControladorUsuarios(context).DeleteUsuario(usuario.IdUsuario, "Baja por fin del contrato del técnico");

        Assert.NotEqual(selloAnterior, (await context.Usuarios.AsNoTracking().SingleAsync(u => u.IdUsuario == usuario.IdUsuario)).SecurityStamp);
    }

    [Fact]
    public async Task CrearUsuario_RechazaUnaPasswordQueNoCumpleLaPolitica()
    {
        var (context, _) = CrearContexto();
        await using var _ = context;

        var resultado = await ControladorUsuarios(context).CreateUsuario(new CreateUsuarioDto
        {
            Nombre = "Nuevo",
            Correo = "nuevo@ecar.test",
            Password = "123"
        });

        Assert.IsType<BadRequestObjectResult>(resultado.Result);
    }
}
