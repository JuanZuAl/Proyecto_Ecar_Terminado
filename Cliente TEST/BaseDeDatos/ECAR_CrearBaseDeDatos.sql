/* ==================================================================================================================
   ECAR - Sistema de Gestión de Equipos e Inspecciones
   Script de creación de la base de datos

   Esquema de la migración 20261108184118_EsquemaInicial (EF Core 10.0.11).

   Qué hace, en una sola transacción (si algo falla, no queda nada a medias):
     1. Crea la base si no existe (con la intercalación por defecto del servidor).
     2. Crea las 14 tablas con sus claves, índices y relaciones, el trigger TR_Auditoria_SoloInsercion (la auditoría
        no se puede modificar ni borrar) y el historial de migraciones de EF Core. Con ese historial el API reconoce
        la base al arrancar y no intenta crearla otra vez.
     3. Siembra los 4 roles del sistema (Administrador, Operador, Estándar, Supervisor) y el administrador inicial
        admin@ecar.com con el rol Administrador, y deja esas altas en la auditoría con su cadena SHA-256, como el
        API. En la auditoría quedan la cuenta de Windows o de SQL que ejecutó el script, su equipo y el programa.

   La contraseña de admin@ecar.com NO está en este script: SQL Server no puede calcular el hash BCrypt que usa el
   sistema. El administrador queda sin contraseña y el API se la pone en su primer arranque con la variable de
   entorno AdminPassword (Guía de primer inicio, paso 6). Ese cambio también queda en la auditoría. Hasta entonces
   admin@ecar.com no puede entrar.

   Cómo ejecutarlo (como administrador de SQL Server o con permiso para crear bases):
     - sqlcmd:  sqlcmd -S <servidor\instancia> -E -i ECAR_CrearBaseDeDatos.sql
     - SQL Server Management Studio: abrir el archivo, activar Consulta > Modo SQLCMD y ejecutar.
   Para usar otro nombre de base, cambie el valor de :setvar BaseDatos (y la cadena de conexión del API).

   Solo trabaja sobre una base nueva o vacía: si la base ya tiene tablas, se detiene sin cambiar nada. Para
   actualizar una base existente a una versión nueva, vea el README («Base de datos»).

   Requisitos: SQL Server 2016 o posterior. Probado con SQL Server 2022 Express.
   Después de crear la base, la cuenta del API solo necesita leer y escribir (sección opcional al final).
   ================================================================================================================== */

:setvar BaseDatos "ECARDB"
:on error exit

SET NOCOUNT ON;
IF DB_ID(N'$(BaseDatos)') IS NULL
BEGIN
    CREATE DATABASE [$(BaseDatos)];
    PRINT N'Base $(BaseDatos) creada.';
END
GO

USE [$(BaseDatos)];
GO

-- Opciones que necesitan los índices filtrados y el trigger; valen para el resto de la sesión.
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
    THROW 50001, N'La base $(BaseDatos) ya tiene tablas: este script solo crea una base nueva o vacía y no cambió nada. Para actualizar una base existente, vea el README.', 1;
GO

BEGIN TRANSACTION;

-- ==================================================================================================================
-- 1. Esquema (generado con «dotnet ef migrations script» desde la migración única)
-- ==================================================================================================================

CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);

CREATE TABLE [CategoriasEquipo] (
    [IdCategoria] bigint NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [Descripcion] nvarchar(500) NULL,
    CONSTRAINT [PK_CategoriasEquipo] PRIMARY KEY ([IdCategoria])
);

CREATE TABLE [Checklists] (
    [IdChecklist] bigint NOT NULL IDENTITY,
    [Nombre] nvarchar(200) NOT NULL,
    [Version] nvarchar(20) NOT NULL,
    [Activo] bit NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    CONSTRAINT [PK_Checklists] PRIMARY KEY ([IdChecklist])
);

CREATE TABLE [Roles] (
    [IdRol] bigint NOT NULL IDENTITY,
    [Nombre] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([IdRol])
);

CREATE TABLE [Ubicaciones] (
    [IdUbicacion] bigint NOT NULL IDENTITY,
    [Planta] nvarchar(100) NOT NULL,
    [Area] nvarchar(100) NOT NULL,
    [Descripcion] nvarchar(300) NULL,
    CONSTRAINT [PK_Ubicaciones] PRIMARY KEY ([IdUbicacion])
);

