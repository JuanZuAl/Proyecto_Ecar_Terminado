using ECAR.API.Controllers;
using ECAR.API.Services;
using ECAR.Infrastructure.Data;
using ECAR.Infrastructure.Entities;
using ECAR.Shared;
using ECAR.Shared.DTOs;
using ECAR.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECAR.API.Tests;

public class BackendPhaseOneTests
{
    [Fact]
    public async Task ActiveDirectoryAdapterDoesNotConnectWhenDisabled()
    {
        var service = new LdapActiveDirectoryAuthService(
            Options.Create(new ActiveDirectoryOptions { Enabled = false }),
            NullLogger<LdapActiveDirectoryAuthService>.Instance);

        Assert.False(await service.AuthenticateAsync("usuario", "clave"));
    }

    [Fact]
    public async Task ActiveDirectoryUserCanBeCreatedWithoutLocalPassword()
    {
        await using var context = CreateContext();
        var rol = new Rol { Nombre = "Operador" };
        context.Roles.Add(rol);
        await context.SaveChangesAsync();
        var controller = Fabrica.Usuarios(context);

        var result = await controller.CreateUsuario(new CreateUsuarioDto
        {
            Nombre = "Usuario Directorio",
            Correo = "directorio@ecar.com",
            UsuarioAD = "usuario.directorio",
            RoleIds = [rol.IdRol]
        });

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(string.Empty, (await context.Usuarios.SingleAsync()).PasswordHash);
    }

    [Fact]
    public async Task UpdatingUserWithUnknownRoleDoesNotSavePartialChanges()
    {
        await using var context = CreateContext();
        var user = new Usuario
        {
            Nombre = "Nombre original",
            Correo = "usuario@ecar.com",
            PasswordHash = "hash",
            Activo = true
        };
        context.Usuarios.Add(user);
        await context.SaveChangesAsync();

        var controller = Fabrica.Usuarios(context);
        var result = await controller.UpdateUsuario(user.IdUsuario, new UpdateUsuarioDto
        {
            Nombre = "Nombre que no debe guardarse",
            RoleIds = [999]
        });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        context.ChangeTracker.Clear();
        Assert.Equal("Nombre original", (await context.Usuarios.SingleAsync()).Nombre);
    }

    [Fact]
    public async Task LastActiveAdministratorCannotBeDisabled()
    {
        await using var context = CreateContext();
        var adminRole = new Rol { Nombre = "Administrador" };
        var admin = new Usuario
        {
            Nombre = "Administrador",
            Correo = "admin@ecar.com",
            PasswordHash = "hash",
            Activo = true
        };
        admin.UsuarioRoles.Add(new UsuarioRol { Rol = adminRole });
        context.Usuarios.Add(admin);
        await context.SaveChangesAsync();

        var controller = Fabrica.Usuarios(context);
        var result = await controller.UpdateUsuario(admin.IdUsuario, new UpdateUsuarioDto { Activo = false });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UsuarioDto>>(badRequest.Value);
        Assert.Contains("último administrador", response.Message);
        Assert.True((await context.Usuarios.SingleAsync()).Activo);
    }

    [Fact]
    public async Task CategoryAssignedToEquipmentCannotBeDeleted()
    {
        await using var context = CreateContext();
        var category = new CategoriaEquipo { Nombre = "Laboratorio" };
        context.Equipos.Add(new Equipo
        {
            CodigoInterno = "EQ-001",
            ActivoFijo = "AF-001",
            NombreEquipo = "Balanza",
            Categoria = category
        });
        await context.SaveChangesAsync();

        var controller = new CategoriasEquipoController(context);
        var result = await controller.DeleteCategoriaEquipo(category.IdCategoria);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.True(await context.CategoriasEquipo.AnyAsync());
    }

