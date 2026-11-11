using ECAR.Shared;
using MudBlazor;

namespace ECAR.Client.Services;

/// <summary>Presentación común de la auditoría: color de cada acción y hora local.</summary>
public static class FormatoAuditoria
{
    public static Color ColorAccion(string? accion) => accion switch
    {
        AuditoriaAcciones.Crear or AuditoriaAcciones.Activar or AuditoriaAcciones.LoginExitoso
            or AuditoriaAcciones.NotificacionEnviada => Color.Success,
        AuditoriaAcciones.Modificar or AuditoriaAcciones.CambioPassword => Color.Info,
        AuditoriaAcciones.Firmar or AuditoriaAcciones.Cerrar or AuditoriaAcciones.Resolver => Color.Primary,
        AuditoriaAcciones.Desactivar or AuditoriaAcciones.Retirar or AuditoriaAcciones.RestablecerPassword
            or AuditoriaAcciones.NotificacionFallida => Color.Warning,
        AuditoriaAcciones.Eliminar or AuditoriaAcciones.Anular or AuditoriaAcciones.LoginFallido
            or AuditoriaAcciones.CuentaBloqueada => Color.Error,
        // Filas anteriores a la Fase 4.
        _ => (accion ?? string.Empty).ToUpperInvariant() switch
        {
            "INSERT" or "CREATE" => Color.Success,
            "UPDATE" or "EDITAR" => Color.Info,
            "DELETE" or "ELIMINAR" => Color.Error,
            _ => Color.Default
        }
    };

    /// <summary>Nombre legible de la acción (el valor guardado, p. ej. «LoginExitoso», no cambia).</summary>
    public static string NombreAccion(string? accion) => accion switch
    {
        AuditoriaAcciones.LoginExitoso => "Inicio de sesión",
        AuditoriaAcciones.LoginFallido => "Inicio de sesión fallido",
        AuditoriaAcciones.CuentaBloqueada => "Cuenta bloqueada",
        AuditoriaAcciones.CierreSesion => "Cierre de sesión",
        AuditoriaAcciones.CambioPassword => "Cambio de contraseña",
        AuditoriaAcciones.RestablecerPassword => "Restablecimiento de contraseña",
        AuditoriaAcciones.Exportacion => "Exportación",
        AuditoriaAcciones.VerificacionIntegridad => "Verificación de integridad",
        AuditoriaAcciones.NotificacionEnviada => "Notificación enviada",
        AuditoriaAcciones.NotificacionFallida => "Notificación fallida",
        _ => accion ?? string.Empty
    };

    /// <summary>La auditoría guarda la hora del servidor en UTC; se muestra en la hora local con segundos.</summary>
    public static string FechaHora(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
}
