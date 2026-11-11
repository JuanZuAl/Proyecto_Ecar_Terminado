using System.Globalization;
using System.Text;
using ECAR.Shared;

namespace ECAR.API.Services.SgiIdentity;

/// <summary>
/// Roles de ECAR que dan los grupos que devuelve SGI.Identity. SGI.Identity solo autentica: qué puede hacer cada
/// usuario lo decide ECAR (guía §4.4), con la tabla SgiIdentity:Grupos.
/// </summary>
public static class MapeoGruposRoles
{
    /// <summary>
    /// Roles del sistema (con su nombre exacto de RolesSistema) que dan <paramref name="grupos"/>. Con
    /// <paramref name="mapeo"/> vacío, un grupo que se llama como un rol da ese rol.
    /// </summary>
    public static List<string> Roles(IEnumerable<string>? grupos, IReadOnlyDictionary<string, string> mapeo)
    {
        var roles = new List<string>();
        foreach (var grupo in grupos ?? [])
        {
            if (string.IsNullOrWhiteSpace(grupo))
            {
                continue;
            }

            var rol = mapeo.Count == 0
                ? RolDelSistema(grupo)
                : BuscarEnMapeo(mapeo, grupo.Trim()) is { } destino ? RolDelSistema(destino) : null;
            if (rol != null && !roles.Contains(rol))
            {
                roles.Add(rol);
            }
        }

        // Siempre en el orden de RolesSistema, para comparar y mostrar igual en cada inicio de sesión.
        return RolesSistema.DelSistema.Where(roles.Contains).ToList();
    }

    /// <summary>El rol del sistema que se llama <paramref name="nombre"/>, sin distinguir mayúsculas ni tildes.</summary>
    public static string? RolDelSistema(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return null;
        }

        var buscado = Normalizar(nombre);
        return RolesSistema.DelSistema.FirstOrDefault(rol => Normalizar(rol) == buscado);
    }

    private static string? BuscarEnMapeo(IReadOnlyDictionary<string, string> mapeo, string grupo)
    {
        if (mapeo.TryGetValue(grupo, out var rol))
        {
            return rol;
        }

        // El diccionario que arma la configuración puede no ser insensible a mayúsculas.
        return mapeo.FirstOrDefault(par => string.Equals(par.Key.Trim(), grupo, StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
        var sinTildes = new StringBuilder(descompuesto.Length);
        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                sinTildes.Append(caracter);
            }
        }

        return sinTildes.ToString().ToUpperInvariant();
    }
}