    [Fact]
    public async Task UpdatingRoleAssignmentReturnsTheNewUserAndRole()
    {
        await using var context = CreateContext();
        var auditor = new Rol { Nombre = "Supervisor" };
        var tecnico = new Rol { Nombre = "Operador" };
        var firstUser = new Usuario
        {
            Nombre = "Primer usuario",
            Correo = "primero@ecar.com",
            PasswordHash = "hash",
            Activo = true
        };
        var secondUser = new Usuario
        {
            Nombre = "Segundo usuario",
            Correo = "segundo@ecar.com",
            PasswordHash = "hash",
            Activo = true
        };
        var assignment = new UsuarioRol { Usuario = firstUser, Rol = auditor };
        // El primer usuario tiene otro rol: pasar esta asignación al segundo no lo deja sin rol.
        var otroRol = new UsuarioRol { Usuario = firstUser, Rol = tecnico };
        context.AddRange(assignment, otroRol, secondUser);
        await context.SaveChangesAsync();

        var controller = new UsuariosRolController(context);
        var result = await controller.UpdateUsuarioRol(assignment.Id, new UpdateUsuarioRolDto
        {
            IdUsuario = secondUser.IdUsuario,
            IdRol = tecnico.IdRol
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<UsuarioRolDto>>(ok.Value);
        Assert.Equal("Segundo usuario", response.Data!.UsuarioNombre);
        Assert.Equal("Operador", response.Data.RolNombre);
    }

    // ------------------------------------------------------------------ siempre al menos un rol (08/11/2026)

    [Fact]
    public async Task No_se_crea_un_usuario_sin_roles()
    {
        await using var context = CreateContext();

        var result = await Fabrica.Usuarios(context).CreateUsuario(new CreateUsuarioDto
        {
            Nombre = "Sin rol",
            Correo = "sinrol@ecar.com",
            Password = "ClaveInicial2026"
        });

        var error = Assert.IsType<ApiResponse<UsuarioDto>>(Assert.IsType<BadRequestObjectResult>(result.Result).Value);
        Assert.Equal(UsuariosController.MensajeAlMenosUnRol, error.Message);
        Assert.False(await context.Usuarios.AnyAsync());
    }

    [Fact]
    public async Task Editar_un_usuario_no_le_quita_todos_los_roles()
    {
        await using var context = CreateContext();
        var usuario = new Usuario { Nombre = "Ana", Correo = "ana@ecar.com", PasswordHash = "hash", Activo = true };
        usuario.UsuarioRoles.Add(new UsuarioRol { Rol = new Rol { Nombre = "Operador" } });
        context.Usuarios.Add(usuario);
        await context.SaveChangesAsync();

        var result = await Fabrica.Usuarios(context).UpdateUsuario(usuario.IdUsuario, new UpdateUsuarioDto { RoleIds = [] });

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(1, await context.UsuarioRoles.CountAsync());
    }

    [Fact]
    public async Task No_se_quita_ni_se_pasa_a_otro_el_unico_rol_de_un_usuario()
    {
        await using var context = CreateContext();
        var operador = new Rol { Nombre = "Operador" };
        var estandar = new Rol { Nombre = "Estándar" };
        var ana = new Usuario { Nombre = "Ana", Correo = "ana@ecar.com", PasswordHash = "hash", Activo = true };
        var luis = new Usuario { Nombre = "Luis", Correo = "luis@ecar.com", PasswordHash = "hash", Activo = true };
        var unico = new UsuarioRol { Usuario = ana, Rol = operador };
        var primero = new UsuarioRol { Usuario = luis, Rol = operador };
        var segundo = new UsuarioRol { Usuario = luis, Rol = estandar };
        context.AddRange(unico, primero, segundo);
        await context.SaveChangesAsync();
        var controller = new UsuariosRolController(context);

        var quitarUnico = await controller.DeleteUsuarioRol(unico.Id);
        var pasarUnico = await controller.UpdateUsuarioRol(unico.Id, new UpdateUsuarioRolDto { IdUsuario = luis.IdUsuario, IdRol = estandar.IdRol });
        var cambiarDeRol = await controller.UpdateUsuarioRol(unico.Id, new UpdateUsuarioRolDto { IdUsuario = ana.IdUsuario, IdRol = estandar.IdRol });
        var quitarUnoDeDos = await controller.DeleteUsuarioRol(segundo.Id);

        Assert.Equal(UsuariosRolController.MensajeUnicoRol,
            Assert.IsType<ApiResponse<bool>>(Assert.IsType<BadRequestObjectResult>(quitarUnico.Result).Value).Message);
        Assert.IsType<BadRequestObjectResult>(pasarUnico.Result);
        // Cambiar el rol sin cambiar de usuario sí se puede, y quitar uno de dos también.
        Assert.IsType<OkObjectResult>(cambiarDeRol.Result);
        Assert.IsType<OkObjectResult>(quitarUnoDeDos.Result);
        Assert.Equal(1, await context.UsuarioRoles.CountAsync(ur => ur.IdUsuario == ana.IdUsuario));
        Assert.Equal(1, await context.UsuarioRoles.CountAsync(ur => ur.IdUsuario == luis.IdUsuario));
    }

    // ------------------------------------------------------------------ datos de ejemplo (08/11/2026)

    private static IConfiguration ConfiguracionSemilla() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["AdminPassword"] = "ClaveTemporalSegura123!" })
        .Build();

