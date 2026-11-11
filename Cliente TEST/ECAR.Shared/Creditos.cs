namespace ECAR.Shared;

/// <summary>
/// Equipo que desarrolló el sistema, con su perfil de GitHub. Lo muestra la página «Créditos» del cliente
/// (/creditos) y lo repite la sección «Créditos» del README.
/// </summary>
public static class Creditos
{
    public const string RepositorioDesarrollo = "https://github.com/carlostamayo1295/Proyecto_ECAR";

    /// <param name="Codigo">Código del rol en el equipo (BE = backend, FE = frontend; 0 = líder).</param>
    /// <param name="Usuario">Usuario de GitHub.</param>
    public sealed record Integrante(string Codigo, string Nombre, string Rol, string Usuario)
    {
        public string Perfil => $"https://github.com/{Usuario}";
    }

    public static readonly IReadOnlyList<Integrante> Backend =
    [
        new("BE-0", "Carlos Tamayo",
            "Líder de backend: arquitectura del API, migraciones, auditoría transaccional y sello de seguridad de las sesiones.",
            "carlostamayo1295"),
        new("BE-1", "Juan David López",
            "Hallazgos y novedades, anulación de inspecciones, consultas de auditoría y búsqueda de usuarios para reportes.",
            "juandalopeztrujillo-code"),
        new("BE-2", "Alejandro Gómez",
            "Reportes en PDF y Excel, registro PDF de la inspección, tablero en PDF y retiro de evidencias.",
            "alejogomez21"),
        new("BE-3", "Simón Guarín",
            "Firma electrónica de la Parte 11 (HMAC, reautenticación) y seguridad de cuentas.",
            "sguarin757"),
    ];

    public static readonly IReadOnlyList<Integrante> Frontend =
    [
        new("FE-0", "Juan Alberto Zuluaga",
            "Líder de frontend: DTOs y servicios del cliente, integración de ramas, pruebas integrales y entrega.",
            "JuanZuAl"),
        new("FE-1", "Erica Avendaño",
            "Pantallas de hallazgos y novedades, colores por tipo de acción y formularios.",
            "ericamarcelaavendano-cmd"),
        new("FE-2", "Santiago Arango",
            "Pantallas de reportes y de evidencias retiradas; reportes de QA.",
            "backyardignas"),
        new("FE-3", "Tatiana Suárez",
            "Firma en pantalla, resultado de la inspección y cuentas; reportes de pruebas.",
            "Gary1913"),
    ];
}