CREATE TABLE [Usuarios] (
    [IdUsuario] bigint NOT NULL IDENTITY,
    [Nombre] nvarchar(200) NOT NULL,
    [Correo] nvarchar(150) NOT NULL,
    [UsuarioAD] nvarchar(100) NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [Activo] bit NOT NULL,
    [IntentosFallidos] int NOT NULL,
    [BloqueadoHasta] datetime2 NULL,
    [FechaCambioPassword] datetime2 NULL,
    [DebeCambiarPassword] bit NOT NULL,
    [SecurityStamp] nvarchar(64) NOT NULL,
    [UltimoAcceso] datetime2 NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([IdUsuario])
);

CREATE TABLE [PreguntasChecklist] (
    [IdPregunta] bigint NOT NULL IDENTITY,
    [IdChecklist] bigint NOT NULL,
    [Pregunta] nvarchar(max) NOT NULL,
    [TipoRespuesta] nvarchar(50) NOT NULL,
    [Obligatoria] bit NOT NULL,
    [Orden] int NOT NULL,
    CONSTRAINT [PK_PreguntasChecklist] PRIMARY KEY ([IdPregunta]),
    CONSTRAINT [FK_PreguntasChecklist_Checklists_IdChecklist] FOREIGN KEY ([IdChecklist]) REFERENCES [Checklists] ([IdChecklist]) ON DELETE NO ACTION
);

CREATE TABLE [Equipos] (
    [IdEquipo] bigint NOT NULL IDENTITY,
    [CodigoInterno] nvarchar(50) NOT NULL,
    [ActivoFijo] nvarchar(50) NOT NULL,
    [SerialFabricante] nvarchar(100) NULL,
    [NombreEquipo] nvarchar(200) NOT NULL,
    [Marca] nvarchar(100) NULL,
    [Modelo] nvarchar(100) NULL,
    [Fabricante] nvarchar(200) NULL,
    [Criticidad] nvarchar(20) NULL,
    [IdCategoria] bigint NULL,
    [IdUbicacion] bigint NULL,
    [QRCode] nvarchar(64) NULL,
    [Activo] bit NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    CONSTRAINT [PK_Equipos] PRIMARY KEY ([IdEquipo]),
    CONSTRAINT [FK_Equipos_CategoriasEquipo_IdCategoria] FOREIGN KEY ([IdCategoria]) REFERENCES [CategoriasEquipo] ([IdCategoria]),
    CONSTRAINT [FK_Equipos_Ubicaciones_IdUbicacion] FOREIGN KEY ([IdUbicacion]) REFERENCES [Ubicaciones] ([IdUbicacion])
);

