using ECAR.API.Configuration;
using ECAR.API.Controllers;
using ECAR.API.Services;
using ECAR.Infrastructure.Data;
using ECAR.Infrastructure.Entities;
using ECAR.Shared.DTOs;
using ECAR.Shared.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ECAR.IntegrationTests;

/// <summary>
/// M-14: GET /api/usuarios y GET /api/roles paginaban sin orden definido (ORDER BY (SELECT 1) con
/// OFFSET/FETCH; EF avisa «Skip/Take sin OrderBy»). Con más de una página el orden podía variar entre
/// consultas y repetir u omitir filas. El proveedor en memoria no lo nota: se prueba contra SQL Server.
/// </summary>
public class OrdenPaginacionSqlServerTests(BaseCompartida compartida) : IClassFixture<BaseCompartida>
{
    private static List<long> Ids<T>(ActionResult<ApiResponse<PagedResultDto<T>>> resultado, Func<T, long> id)
    {
        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var respuesta = Assert.IsType<ApiResponse<PagedResultDto<T>>>(ok.Value);
        return respuesta.Data!.Data.Select(id).ToList();
    }

    [Fact]
    public async Task Usuarios_y_roles_se_paginan_con_orden_definido_y_sin_aviso_de_EF()
    {
        var avisos = new List<string>();
        var opciones = new DbContextOptionsBuilder<ECARDbContext>()
            .UseSqlServer(compartida.Base.CadenaConexion)
            .LogTo(avisos.Add, [CoreEventId.RowLimitingOperationWithoutOrderByWarning], LogLevel.Warning)
            .Options;
        await using var db = new ECARDbContext(opciones);

        var sufijo = Guid.NewGuid().ToString("N")[..8];
        for (var i = 1; i <= 5; i++)
        {
            db.Usuarios.Add(new Usuario { Nombre = $"Usuario {sufijo} {i}", Correo = $"u{i}-{sufijo}@ecar.test", PasswordHash = "hash" });
            db.Roles.Add(new Rol { Nombre = $"Rol {sufijo} {i}" });
        }

        await db.SaveChangesAsync();

        var usuarios = new UsuariosController(db, new PoliticaPasswords(Options.Create(new SeguridadOptions())), new MotivoCambio());
        var roles = new RolesController(db);

        // Páginas de 2 elementos hasta agotarlas: juntas cubren todos, cada uno una vez, en orden por Id.
        var idsUsuarios = new List<long>();
        var idsRoles = new List<long>();
        for (var pagina = 1; pagina <= 20; pagina++)
        {
            var u = Ids(await usuarios.GetUsuarios(pagina, 2, sufijo), x => x.IdUsuario);
            var r = Ids(await roles.GetRoles(pagina, 2, sufijo), x => x.IdRol);
            idsUsuarios.AddRange(u);
            idsRoles.AddRange(r);
            if (u.Count == 0 && r.Count == 0)
            {
                break;
            }
        }

        Assert.Equal(5, idsUsuarios.Count);
        Assert.Equal(5, idsRoles.Count);
        Assert.Equal(idsUsuarios.OrderBy(x => x), idsUsuarios);
        Assert.Equal(idsRoles.OrderBy(x => x), idsRoles);
        Assert.Equal(idsUsuarios.Distinct().Count(), idsUsuarios.Count);
        Assert.Equal(idsRoles.Distinct().Count(), idsRoles.Count);
        Assert.Empty(avisos);
    }
}