    [Fact]
    public async Task El_seeder_completa_los_roles_pero_no_rellena_catalogos_que_ya_tienen_datos()
    {
        await using var context = CreateContext();
        context.Roles.Add(new Rol { Nombre = "Supervisor" });
        context.CategoriasEquipo.Add(new CategoriaEquipo { Nombre = "Instrumentación" });
        context.Ubicaciones.Add(new Ubicacion { Planta = "Planta Principal", Area = "Producción" });
        await context.SaveChangesAsync();

        await DataSeeder.SeedDataAsync(context, ConfiguracionSemilla());
        await DataSeeder.SeedDataAsync(context, ConfiguracionSemilla());

        Assert.Equal(RolesSistema.DelSistema.OrderBy(r => r), (await context.Roles.Select(r => r.Nombre).ToListAsync()).OrderBy(r => r));
        Assert.Equal(1, await context.Usuarios.CountAsync(u => u.Correo == "admin@ecar.com"));
        Assert.Equal(1, await context.UsuarioRoles.CountAsync());
        Assert.Equal(1, await context.CategoriasEquipo.CountAsync());
        Assert.Equal(1, await context.Ubicaciones.CountAsync());
    }

    [Fact]
    public async Task Los_datos_de_ejemplo_se_crean_solo_en_una_base_vacia_y_lo_borrado_no_vuelve()
    {
        await using var context = CreateContext();

        await DataSeeder.SeedDataAsync(context, ConfiguracionSemilla());
        Assert.Equal(4, await context.CategoriasEquipo.CountAsync());
        Assert.Equal(3, await context.Ubicaciones.CountAsync());

        // El Administrador borra un ejemplo que no usa y el API se reinicia.
        context.Ubicaciones.Remove(await context.Ubicaciones.FirstAsync(u => u.Area == "Almacén"));
        context.CategoriasEquipo.Remove(await context.CategoriasEquipo.FirstAsync(c => c.Nombre == "Servicios Industriales"));
        await context.SaveChangesAsync();
        await DataSeeder.SeedDataAsync(context, ConfiguracionSemilla());

        Assert.Equal(3, await context.CategoriasEquipo.CountAsync());
        Assert.Equal(2, await context.Ubicaciones.CountAsync());
        Assert.False(await context.Ubicaciones.AnyAsync(u => u.Area == "Almacén"));
    }

    [Fact]
    public async Task El_seeder_renombra_Tecnico_y_Auditor_y_sus_usuarios_conservan_el_rol()
    {
        await using var context = CreateContext();
        var tecnico = new Rol { Nombre = RolesSistema.TecnicoAnterior };
        var auditor = new Rol { Nombre = RolesSistema.AuditorAnterior };
        var ana = new Usuario { Nombre = "Ana", Correo = "ana@ecar.test", PasswordHash = "x" };
        var raul = new Usuario { Nombre = "Raúl", Correo = "raul@ecar.test", PasswordHash = "x" };
        ana.UsuarioRoles.Add(new UsuarioRol { Rol = tecnico });
        raul.UsuarioRoles.Add(new UsuarioRol { Rol = auditor });
        context.AddRange(ana, raul);
        await context.SaveChangesAsync();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AdminPassword"] = "ClaveTemporalSegura123!" })
            .Build();

        await DataSeeder.SeedDataAsync(context, configuration);
        await DataSeeder.SeedDataAsync(context, configuration);

        Assert.Equal(RolesSistema.DelSistema.OrderBy(r => r), (await context.Roles.Select(r => r.Nombre).ToListAsync()).OrderBy(r => r));
        string RolDe(Usuario u) => context.UsuarioRoles.Include(ur => ur.Rol).Single(ur => ur.IdUsuario == u.IdUsuario).Rol.Nombre;
        Assert.Equal(RolesSistema.Operador, RolDe(ana));
        Assert.Equal(RolesSistema.Supervisor, RolDe(raul));
        Assert.Equal(tecnico.IdRol, (await context.Roles.SingleAsync(r => r.Nombre == RolesSistema.Operador)).IdRol);
    }

    private static ECARDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ECARDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ECARDbContext(options);
    }
}