CREATE TABLE [Auditoria] (
    [IdAuditoria] bigint NOT NULL IDENTITY,
    [Tabla] nvarchar(100) NOT NULL,
    [RegistroId] bigint NOT NULL,
    [Accion] nvarchar(50) NOT NULL,
    [ValorAnterior] nvarchar(max) NULL,
    [ValorNuevo] nvarchar(max) NULL,
    [Usuario] nvarchar(100) NOT NULL,
    [FechaHora] datetime2 NOT NULL,
    [IdUsuario] bigint NULL,
    [Motivo] nvarchar(500) NULL,
    [DireccionIp] nvarchar(45) NULL,
    [AgenteUsuario] nvarchar(300) NULL,
    [HashAnterior] char(64) NULL,
    [Hash] char(64) NOT NULL,
    CONSTRAINT [PK_Auditoria] PRIMARY KEY ([IdAuditoria]),
    CONSTRAINT [FK_Auditoria_Usuarios_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [Usuarios] ([IdUsuario]) ON DELETE NO ACTION
);

CREATE TABLE [HistorialPasswords] (
    [Id] bigint NOT NULL IDENTITY,
    [IdUsuario] bigint NOT NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [Fecha] datetime2 NOT NULL,
    CONSTRAINT [PK_HistorialPasswords] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_HistorialPasswords_Usuarios_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [Usuarios] ([IdUsuario]) ON DELETE CASCADE
);

CREATE TABLE [UsuarioRol] (
    [Id] bigint NOT NULL IDENTITY,
    [IdUsuario] bigint NOT NULL,
    [IdRol] bigint NOT NULL,
    CONSTRAINT [PK_UsuarioRol] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UsuarioRol_Roles_IdRol] FOREIGN KEY ([IdRol]) REFERENCES [Roles] ([IdRol]) ON DELETE CASCADE,
    CONSTRAINT [FK_UsuarioRol_Usuarios_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [Usuarios] ([IdUsuario]) ON DELETE CASCADE
);

CREATE TABLE [Inspecciones] (
    [IdInspeccion] bigint NOT NULL IDENTITY,
    [IdEquipo] bigint NOT NULL,
    [IdUsuario] bigint NOT NULL,
    [IdChecklist] bigint NOT NULL,
    [FechaInspeccion] datetime2 NOT NULL,
    [Resultado] nvarchar(50) NULL,
    [Observaciones] nvarchar(max) NULL,
    [Sede] nvarchar(30) NULL,
    [FirmaDigital] nvarchar(max) NULL,
    [Estado] nvarchar(20) NOT NULL DEFAULT N'EnCurso',
    [FechaCierre] datetime2 NULL,
    [FirmaHash] nvarchar(64) NULL,
    [FirmaNombre] nvarchar(200) NULL,
    [FirmaSignificado] nvarchar(200) NULL,
    [FirmaAlgoritmo] nvarchar(30) NULL,
    [MotivoAnulacion] nvarchar(500) NULL,
    [FechaAnulacion] datetime2 NULL,
    [IdUsuarioAnulacion] bigint NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Inspecciones] PRIMARY KEY ([IdInspeccion]),
    CONSTRAINT [FK_Inspecciones_Checklists_IdChecklist] FOREIGN KEY ([IdChecklist]) REFERENCES [Checklists] ([IdChecklist]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Inspecciones_Equipos_IdEquipo] FOREIGN KEY ([IdEquipo]) REFERENCES [Equipos] ([IdEquipo]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Inspecciones_Usuarios_IdUsuario] FOREIGN KEY ([IdUsuario]) REFERENCES [Usuarios] ([IdUsuario]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Inspecciones_Usuarios_IdUsuarioAnulacion] FOREIGN KEY ([IdUsuarioAnulacion]) REFERENCES [Usuarios] ([IdUsuario]) ON DELETE NO ACTION
);

CREATE TABLE [Evidencias] (
    [IdEvidencia] bigint NOT NULL IDENTITY,
    [IdInspeccion] bigint NOT NULL,
    [Archivo] nvarchar(max) NOT NULL,
    [NombreOriginal] nvarchar(200) NOT NULL,
    [TipoContenido] nvarchar(100) NOT NULL,
    [TamanoBytes] bigint NOT NULL,
    [FechaCarga] datetime2 NOT NULL,
    [IdUsuarioCarga] bigint NOT NULL,
    [Retirada] bit NOT NULL,
    [FechaRetiro] datetime2 NULL,
    [IdUsuarioRetiro] bigint NULL,
    [MotivoRetiro] nvarchar(500) NULL,
    CONSTRAINT [PK_Evidencias] PRIMARY KEY ([IdEvidencia]),
    CONSTRAINT [FK_Evidencias_Inspecciones_IdInspeccion] FOREIGN KEY ([IdInspeccion]) REFERENCES [Inspecciones] ([IdInspeccion]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Evidencias_Usuarios_IdUsuarioCarga] FOREIGN KEY ([IdUsuarioCarga]) REFERENCES [Usuarios] ([IdUsuario]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Evidencias_Usuarios_IdUsuarioRetiro] FOREIGN KEY ([IdUsuarioRetiro]) REFERENCES [Usuarios] ([IdUsuario]) ON DELETE NO ACTION
);

