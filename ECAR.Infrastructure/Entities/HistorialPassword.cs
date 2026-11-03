using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECAR.Infrastructure.Entities;

/// <summary>
/// Hashes de las contraseñas anteriores de un usuario (PLAN_FASE4_TAREAS §3.1 y §3.8). Desde el 03/11/2026 no se
/// agregan filas: el sistema ya no cambia contraseñas. La tabla se conserva con lo anterior. La auditoría nunca
/// guarda el hash.
/// </summary>
[Table("HistorialPasswords")]
public class HistorialPassword
{
    [Key]
    [Column("Id")]
    public long Id { get; set; }

    [Required]
    [Column("IdUsuario")]
    public long IdUsuario { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("PasswordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [Column("Fecha")]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    [ForeignKey("IdUsuario")]
    public virtual Usuario Usuario { get; set; } = null!;
}
