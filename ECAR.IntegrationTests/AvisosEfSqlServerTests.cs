using ECAR.Infrastructure.Data;
using ECAR.Infrastructure.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ECAR.IntegrationTests;

/// <summary>
/// Avisos de EF Core que el log del API repetía en cada guardado (B-14). La cadena de conexión
/// recomendada en ENV_SETUP.md lleva MultipleActiveResultSets=true (MARS); con MARS, EF no puede
/// crear savepoints dentro de una transacción y lo avisa en cada SaveChanges.
/// </summary>
public class AvisosEfSqlServerTests(BaseCompartida compartida) : IClassFixture<BaseCompartida>
{
    [Fact]
    public async Task Guardar_con_auditoria_y_MARS_no_deja_el_aviso_de_savepoints()
    {
        var conMars = new SqlConnectionStringBuilder(compartida.Base.CadenaConexion)
        {
            MultipleActiveResultSets = true
        }.ConnectionString;
        var avisos = new List<string>();
        var opciones = new DbContextOptionsBuilder<ECARDbContext>()
            .UseSqlServer(conMars)
            .LogTo(avisos.Add, [SqlServerEventId.SavepointsDisabledBecauseOfMARS], LogLevel.Warning)
            .Options;

        var codigo = "IT-" + Guid.NewGuid().ToString("N")[..10];
        await using (var db = new ECARDbContext(opciones))
        {
            db.Equipos.Add(new Equipo { CodigoInterno = codigo, ActivoFijo = "AF-" + codigo, NombreEquipo = "Equipo " + codigo });
            await db.SaveChangesAsync();
        }

        Assert.Empty(avisos);

        // Y el guardado sí quedó hecho, con su fila de auditoría.
        await using var lectura = compartida.Base.CrearContexto();
        var equipo = await lectura.Equipos.SingleAsync(e => e.CodigoInterno == codigo);
        Assert.True(await lectura.Auditoria.AnyAsync(a => a.Tabla == "Equipos" && a.RegistroId == equipo.IdEquipo));
    }
}