CREATE TABLE [RespuestasInspeccion] (
    [IdRespuesta] bigint NOT NULL IDENTITY,
    [IdInspeccion] bigint NOT NULL,
    [IdPregunta] bigint NOT NULL,
    [Respuesta] nvarchar(max) NULL,
    [Observacion] nvarchar(max) NULL,
    [PresentaNovedad] bit NULL,
    CONSTRAINT [PK_RespuestasInspeccion] PRIMARY KEY ([IdRespuesta]),
    CONSTRAINT [FK_RespuestasInspeccion_Inspecciones_IdInspeccion] FOREIGN KEY ([IdInspeccion]) REFERENCES [Inspecciones] ([IdInspeccion]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RespuestasInspeccion_PreguntasChecklist_IdPregunta] FOREIGN KEY ([IdPregunta]) REFERENCES [PreguntasChecklist] ([IdPregunta]) ON DELETE NO ACTION
);

CREATE TABLE [Novedades] (
    [IdNovedad] bigint NOT NULL IDENTITY,
    [IdInspeccion] bigint NOT NULL,
    [IdPregunta] bigint NOT NULL,
    [IdRespuesta] bigint NOT NULL,
    [Descripcion] nvarchar(max) NOT NULL,
    [Criticidad] nvarchar(20) NULL,
    [Estado] nvarchar(20) NOT NULL DEFAULT N'Pendiente',
    [FechaRegistro] datetime2 NOT NULL,
    [IdUsuarioRegistro] bigint NOT NULL,
    [Solucion] nvarchar(2000) NULL,
    [FechaResolucion] datetime2 NULL,
    [IdUsuarioResolucion] bigint NULL,
    [FirmaDigital] nvarchar(max) NULL,
    [FirmaNombre] nvarchar(200) NULL,
    [FirmaSignificado] nvarchar(200) NULL,
    [FirmaAlgoritmo] nvarchar(30) NULL,
    [FirmaHash] nvarchar(64) NULL,
    [MotivoAnulacion] nvarchar(500) NULL,
    [FechaAnulacion] datetime2 NULL,
    [IdUsuarioAnulacion] bigint NULL,
    [FechaNotificacion] datetime2 NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Novedades] PRIMARY KEY ([IdNovedad]),
    CONSTRAINT [FK_Novedades_Inspecciones_IdInspeccion] FOREIGN KEY ([IdInspeccion]) REFERENCES [Inspecciones] ([IdInspeccion]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Novedades_PreguntasChecklist_IdPregunta] FOREIGN KEY ([IdPregunta]) REFERENCES [PreguntasChecklist] ([IdPregunta]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Novedades_RespuestasInspeccion_IdRespuesta] FOREIGN KEY ([IdRespuesta]) REFERENCES [RespuestasInspeccion] ([IdRespuesta]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Novedades_Usuarios_IdUsuarioAnulacion] FOREIGN KEY ([IdUsuarioAnulacion]) REFERENCES [Usuarios] ([IdUsuario]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Novedades_Usuarios_IdUsuarioRegistro] FOREIGN KEY ([IdUsuarioRegistro]) REFERENCES [Usuarios] ([IdUsuario]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Novedades_Usuarios_IdUsuarioResolucion] FOREIGN KEY ([IdUsuarioResolucion]) REFERENCES [Usuarios] ([IdUsuario]) ON DELETE NO ACTION
);

CREATE INDEX [IX_Auditoria_Accion] ON [Auditoria] ([Accion]);

CREATE INDEX [IX_Auditoria_FechaHora] ON [Auditoria] ([FechaHora]);

CREATE INDEX [IX_Auditoria_IdUsuario] ON [Auditoria] ([IdUsuario]);

CREATE INDEX [IX_Auditoria_RegistroId] ON [Auditoria] ([RegistroId]);

CREATE INDEX [IX_Auditoria_Tabla] ON [Auditoria] ([Tabla]);

CREATE INDEX [IX_Auditoria_Tabla_RegistroId_FechaHora] ON [Auditoria] ([Tabla], [RegistroId], [FechaHora]);

CREATE INDEX [IX_Auditoria_Usuario] ON [Auditoria] ([Usuario]);

CREATE UNIQUE INDEX [IX_CategoriasEquipo_Nombre] ON [CategoriasEquipo] ([Nombre]);

CREATE INDEX [IX_Checklists_Activo] ON [Checklists] ([Activo]);

CREATE UNIQUE INDEX [IX_Checklists_Nombre_Version] ON [Checklists] ([Nombre], [Version]);

