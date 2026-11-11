using BCrypt.Net;
using ECAR.Infrastructure.Entities;
using ECAR.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ECAR.Infrastructure.Data;

/// <summary>
/// Datos mínimos para usar el sistema, que el API crea al arrancar si faltan: los roles del sistema y el
/// administrador inicial (admin@ecar.com, con la contraseña de la clave AdminPassword) con el rol Administrador.
/// No se siembra nada más: categorías, ubicaciones, equipos, checklists y usuarios los registra el Administrador.
/// </summary>
/// <remarks>
/// El script BaseDeDatos/ECAR_CrearBaseDeDatos.sql deja los mismos datos, pero el administrador sin contraseña,
/// porque SQL Server no calcula hashes BCrypt: se la pone este método en el primer arranque, con AdminPassword.
/// </remarks>
public static class DataSeeder
{
    public static async Task SeedDataAsync(ECARDbContext context, IConfiguration configuration)
    {
        // Roles del sistema (RolesSistema). Los permisos de cada pantalla y de cada acción del API dependen de
        // estos nombres, y el sistema no tiene pantalla para crear roles, así que se siembran los que falten.
        var existingRoleNames = await context.Roles.Select(r => r.Nombre).ToListAsync();
        var missingRoles = RolesSistema.DelSistema
            .Except(existingRoleNames, StringComparer.OrdinalIgnoreCase)
            .Select(nombre => new Rol { Nombre = nombre })
            .ToList();
        if (missingRoles.Count > 0)
        {
            await context.Roles.AddRangeAsync(missingRoles);
            await context.SaveChangesAsync();
        }

        var adminUsuario = await context.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == "admin@ecar.com");
        if (adminUsuario == null)
        {
            adminUsuario = new Usuario
            {
                Nombre = "Administrador ECAR",
                Correo = "admin@ecar.com",
                UsuarioAD = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword(configuration)),
                Activo = true
            };
            await context.Usuarios.AddAsync(adminUsuario);
            await context.SaveChangesAsync();
        }
        else if (string.IsNullOrEmpty(adminUsuario.PasswordHash))
        {
            // Creado por el script de la base, sin contraseña: queda en la auditoría como un cambio de contraseña.
            adminUsuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword(configuration));
            await context.SaveChangesAsync();
        }

        var adminRol = await context.Roles.SingleAsync(r => r.Nombre == RolesSistema.Administrador);
        if (!await context.UsuarioRoles.AnyAsync(ur =>
                ur.IdUsuario == adminUsuario.IdUsuario && ur.IdRol == adminRol.IdRol))
        {
            await context.UsuarioRoles.AddAsync(new UsuarioRol
            {
                IdUsuario = adminUsuario.IdUsuario,
                IdRol = adminRol.IdRol
            });
            await context.SaveChangesAsync();
        }
    }

    private static string AdminPassword(IConfiguration configuration) =>
        configuration["AdminPassword"] ??
        throw new InvalidOperationException("AdminPassword no está configurado (user-secrets o variable de entorno AdminPassword)");
}
