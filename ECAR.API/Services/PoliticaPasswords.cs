using ECAR.API.Configuration;
using Microsoft.Extensions.Options;

namespace ECAR.API.Services;

/// <summary>
/// Reglas de la contraseña local que pone el Administrador al crear un usuario. Desde el 03/11/2026 este sistema
/// no cambia, restablece ni hace caducar contraseñas (lo hará una API externa): solo queda validar la del alta.
/// </summary>
public class PoliticaPasswords(IOptions<SeguridadOptions> opciones)
{
    public SeguridadOptions Opciones => opciones.Value;

    public List<string> Validar(string? password)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errores.Add("La contraseña es requerida");
            return errores;
        }

        if (password.Length < Opciones.LongitudMinimaPassword)
        {
            errores.Add($"La contraseña debe tener al menos {Opciones.LongitudMinimaPassword} caracteres");
        }

        if (Opciones.RequiereLetrasYNumeros && (!password.Any(char.IsLetter) || !password.Any(char.IsDigit)))
        {
            errores.Add("La contraseña debe combinar letras y números");
        }

        return errores;
    }
}