CREATE UNIQUE INDEX [IX_Equipos_ActivoFijo] ON [Equipos] ([ActivoFijo]);

CREATE UNIQUE INDEX [IX_Equipos_CodigoInterno] ON [Equipos] ([CodigoInterno]);

CREATE INDEX [IX_Equipos_Criticidad] ON [Equipos] ([Criticidad]);

CREATE INDEX [IX_Equipos_IdCategoria] ON [Equipos] ([IdCategoria]);

CREATE INDEX [IX_Equipos_IdUbicacion] ON [Equipos] ([IdUbicacion]);

CREATE UNIQUE INDEX [IX_Equipos_QRCode] ON [Equipos] ([QRCode]) WHERE [QRCode] IS NOT NULL;

CREATE INDEX [IX_Evidencias_FechaCarga] ON [Evidencias] ([FechaCarga]);

CREATE INDEX [IX_Evidencias_IdInspeccion] ON [Evidencias] ([IdInspeccion]);

CREATE INDEX [IX_Evidencias_IdUsuarioCarga] ON [Evidencias] ([IdUsuarioCarga]);

CREATE INDEX [IX_Evidencias_IdUsuarioRetiro] ON [Evidencias] ([IdUsuarioRetiro]);

CREATE INDEX [IX_HistorialPasswords_IdUsuario_Fecha] ON [HistorialPasswords] ([IdUsuario], [Fecha]);

CREATE INDEX [IX_Inspecciones_Estado] ON [Inspecciones] ([Estado]);

CREATE INDEX [IX_Inspecciones_FechaInspeccion] ON [Inspecciones] ([FechaInspeccion]);

CREATE INDEX [IX_Inspecciones_IdChecklist] ON [Inspecciones] ([IdChecklist]);

CREATE INDEX [IX_Inspecciones_IdEquipo] ON [Inspecciones] ([IdEquipo]);

CREATE INDEX [IX_Inspecciones_IdUsuario] ON [Inspecciones] ([IdUsuario]);

CREATE INDEX [IX_Inspecciones_IdUsuario_Estado] ON [Inspecciones] ([IdUsuario], [Estado]);

CREATE INDEX [IX_Inspecciones_IdUsuarioAnulacion] ON [Inspecciones] ([IdUsuarioAnulacion]);

CREATE INDEX [IX_Inspecciones_Resultado] ON [Inspecciones] ([Resultado]);

CREATE INDEX [IX_Novedades_Criticidad] ON [Novedades] ([Criticidad]);

CREATE INDEX [IX_Novedades_Estado] ON [Novedades] ([Estado]);

CREATE INDEX [IX_Novedades_FechaRegistro] ON [Novedades] ([FechaRegistro]);

CREATE UNIQUE INDEX [IX_Novedades_IdInspeccion_IdPregunta] ON [Novedades] ([IdInspeccion], [IdPregunta]);

CREATE INDEX [IX_Novedades_IdPregunta] ON [Novedades] ([IdPregunta]);

CREATE UNIQUE INDEX [IX_Novedades_IdRespuesta] ON [Novedades] ([IdRespuesta]);

CREATE INDEX [IX_Novedades_IdUsuarioAnulacion] ON [Novedades] ([IdUsuarioAnulacion]);

CREATE INDEX [IX_Novedades_IdUsuarioRegistro] ON [Novedades] ([IdUsuarioRegistro]);

CREATE INDEX [IX_Novedades_IdUsuarioResolucion] ON [Novedades] ([IdUsuarioResolucion]);

CREATE INDEX [IX_PreguntasChecklist_IdChecklist] ON [PreguntasChecklist] ([IdChecklist]);

CREATE INDEX [IX_PreguntasChecklist_TipoRespuesta] ON [PreguntasChecklist] ([TipoRespuesta]);

CREATE INDEX [IX_RespuestasInspeccion_IdInspeccion] ON [RespuestasInspeccion] ([IdInspeccion]);

CREATE UNIQUE INDEX [IX_RespuestasInspeccion_IdInspeccion_IdPregunta] ON [RespuestasInspeccion] ([IdInspeccion], [IdPregunta]);

CREATE INDEX [IX_RespuestasInspeccion_IdPregunta] ON [RespuestasInspeccion] ([IdPregunta]);

