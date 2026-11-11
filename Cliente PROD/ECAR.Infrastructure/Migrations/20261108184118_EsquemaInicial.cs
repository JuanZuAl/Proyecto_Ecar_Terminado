using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECAR.Infrastructure.Migrations
{
    /// <summary>
    /// Esquema completo de la base de datos de ECAR para una instalación nueva: las tablas, índices y relaciones
    /// del modelo (ECARDbContext) y el trigger que impide modificar o borrar las filas de la auditoría
    /// (CFR 21 Parte 11 §11.10(e)). El API la aplica sola al arrancar (Database.MigrateAsync en Program.cs).
    /// </summary>
    public partial class EsquemaInicial : Migration
    {
        private const string Trigger = "TR_Auditoria_SoloInsercion";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasEquipo",
                columns: table => new
                {
                    IdCategoria = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasEquipo", x => x.IdCategoria);
                });

            migrationBuilder.CreateTable(
                name: "Checklists",
                columns: table => new
                {
                    IdChecklist = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Checklists", x => x.IdChecklist);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    IdRol = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.IdRol);
                });

            migrationBuilder.CreateTable(
                name: "Ubicaciones",
                columns: table => new
                {
                    IdUbicacion = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Planta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Area = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ubicaciones", x => x.IdUbicacion);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UsuarioAD = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    IntentosFallidos = table.Column<int>(type: "int", nullable: false),
                    BloqueadoHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaCambioPassword = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DebeCambiarPassword = table.Column<bool>(type: "bit", nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UltimoAcceso = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                });

            migrationBuilder.CreateTable(
                name: "PreguntasChecklist",
                columns: table => new
                {
                    IdPregunta = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdChecklist = table.Column<long>(type: "bigint", nullable: false),
                    Pregunta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TipoRespuesta = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Obligatoria = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreguntasChecklist", x => x.IdPregunta);
                    table.ForeignKey(
                        name: "FK_PreguntasChecklist_Checklists_IdChecklist",
                        column: x => x.IdChecklist,
                        principalTable: "Checklists",
                        principalColumn: "IdChecklist",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Equipos",
                columns: table => new
                {
                    IdEquipo = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoInterno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActivoFijo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SerialFabricante = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NombreEquipo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Modelo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Fabricante = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Criticidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IdCategoria = table.Column<long>(type: "bigint", nullable: true),
                    IdUbicacion = table.Column<long>(type: "bigint", nullable: true),
                    QRCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipos", x => x.IdEquipo);
                    table.ForeignKey(
                        name: "FK_Equipos_CategoriasEquipo_IdCategoria",
                        column: x => x.IdCategoria,
                        principalTable: "CategoriasEquipo",
                        principalColumn: "IdCategoria");
                    table.ForeignKey(
                        name: "FK_Equipos_Ubicaciones_IdUbicacion",
                        column: x => x.IdUbicacion,
                        principalTable: "Ubicaciones",
                        principalColumn: "IdUbicacion");
                });

            migrationBuilder.CreateTable(
                name: "Auditoria",
                columns: table => new
                {
                    IdAuditoria = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tabla = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RegistroId = table.Column<long>(type: "bigint", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValorAnterior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValorNuevo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Usuario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaHora = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DireccionIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    AgenteUsuario = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    HashAnterior = table.Column<string>(type: "char(64)", nullable: true),
                    Hash = table.Column<string>(type: "char(64)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria", x => x.IdAuditoria);
                    table.ForeignKey(
                        name: "FK_Auditoria_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistorialPasswords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPasswords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialPasswords_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioRol",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    IdRol = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRol", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioRol_Roles_IdRol",
                        column: x => x.IdRol,
                        principalTable: "Roles",
                        principalColumn: "IdRol",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuarioRol_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inspecciones",
                columns: table => new
                {
                    IdInspeccion = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEquipo = table.Column<long>(type: "bigint", nullable: false),
                    IdUsuario = table.Column<long>(type: "bigint", nullable: false),
                    IdChecklist = table.Column<long>(type: "bigint", nullable: false),
                    FechaInspeccion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Resultado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sede = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FirmaDigital = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "EnCurso"),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FirmaHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FirmaNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FirmaSignificado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FirmaAlgoritmo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdUsuarioAnulacion = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inspecciones", x => x.IdInspeccion);
                    table.ForeignKey(
                        name: "FK_Inspecciones_Checklists_IdChecklist",
                        column: x => x.IdChecklist,
                        principalTable: "Checklists",
                        principalColumn: "IdChecklist",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspecciones_Equipos_IdEquipo",
                        column: x => x.IdEquipo,
                        principalTable: "Equipos",
                        principalColumn: "IdEquipo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspecciones_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Inspecciones_Usuarios_IdUsuarioAnulacion",
                        column: x => x.IdUsuarioAnulacion,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Evidencias",
                columns: table => new
                {
                    IdEvidencia = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdInspeccion = table.Column<long>(type: "bigint", nullable: false),
                    Archivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreOriginal = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoContenido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    FechaCarga = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdUsuarioCarga = table.Column<long>(type: "bigint", nullable: false),
                    Retirada = table.Column<bool>(type: "bit", nullable: false),
                    FechaRetiro = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdUsuarioRetiro = table.Column<long>(type: "bigint", nullable: true),
                    MotivoRetiro = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evidencias", x => x.IdEvidencia);
                    table.ForeignKey(
                        name: "FK_Evidencias_Inspecciones_IdInspeccion",
                        column: x => x.IdInspeccion,
                        principalTable: "Inspecciones",
                        principalColumn: "IdInspeccion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evidencias_Usuarios_IdUsuarioCarga",
                        column: x => x.IdUsuarioCarga,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Evidencias_Usuarios_IdUsuarioRetiro",
                        column: x => x.IdUsuarioRetiro,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RespuestasInspeccion",
                columns: table => new
                {
                    IdRespuesta = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdInspeccion = table.Column<long>(type: "bigint", nullable: false),
                    IdPregunta = table.Column<long>(type: "bigint", nullable: false),
                    Respuesta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PresentaNovedad = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RespuestasInspeccion", x => x.IdRespuesta);
                    table.ForeignKey(
                        name: "FK_RespuestasInspeccion_Inspecciones_IdInspeccion",
                        column: x => x.IdInspeccion,
                        principalTable: "Inspecciones",
                        principalColumn: "IdInspeccion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RespuestasInspeccion_PreguntasChecklist_IdPregunta",
                        column: x => x.IdPregunta,
                        principalTable: "PreguntasChecklist",
                        principalColumn: "IdPregunta",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Novedades",
                columns: table => new
                {
                    IdNovedad = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdInspeccion = table.Column<long>(type: "bigint", nullable: false),
                    IdPregunta = table.Column<long>(type: "bigint", nullable: false),
                    IdRespuesta = table.Column<long>(type: "bigint", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Criticidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pendiente"),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IdUsuarioRegistro = table.Column<long>(type: "bigint", nullable: false),
                    Solucion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdUsuarioResolucion = table.Column<long>(type: "bigint", nullable: true),
                    FirmaDigital = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirmaNombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FirmaSignificado = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FirmaAlgoritmo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    FirmaHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdUsuarioAnulacion = table.Column<long>(type: "bigint", nullable: true),
                    FechaNotificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Novedades", x => x.IdNovedad);
                    table.ForeignKey(
                        name: "FK_Novedades_Inspecciones_IdInspeccion",
                        column: x => x.IdInspeccion,
                        principalTable: "Inspecciones",
                        principalColumn: "IdInspeccion",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Novedades_PreguntasChecklist_IdPregunta",
                        column: x => x.IdPregunta,
                        principalTable: "PreguntasChecklist",
                        principalColumn: "IdPregunta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Novedades_RespuestasInspeccion_IdRespuesta",
                        column: x => x.IdRespuesta,
                        principalTable: "RespuestasInspeccion",
                        principalColumn: "IdRespuesta",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Novedades_Usuarios_IdUsuarioAnulacion",
                        column: x => x.IdUsuarioAnulacion,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Novedades_Usuarios_IdUsuarioRegistro",
                        column: x => x.IdUsuarioRegistro,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Novedades_Usuarios_IdUsuarioResolucion",
                        column: x => x.IdUsuarioResolucion,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Accion",
                table: "Auditoria",
                column: "Accion");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_FechaHora",
                table: "Auditoria",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_IdUsuario",
                table: "Auditoria",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_RegistroId",
                table: "Auditoria",
                column: "RegistroId");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Tabla",
                table: "Auditoria",
                column: "Tabla");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Tabla_RegistroId_FechaHora",
                table: "Auditoria",
                columns: new[] { "Tabla", "RegistroId", "FechaHora" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Usuario",
                table: "Auditoria",
                column: "Usuario");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasEquipo_Nombre",
                table: "CategoriasEquipo",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_Activo",
                table: "Checklists",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_Checklists_Nombre_Version",
                table: "Checklists",
                columns: new[] { "Nombre", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_ActivoFijo",
                table: "Equipos",
                column: "ActivoFijo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_CodigoInterno",
                table: "Equipos",
                column: "CodigoInterno",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_Criticidad",
                table: "Equipos",
                column: "Criticidad");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_IdCategoria",
                table: "Equipos",
                column: "IdCategoria");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_IdUbicacion",
                table: "Equipos",
                column: "IdUbicacion");

            migrationBuilder.CreateIndex(
                name: "IX_Equipos_QRCode",
                table: "Equipos",
                column: "QRCode",
                unique: true,
                filter: "[QRCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Evidencias_FechaCarga",
                table: "Evidencias",
                column: "FechaCarga");

            migrationBuilder.CreateIndex(
                name: "IX_Evidencias_IdInspeccion",
                table: "Evidencias",
                column: "IdInspeccion");

            migrationBuilder.CreateIndex(
                name: "IX_Evidencias_IdUsuarioCarga",
                table: "Evidencias",
                column: "IdUsuarioCarga");

            migrationBuilder.CreateIndex(
                name: "IX_Evidencias_IdUsuarioRetiro",
                table: "Evidencias",
                column: "IdUsuarioRetiro");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPasswords_IdUsuario_Fecha",
                table: "HistorialPasswords",
                columns: new[] { "IdUsuario", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Inspecciones_Estado",
                table: "Inspecciones",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Inspecciones_FechaInspeccion",
                table: "Inspecciones",
                column: "FechaInspeccion");

            migrationBuilder.CreateIndex(
                name: "IX_Inspecciones_IdChecklist",
                table: "Inspecciones",
                column: "IdChecklist");

            migrationBuilder.CreateIndex(
                name: "IX_Inspecciones_IdEquipo",
                table: "Inspecciones",
                column: "IdEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_Inspecciones_IdUsuario",
                table: "Inspecciones",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Inspecciones_IdUsuario_Estado",
                table: "Inspecciones",
                columns: new[] { "IdUsuario", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Inspecciones_IdUsuarioAnulacion",
                table: "Inspecciones",
                column: "IdUsuarioAnulacion");

            migrationBuilder.CreateIndex(
                name: "IX_Inspecciones_Resultado",
                table: "Inspecciones",
                column: "Resultado");

            migrationBuilder.CreateIndex(
                name: "IX_Novedades_Criticidad",
                table: "Novedades",
                column: "Criticidad");

            migrationBuilder.CreateIndex(
                name: "IX_Novedades_Estado",
                table: "Novedades",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Novedades_FechaRegistro",
                table: "Novedades",
                column: "FechaRegistro");

            migrationBuilder.CreateIndex(
                name: "IX_Novedades_IdInspeccion_IdPregunta",
                table: "Novedades",
                columns: new[] { "IdInspeccion", "IdPregunta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Novedades_IdPregunta",
                table: "Novedades",
                column: "IdPregunta");

            migrationBuilder.CreateIndex(
                name: "IX_Novedades_IdRespuesta",
                table: "Novedades",
                column: "IdRespuesta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Novedades_IdUsuarioAnulacion",
                table: "Novedades",
                column: "IdUsuarioAnulacion");

            migrationBuilder.CreateIndex(
                name: "IX_Novedades_IdUsuarioRegistro",
                table: "Novedades",
                column: "IdUsuarioRegistro");

            migrationBuilder.CreateIndex(
                name: "IX_Novedades_IdUsuarioResolucion",
                table: "Novedades",
                column: "IdUsuarioResolucion");

            migrationBuilder.CreateIndex(
                name: "IX_PreguntasChecklist_IdChecklist",
                table: "PreguntasChecklist",
                column: "IdChecklist");

            migrationBuilder.CreateIndex(
                name: "IX_PreguntasChecklist_TipoRespuesta",
                table: "PreguntasChecklist",
                column: "TipoRespuesta");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasInspeccion_IdInspeccion",
                table: "RespuestasInspeccion",
                column: "IdInspeccion");

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasInspeccion_IdInspeccion_IdPregunta",
                table: "RespuestasInspeccion",
                columns: new[] { "IdInspeccion", "IdPregunta" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RespuestasInspeccion_IdPregunta",
                table: "RespuestasInspeccion",
                column: "IdPregunta");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nombre",
                table: "Roles",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ubicaciones_Planta_Area",
                table: "Ubicaciones",
                columns: new[] { "Planta", "Area" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRol_IdRol",
                table: "UsuarioRol",
                column: "IdRol");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRol_IdUsuario_IdRol",
                table: "UsuarioRol",
                columns: new[] { "IdUsuario", "IdRol" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                table: "Usuarios",
                column: "Correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_UsuarioAD",
                table: "Usuarios",
                column: "UsuarioAD",
                unique: true,
                filter: "[UsuarioAD] IS NOT NULL");

            // Parte 11 §11.10(e): las filas de auditoría no se pueden modificar ni borrar, tampoco por SQL
            // directo. ECARDbContext lo declara con HasTrigger para que EF no use OUTPUT al insertar.
            // Va dentro de EXEC porque CREATE TRIGGER debe ser la primera instrucción de su lote, y los scripts de
            // «dotnet ef migrations script» (también el idempotente, dentro de un IF) lo dejarían en medio de uno.
            migrationBuilder.Sql($@"
EXEC(N'CREATE TRIGGER [dbo].[{Trigger}] ON [dbo].[Auditoria]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000, N''La auditoría solo admite inserciones: sus filas no se pueden modificar ni borrar.'', 1;
END');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS [dbo].[{Trigger}];");

            migrationBuilder.DropTable(
                name: "Auditoria");

            migrationBuilder.DropTable(
                name: "Evidencias");

            migrationBuilder.DropTable(
                name: "HistorialPasswords");

            migrationBuilder.DropTable(
                name: "Novedades");

            migrationBuilder.DropTable(
                name: "UsuarioRol");

            migrationBuilder.DropTable(
                name: "RespuestasInspeccion");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Inspecciones");

            migrationBuilder.DropTable(
                name: "PreguntasChecklist");

            migrationBuilder.DropTable(
                name: "Equipos");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Checklists");

            migrationBuilder.DropTable(
                name: "CategoriasEquipo");

            migrationBuilder.DropTable(
                name: "Ubicaciones");
        }
    }
}
