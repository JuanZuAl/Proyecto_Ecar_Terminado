using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace ECAR.Infrastructure.Migrations
{
    /// <summary>
    /// SQL escrito a mano en una migración, ejecutado con EXEC para que SQL Server lo compile cuando le toca y no al
    /// leer el lote.
    /// </summary>
    /// <remarks>
    /// «dotnet ef migrations script» (el normal y el idempotente) deja cada migración en un solo lote, y SQL Server
    /// compila el lote entero antes de ejecutarlo: una instrucción que usa una columna agregada en esa misma migración
    /// falla («Invalid column name») y CREATE TRIGGER, que debe ser la primera de su lote, no se admite. Dentro de EXEC
    /// se compila al ejecutarse. Al arrancar, Migrate envía cada instrucción por separado, así que allí daba igual.
    /// Toda migración nueva con SQL a mano debe usar este método en lugar de migrationBuilder.Sql.
    /// </remarks>
    public static class MigracionesSql
    {
        public static OperationBuilder<SqlOperation> SqlDiferido(this MigrationBuilder migrationBuilder, string sql) =>
            migrationBuilder.Sql("EXEC(N'" + sql.Replace("'", "''") + "');");
    }
}