CREATE UNIQUE INDEX [IX_Roles_Nombre] ON [Roles] ([Nombre]);

CREATE UNIQUE INDEX [IX_Ubicaciones_Planta_Area] ON [Ubicaciones] ([Planta], [Area]);

CREATE INDEX [IX_UsuarioRol_IdRol] ON [UsuarioRol] ([IdRol]);

CREATE UNIQUE INDEX [IX_UsuarioRol_IdUsuario_IdRol] ON [UsuarioRol] ([IdUsuario], [IdRol]);

CREATE UNIQUE INDEX [IX_Usuarios_Correo] ON [Usuarios] ([Correo]);

CREATE UNIQUE INDEX [IX_Usuarios_UsuarioAD] ON [Usuarios] ([UsuarioAD]) WHERE [UsuarioAD] IS NOT NULL;


EXEC(N'CREATE TRIGGER [dbo].[TR_Auditoria_SoloInsercion] ON [dbo].[Auditoria]
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000, N''La auditoría solo admite inserciones: sus filas no se pueden modificar ni borrar.'', 1;
END');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261108184118_EsquemaInicial', N'10.0.11');

-- ==================================================================================================================
-- 2. Datos semilla: los mismos que crea el API al arrancar (ECAR.Infrastructure/Data/DataSeeder.cs)
-- ==================================================================================================================

-- Los permisos de cada pantalla y de cada acción del API dependen de estos nombres exactos (ECAR.Shared/RolesSistema).
SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([IdRol], [Nombre]) VALUES
    (1, N'Administrador'),
    (2, N'Operador'),
    (3, N'Estándar'),
    (4, N'Supervisor');
SET IDENTITY_INSERT [Roles] OFF;

-- Sin contraseña (PasswordHash vacío): el API la pone en su primer arranque con AdminPassword.
SET IDENTITY_INSERT [Usuarios] ON;
INSERT INTO [Usuarios] ([IdUsuario], [Nombre], [Correo], [UsuarioAD], [PasswordHash], [Activo], [IntentosFallidos],
                        [BloqueadoHasta], [FechaCambioPassword], [DebeCambiarPassword], [SecurityStamp], [UltimoAcceso])
VALUES (1, N'Administrador ECAR', N'admin@ecar.com', N'admin', N'', 1, 0, NULL, NULL, 0,
        LOWER(REPLACE(CONVERT(nchar(36), NEWID()), N'-', N'')), NULL);
SET IDENTITY_INSERT [Usuarios] OFF;

SET IDENTITY_INSERT [UsuarioRol] ON;
INSERT INTO [UsuarioRol] ([Id], [IdUsuario], [IdRol]) VALUES (1, 1, 1);
SET IDENTITY_INSERT [UsuarioRol] OFF;

-- ==================================================================================================================
-- 3. Auditoría de las altas, con la misma cadena que escribe el API (ECAR.Infrastructure/Data/CadenaAuditoria.cs):
--    Hash = SHA-256 en UTF-16LE de HashAnterior|Tabla|RegistroId|Accion|ValorAnterior|ValorNuevo|IdUsuario|
--    FechaHora (7 decimales)|Motivo, con los nulos como texto vacío. La pantalla Auditoría > Verificar integridad la
--    comprueba desde la primera fila.
-- ==================================================================================================================

DECLARE @altas TABLE ([Orden] int NOT NULL PRIMARY KEY, [Tabla] nvarchar(100) NOT NULL, [RegistroId] bigint NOT NULL,
                      [ValorNuevo] nvarchar(max) NOT NULL);
INSERT INTO @altas ([Orden], [Tabla], [RegistroId], [ValorNuevo]) VALUES
    (1, N'Roles', 1, N'{"IdRol":1,"Nombre":"Administrador"}'),
    (2, N'Roles', 2, N'{"IdRol":2,"Nombre":"Operador"}'),
    (3, N'Roles', 3, N'{"IdRol":3,"Nombre":"Estándar"}'),
    (4, N'Roles', 4, N'{"IdRol":4,"Nombre":"Supervisor"}'),
    (5, N'Usuarios', 1, N'{"IdUsuario":1,"Activo":true,"BloqueadoHasta":null,"Correo":"admin@ecar.com","DebeCambiarPassword":false,"FechaCambioPassword":null,"Nombre":"Administrador ECAR","PasswordHash":"(oculta)","UsuarioAD":"admin"}'),
    (6, N'UsuarioRol', 1, N'{"Id":1,"IdRol":1,"IdUsuario":1}');

