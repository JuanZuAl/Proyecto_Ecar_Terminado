using System.Text.Json.Serialization;

namespace ECAR.Shared.DTOs;

// Respuesta de la consulta por QR (GET api/equipos/qr/{token}, pública): solo lo que usa la pantalla,
// no el EquipoDto completo. Así no viajan campos vacíos ni de relleno (fecha 0001-01-01, ActivoFijo "")
// ni datos que no se muestran (token, id de la ubicación, serial, fabricante).
public class ConsultaQrDto
{
    public EquipoQrFichaDto Equipo { get; set; } = new();
    public List<ChecklistQrDto> ChecklistsActivos { get; set; } = new();
}

/// <summary>
/// Ficha del equipo para quien escanea el QR. Sin sesión: código, nombre y ubicación. Con sesión: además
/// criticidad, marca, modelo y categoría; los campos que no aplican no se envían.
/// </summary>
public class EquipoQrFichaDto
{
    /// <summary>Lo necesita «Iniciar» para abrir la inspección tras iniciar sesión.</summary>
    public long IdEquipo { get; set; }
    public string CodigoInterno { get; set; } = string.Empty;
    public string NombreEquipo { get; set; } = string.Empty;
    public string? UbicacionNombre { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Criticidad { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Marca { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Modelo { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CategoriaNombre { get; set; }
}

/// <summary>Checklist activo que se puede iniciar sobre el equipo (sin preguntas, fechas ni estado).</summary>
public class ChecklistQrDto
{
    public long IdChecklist { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}