DECLARE @fecha datetime2(7) = SYSUTCDATETIME();
DECLARE @usuario nvarchar(100) = LEFT(CONCAT(N'Instalación (', SUSER_SNAME(), N')'), 100);
DECLARE @motivo nvarchar(500) = N'Instalación: base creada con el script ECAR_CrearBaseDeDatos.sql';
DECLARE @equipo nvarchar(45) = LEFT(CONVERT(nvarchar(48), CONNECTIONPROPERTY('client_net_address')), 45);
DECLARE @programa nvarchar(300) = LEFT(CONCAT(APP_NAME(), N' en ', HOST_NAME()), 300);
DECLARE @orden int = 1, @anterior char(64) = NULL, @hash char(64);

WHILE @orden <= 6
BEGIN
    SELECT @hash = CONVERT(char(64), HASHBYTES('SHA2_256', CONCAT(
            ISNULL(CONVERT(nvarchar(64), @anterior), N''), N'|',
            [Tabla], N'|',
            CONVERT(nvarchar(20), [RegistroId]), N'|',
            N'Crear', N'|',
            N'', N'|',
            [ValorNuevo], N'|',
            N'', N'|',
            FORMAT(@fecha, N'yyyy-MM-dd''T''HH'':''mm'':''ss''.''fffffff', 'en-US'), N'|',
            @motivo)), 2)
    FROM @altas WHERE [Orden] = @orden;

    -- Una fila por instrucción, en orden: la verificación recorre la tabla por IdAuditoria.
    INSERT INTO [Auditoria] ([Tabla], [RegistroId], [Accion], [ValorAnterior], [ValorNuevo], [Usuario], [FechaHora],
                             [IdUsuario], [Motivo], [DireccionIp], [AgenteUsuario], [HashAnterior], [Hash])
    SELECT [Tabla], [RegistroId], N'Crear', NULL, [ValorNuevo], @usuario, @fecha, NULL, @motivo, @equipo, @programa,
           @anterior, @hash
    FROM @altas WHERE [Orden] = @orden;

    SET @anterior = @hash;
    SET @orden += 1;
END

COMMIT;

DECLARE @resumen nvarchar(400);
SELECT @resumen = CONCAT(N'Base $(BaseDatos) lista: ',
    (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0 AND name <> N'__EFMigrationsHistory'), N' tablas, ',
    (SELECT COUNT(*) FROM [Roles]), N' roles, ',
    (SELECT COUNT(*) FROM [Usuarios]), N' usuario y ',
    (SELECT COUNT(*) FROM [Auditoria]), N' filas de auditoría.');
PRINT @resumen;
PRINT N'Siguiente paso: arrancar el API con AdminPassword para que ponga la contraseña de admin@ecar.com.';
GO

/* ==================================================================================================================
   Opcional: permisos mínimos para la cuenta del API. Con la base ya creada, el API solo necesita leer y escribir
   (no db_owner). Cambie la cuenta si el grupo de aplicaciones de IIS usa otra, quite los comentarios y ejecute.

USE [$(BaseDatos)];
IF SUSER_ID(N'IIS APPPOOL\ECAR-API') IS NULL
    CREATE LOGIN [IIS APPPOOL\ECAR-API] FROM WINDOWS;
IF DATABASE_PRINCIPAL_ID(N'IIS APPPOOL\ECAR-API') IS NULL
    CREATE USER [IIS APPPOOL\ECAR-API] FOR LOGIN [IIS APPPOOL\ECAR-API];
ALTER ROLE db_datareader ADD MEMBER [IIS APPPOOL\ECAR-API];
ALTER ROLE db_datawriter ADD MEMBER [IIS APPPOOL\ECAR-API];
GO

   Cuando una versión nueva traiga cambios de esquema, aplíquelos antes con el script idempotente de EF Core o
   devuelva temporalmente db_owner a esa cuenta (README, «Base de datos»).
   ================================================================================================================== */
