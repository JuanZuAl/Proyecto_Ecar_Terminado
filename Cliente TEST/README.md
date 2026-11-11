
# ECAR — Sistema de Gestión de Equipos e Inspecciones (entorno de pruebas con SGI.Identity)

> **Esta carpeta es un entorno de pruebas**, aparte del proyecto de desarrollo y de la versión para el cliente
> (`Cliente PROD`, de la que sale). Sirve para probar el inicio de sesión con SGI.Identity con
> las credenciales del ambiente de Pruebas que entregue TI. Puede correr a la vez que el de desarrollo:
>
> | | Este entorno | Desarrollo |
> |---|---|---|
> | Interfaz | `https://localhost:7367` | `https://localhost:7267` |
> | API | `https://localhost:7396` | `https://localhost:7296` |
> | Base de datos | `ECARDB_PruebaCliente` (la crea el API al primer arranque) | `ECARDB` |
> | Secretos | user-secrets propios (`UserSecretsId` de `ECAR.API.csproj`) | los del desarrollo |

## Puesta en marcha del entorno de pruebas

### 1. Dónde van las credenciales

| Dato | Dónde | Cómo |
|---|---|---|
| URL de SGI.Identity (`SgiIdentity:BaseUrl`) | `ECAR.API/appsettings.Development.json` | La que entrega TI (el archivo no se versiona). Como hoy es `http`, lleva `"PermitirHttp": true` (las contraseñas viajan sin cifrar por la red interna hasta que TI publique HTTPS). |
| Grupos → roles (`SgiIdentity:Grupos`) | `ECAR.API/appsettings.Development.json` | P. ej. `"Grupos": { "ECAR_Administradores": "Administrador", "ECAR_Operadores": "Operador" }`. Vacío: el grupo debe llamarse como el rol. |
| `ClientId` y `ClientSecret` (`SgiIdentity:ClientId`, `SgiIdentity:ClientSecret`) | user-secrets de este entorno | `configurar-secretos.ps1` (los pide sin mostrarlos). Son las credenciales de ECAR en SGI.Identity: **nunca** en un `appsettings`. |
| Contraseña del admin local (`AdminPassword`) | user-secrets de este entorno | `configurar-secretos.ps1`. Es la de `admin@ecar.com`, la cuenta de respaldo. |
| `JWT:Secret` y `Firma:ClaveHmac` | user-secrets de este entorno | `configurar-secretos.ps1` las genera solo. |
| Conexión a SQL Server | `ECAR.API/appsettings.Development.json` | Ya apunta a `.\SQLEXPRESS`, base `ECARDB_PruebaCliente`. Cámbiela si su instancia es otra. |

`appsettings.Development.json` queda así, con los datos del ambiente de Pruebas:

```json
{
  "ConnectionStrings": {
    "ECARConnection": "Server=.\\SQLEXPRESS;Database=ECARDB_PruebaCliente;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "SgiIdentity": {
    "BaseUrl": "http://<servidor-de-sgi>:<puerto>/",
    "PermitirHttp": true,
    "Grupos": {
      "<grupo-de-administradores>": "Administrador",
      "<grupo-de-supervisores>": "Supervisor",
      "<grupo-de-estandar>": "Estándar",
      "<grupo-de-operadores>": "Operador"
    }
  }
}
```

### 2. Secretos

Desde esta carpeta, en PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\configurar-secretos.ps1
```

Pide el `ClientId`, el `ClientSecret` y la contraseña de `admin@ecar.com` sin mostrarlos, y genera las claves que falten. Los guarda
en `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`, fuera del proyecto. Se puede volver a ejecutar:
con Enter deja lo que ya había, y `Firma:ClaveHmac` no la cambia nunca.

### 3. Arrancar

En dos consolas, desde esta carpeta:

```bash
dotnet run --project ECAR.API --launch-profile https
```

```bash
dotnet run --project ECAR.Client --launch-profile https
```

Abrir `https://localhost:7367`. Si el navegador no confía en el certificado: `dotnet dev-certs https --trust`. Si
SGI.Identity usa un certificado de la CA interna, este equipo debe confiar en ella; si no, el API responde «servicio no
disponible». El API no arranca si falta la URL, el `ClientId` o el `ClientSecret`, y el mensaje dice cuál.

### 4. Qué probar (lista de la guía de SGI.Identity, §11)

- Un usuario de cada grupo: entra con el rol que corresponde (se ve debajo de su nombre, arriba a la derecha).
- Contraseña errónea: «Credenciales inválidas». Usuario bloqueado: el mensaje de SGI.Identity.
- Usuario sin grupo de ECAR: no entra, y el mensaje pide a Soporte T.I. que lo agreguen al grupo.
- Contraseña vencida: se abre «Cambiar contraseña». Después del cambio, se entra con la nueva.
- Cuenta local nueva de SGI.Identity: se abre «Activar su cuenta» (número de identificación). Después, se entra.
- Firmar una inspección y resolver una novedad: piden la contraseña de SGI.Identity.
- Con `admin@ecar.com` (cuenta local), en «Usuarios»: los usuarios que entraron, con el nombre que envía
  SGI.Identity y correo `usuario@sgi.local`. En «Auditoría», cada inicio de sesión fallido por falta de grupo
  muestra en su detalle los grupos que llegaron: sirven para completar la tabla `Grupos` con los nombres reales.
- Para volver a empezar, borre la base `ECARDB_PruebaCliente`: el API la crea de nuevo al arrancar.

Aplicación web de **Laboratorios ECAR S.A.** para el inventario de equipos, sus checklists e inspecciones con
firma electrónica, novedades, evidencias fotográficas, reportes y auditoría, conforme a BPM/BPL, Data Integrity,
CFR 21 Parte 11 y ALCOA+.

## Contenido

| Carpeta | Qué es |
|---|---|
| `ECAR.API` | Servicio web (ASP.NET Core, .NET 10): reglas del negocio, seguridad, firma, reportes PDF/Excel y correo. |
| `ECAR.Client` | Interfaz web (Blazor WebAssembly con MudBlazor) que se abre en el navegador del PC o del celular. |
| `ECAR.Infrastructure` | Acceso a la base de datos (Entity Framework Core, SQL Server) y su esquema (`Migrations`). |
| `ECAR.Shared` | Datos y constantes que comparten el API y la interfaz. |
| `ECAR.AuditoriaEquipos.slnx` | Solución que agrupa los cuatro proyectos. |
| `BaseDeDatos` | Script SQL para crear la base con su esquema, los roles y el administrador inicial (opcional, ver «Base de datos»). |
| `configurar-secretos.ps1` | Guarda los secretos de este entorno de pruebas (ver arriba). |

## Documentación

La documentación no se copió a este entorno: está en `Cliente PROD/Documentación`.

| Carpeta | Documento |
|---|---|
| `Documentación/1. Gestión e inicio del proyecto` | Acta de constitución, alcance, interesados, cronograma, requisitos y cumplimiento (incluida la Parte 11 con SGI.Identity), control de cambios, riesgos, calidad, pendientes y acta de entrega. |
| `Documentación/2. Documentación técnica` | Arquitectura, modelo de datos, API, seguridad e integración con SGI.Identity, firma electrónica e integridad, configuración, despliegue y mantenimiento. |
| `Documentación/3. Manual de usuario` | Uso de cada pantalla por rol, con capturas (computador y celular), incluidos el cambio de la contraseña vencida y la activación de la cuenta con SGI.Identity. |
| `Documentación/4. Guía de primer inicio en el servidor` | Paso a paso de la puesta en marcha en la intranet con IIS: componentes, sitios, base de datos, comandos para generar `JWT__Secret` y `Firma__ClaveHmac`, conexión con SGI.Identity, variables de entorno, `appsettings.Production.json`, regla del proxy, verificación y solución de problemas. |

Los cuatro documentos están al día con SGI.Identity (10/11/2026): gestión v1.2, técnica v1.2, manual de usuario v1.2
y guía de primer inicio v1.4.

## Requisitos

- **Para compilar:** SDK de .NET 10.
- **En el servidor:** .NET 10 (en IIS, el *ASP.NET Core Hosting Bundle* de .NET 10 y el módulo *URL Rewrite*).
- **Base de datos:** SQL Server (probado con SQL Server Express).
- **Certificado HTTPS** para la dirección de la intranet. Sin HTTPS el navegador no deja usar la cámara para
  leer los códigos QR.

## Configuración

`ECAR.API/appsettings.json` trae los valores generales. Los secretos **no** se escriben en ese archivo: van en
variables de entorno del servidor (el `:` de la clave se escribe `__`, por ejemplo `Firma__ClaveHmac`) o en el
almacén de secretos que use ECAR. Los valores que no son secretos (dirección, correo, carpetas) también pueden ir en
un `appsettings.Production.json` junto al API publicado. La **Guía de primer inicio en el servidor** (carpeta
`Documentación`) trae los comandos para generar las claves y el paso a paso completo.

| Clave | Obligatoria | Para qué |
|---|---|---|
| `ConnectionStrings__ECARConnection` | Sí | Conexión a SQL Server. |
| `JWT__Secret` | Sí | Clave de las sesiones; 32 caracteres o más. Cambiarla solo cierra las sesiones abiertas. |
| `Firma__ClaveHmac` | Sí | Clave del sello de las firmas electrónicas; 32 caracteres o más. **No se cambia nunca después de la puesta en marcha**: con otra clave, las inspecciones y novedades ya firmadas dejan de verificarse como íntegras. Guárdela con respaldo. |
| `AdminPassword` | Primer arranque | Contraseña del administrador inicial `admin@ecar.com`, que se crea si no existe (use 10 caracteres o más, con letras y números). Después se puede quitar. |
| `Cliente__BaseUrl` | Sí | Dirección de la aplicación en la intranet (por ejemplo `https://ecar-inspecciones.intranet`). Va en las etiquetas QR de los equipos y en los enlaces del correo. |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Detrás de un proxy | `true` cuando el API va detrás de un proxy inverso (como en la publicación de abajo): así la auditoría registra la IP del usuario y no la del servidor. El API debe escuchar solo en `127.0.0.1`. |
| `Correo__Usuario`, `Correo__Password`, `Correo__Remitente` | Para enviar correo | Cuenta que envía el aviso de novedades. Sin usuario y contraseña el aviso no sale: se guarda como `.eml` en `Correo:CarpetaSalida` y el Administrador lo reenvía después desde la novedad. |
| `Correo__Destinatarios__0` (`__1`, …) | Para enviar correo | Quién recibe el aviso de novedades (la mesa de ayuda). |
| `Correo__Servidor`, `Correo__Puerto`, `Correo__UsarSsl` | No | Servidor SMTP; por defecto `smtp.gmail.com`, 587, STARTTLS. |
| `Evidencias__RutaBase` | Recomendada | Carpeta de las fotografías. Use una ruta absoluta fuera de la carpeta de la aplicación e inclúyala en las copias de seguridad. |
| `ECARAuthentication__Mode` | No | `SgiIdentity` (el de `appsettings.json`): las contraseñas las valida el servicio corporativo SGI.Identity; ver la sección siguiente. También admite `Local`, `ActiveDirectory` o `Hybrid` (con `ActiveDirectory__*`). |
| `SgiIdentity__BaseUrl` | Con SGI.Identity | URL del servicio en ese ambiente, tal como la entrega TI. |
| `SgiIdentity__PermitirHttp` | Si la URL es `http://` | `false` por defecto: con una URL `http` el API no arranca. Con `true` la admite y lo avisa en el registro en cada arranque. Por HTTP, las contraseñas de los usuarios y las credenciales de ECAR viajan sin cifrar por la red: úselo solo mientras TI no publique SGI.Identity por HTTPS (su documento de arquitectura, §10.3, lo deja pendiente); después cambie la URL y quite la opción. |
| `SgiIdentity__ClientId` | Con SGI.Identity | Identificador técnico de ECAR en SGI.Identity (lo entrega TI, uno por ambiente). Es parte de las credenciales de ECAR y se trata como confidencial: **nunca** en `appsettings`; variable de entorno o almacén de secretos, igual que el `ClientSecret`. |
| `SgiIdentity__ClientSecret` | Con SGI.Identity | Secreto técnico de ECAR. **Nunca** en `appsettings`: variable de entorno o almacén de secretos. El `ClientId` y el `ClientSecret` solo salen del API hacia SGI.Identity; no llegan al navegador ni a los registros. |
| `SgiIdentity__Grupos__<grupo>` | Con SGI.Identity | Qué rol de ECAR da cada grupo de SGI.Identity, p. ej. `SgiIdentity__Grupos__ECAR_Operadores` = `Operador`. En `appsettings`: `"Grupos": { "ECAR_Operadores": "Operador", "ECAR_Administradores": "Administrador" }`. Si se deja vacío, un grupo que se llame como el rol (Administrador, Supervisor, Estándar, Operador; sin importar mayúsculas ni tildes) da ese rol. |
| `SgiIdentity__PermitirCuentasLocales` | No | `true` por defecto: las cuentas con contraseña guardada en ECAR (`admin@ecar.com`) siguen entrando, como acceso de respaldo. Con `false` solo entran usuarios de SGI.Identity. |
| `SgiIdentity__CrearUsuariosAlEntrar` | No | `true` por defecto: el primer inicio de sesión registra al usuario en ECAR. Con `false`, el Administrador debe registrarlo antes. |
| `SgiIdentity__DominioCorreo`, `SgiIdentity__TimeoutSegundos` | No | Dominio del correo provisional de los usuarios registrados al entrar (`sgi.local`) y espera máxima de cada llamada (15 s). |
| `Seguridad__*` | No | Política de cuentas: contraseña de 10 caracteres o más con letras y números, bloqueo de 30 minutos tras 5 intentos fallidos y cierre de sesión tras 15 minutos sin actividad. |

## Inicio de sesión con SGI.Identity

Con `ECARAuthentication:Mode` = `SgiIdentity`, ECAR usa el servicio corporativo SGI.Identity (guía de integración
v1.0). El API lo llama desde el servidor con `Authorization: Basic` (ClientId y ClientSecret); el navegador nunca ve
esas credenciales. El API no arranca en este modo sin URL, ClientId y ClientSecret, o si la tabla de grupos nombra
un rol que no existe.

- **Inicio de sesión.** El usuario escribe su usuario de SGI.Identity (o el de Active Directory, si SGI.Identity lo
  admite) y su contraseña. Si SGI.Identity lo autentica, ECAR crea su propia sesión con los roles que dan sus grupos.
  Sin un grupo con rol en ECAR no entra, y el mensaje le pide a Soporte T.I. que lo agreguen al grupo.
- **Roles.** Salen de los grupos y se actualizan en cada inicio de sesión. Si cambian, las sesiones abiertas del
  usuario se cierran. Lo que el Administrador cambie en «Usuarios» o «Roles de Usuario» dura hasta que el usuario
  vuelva a entrar.
- **Usuarios y nombres.** El primer inicio de sesión registra al usuario en ECAR con el nombre que envía SGI.Identity
  (`displayName`) y un correo provisional (`usuario@sgi.local`; ECAR identifica a la gente por su usuario, no por el
  correo). Ese nombre es el que queda en cada firma electrónica, y se actualiza en cada inicio de sesión: si el
  Administrador lo cambia en «Usuarios», vuelve al de SGI.Identity cuando la persona entre de nuevo. Si SGI.Identity
  no envía el nombre de una cuenta, esa persona queda con su usuario como nombre: hay que pedirle a TI que lo
  complete en SGI.Identity. La auditoría, el diálogo de firma y los filtros nombran a cada persona como
  «Nombre (usuario)».
- **Contraseña vencida o cuenta nueva.** Solo si SGI.Identity responde `PasswordExpired` o `RequiresPasswordChange`,
  el login abre «Cambiar contraseña» (pide la actual y la nueva). Solo si responde `RequiresInitialPasswordSetup`, abre «Activar su cuenta»
  (pide el número de identificación y la nueva). Las reglas, el historial y la reutilización las valida SGI.Identity,
  y sus mensajes se muestran tal cual. Ninguna de las dos crea sesión: al terminar se vuelve a iniciar sesión. Las
  dos pantallas solo abren con un pase de 10 minutos que da el login para ese usuario; entrando por la URL vuelven al
  login. Las contraseñas de Active Directory se cambian con Soporte T.I., no aquí.
- **Bloqueo y otros estados.** El bloqueo lo lleva SGI.Identity: con `UserLocked` el login y la firma responden con
  su mensaje (423). ECAR ya no cuenta intentos para estos usuarios; «Desbloquear» solo aplica a las cuentas locales.
  Con `UserInactive` (cuenta inactiva) o `UserWithoutGroups` (sin grupos) no se entra ni se firma (403).
- **Firmas.** Firmar una inspección y resolver una novedad vuelven a validar la contraseña con SGI.Identity en ese
  momento. Además, los grupos del usuario deben seguir dando un rol que permita esa firma. Contraseña errónea: 400.
  Bloqueado: 423. Contraseña vencida o sin rol: 403. Servicio caído o que rechaza a ECAR: 503. En ningún caso se
  firma, y el diálogo conserva la firma dibujada.
- **Auditoría.** Cada intento queda registrado con el estado que devolvió SGI.Identity (y los grupos y roles, si
  entró), al igual que los cambios de contraseña con su resultado. Nunca se guardan contraseñas ni el número de
  identificación.

## Base de datos

El API crea la base y su esquema al arrancar la primera vez (migración `EsquemaInicial`) y la actualiza en las
versiones siguientes; el usuario de SQL Server necesita permiso para crear tablas en ese primer arranque.

La base empieza vacía. El API solo crea, si faltan, el administrador inicial (`admin@ecar.com`, con la contraseña de
`AdminPassword`) y los cuatro roles del sistema (Administrador, Estándar, Operador, Supervisor), de los que dependen
los permisos. Todo lo demás lo registra el Administrador desde la aplicación: categorías, ubicaciones, equipos,
checklists y usuarios.

Si el DBA prefiere crear la base él mismo (y dejar a la cuenta del API solo con lectura y escritura), puede usar
`BaseDeDatos/ECAR_CrearBaseDeDatos.sql`. En una sola transacción crea la base, las 14 tablas, el trigger de la
auditoría, el historial de migraciones (así el API no intenta crearla otra vez), los cuatro roles y `admin@ecar.com`
con su rol, y deja esas altas en la auditoría con su cadena de integridad. Solo trabaja sobre una base nueva o vacía:
si encuentra tablas, se detiene sin cambiar nada. La contraseña del administrador no va en el script, porque SQL
Server no calcula el hash BCrypt: el API se la pone en su primer arranque con `AdminPassword` (también queda en la
auditoría), y hasta entonces `admin@ecar.com` no puede entrar.

```bash
sqlcmd -S <servidor\instancia> -E -i BaseDeDatos/ECAR_CrearBaseDeDatos.sql
```

En SQL Server Management Studio, abra el archivo y active Consulta > Modo SQLCMD antes de ejecutarlo. Para otro
nombre de base, cambie `:setvar BaseDatos` al inicio del archivo (y la cadena de conexión del API). Al final del
archivo hay una sección opcional, comentada, con los permisos de lectura y escritura para la cuenta del API.

Para aplicar a mano los cambios de esquema de una versión nueva, el DBA puede generar el script idempotente de EF
Core. Necesita la herramienta `dotnet-ef`, compilar antes (`dotnet build`) y la misma configuración del API (conexión
y `JWT:Secret`); el API no aplica de nuevo lo que el script ya dejó registrado:

```bash
dotnet ef migrations script --idempotent --project ECAR.Infrastructure --startup-project ECAR.API -o esquema.sql
```

La auditoría solo admite inserciones: un trigger de la base impide modificar o borrar sus filas, también por SQL directo.

## Publicación

```bash
dotnet publish ECAR.API -c Release -o publicar/api
```

```bash
dotnet publish ECAR.Client -c Release -o publicar/cliente
```

- **API:** `publicar/api` se instala en el servidor (por ejemplo, como sitio o aplicación de IIS) con las
  variables de entorno de la tabla anterior.
- **Interfaz:** `publicar/cliente` son archivos estáticos. En IIS, el sitio apunta a esa carpeta: su `web.config`
  sirve los archivos de `wwwroot` y devuelve `index.html` en las rutas de la aplicación (necesita *URL Rewrite*).
- **Mismo origen:** la interfaz llama al API en su misma dirección, bajo `/api` (por ejemplo,
  `https://ecar-inspecciones.intranet/api/...`). El servidor web debe enviar todo lo que empiece por `/api` al API
  (en IIS, con *Application Request Routing* como proxy inverso, con esa regla antes de las del `web.config`). Si el
  API va en otra dirección, ponga `"ApiBaseUrl": "https://..."` en `wwwroot/appsettings.json` de la interfaz y
  agregue ese origen a la política CORS de `ECAR.API/Program.cs`, que hoy solo admite `localhost`.

## Prueba en un equipo local

Ver «Puesta en marcha del entorno de pruebas», al principio. En este entorno la interfaz queda en
`https://localhost:7367` y el API en `https://localhost:7396`. Abierta desde `localhost`, la interfaz busca el API
en esa dirección.

## Copias de seguridad

Respalde **juntas** la base de datos y la carpeta de evidencias (`Evidencias:RutaBase`): el sello de cada
inspección firmada incluye el SHA-256 de sus fotografías, así que una foto perdida o cambiada hace fallar la
verificación de integridad de esa inspección.

## Notas de operación

- Las fechas se guardan en UTC y se muestran en hora de Colombia (UTC−5).
- Las contraseñas de los usuarios de SGI.Identity las valida, bloquea, hace caducar y cambia ese servicio. ECAR solo
  conserva la de las cuentas locales (`admin@ecar.com`), sin caducidad, y el Administrador desbloquea esas cuentas
  cuando se bloquean por intentos fallidos.
- Nada se borra físicamente: las inspecciones y novedades se anulan y las evidencias se retiran, siempre con motivo,
  y todo queda en la auditoría.
- Con `ASPNETCORE_ENVIRONMENT=Development` el API publica su documentación en `/scalar`; en producción no.
- Con el registro por defecto (`Logging:LogLevel:Default` = `Information`) el API escribe cada consulta SQL en su
  registro. En producción puede agregar `"Microsoft.EntityFrameworkCore": "Warning"` en `Logging:LogLevel`.
- La interfaz usa la fuente Roboto de Google Fonts; sin salida a internet se ve con la fuente del sistema.

## Pendiente para la puesta en marcha

- La cuenta de correo del aplicativo (`Correo:Usuario`, `Correo:Password`, `Correo:Remitente`) y el destinatario
  de los avisos (`Correo:Destinatarios`, vacío en `appsettings.json`: mientras no se configure, el aviso no se envía
  y la novedad queda pendiente de notificar, para reenviarla después).
- La dirección de la intranet (`Cliente:BaseUrl`), que se conoce al desplegar.
- De SGI.Identity, para cada ambiente: la URL, el `ClientId`, el `ClientSecret` y los nombres de los grupos que dan
  cada rol (`SgiIdentity:Grupos`). Y que TI lo publique por HTTPS: hoy solo está por HTTP
  (`SgiIdentity:PermitirHttp`); cuando haya HTTPS, el servidor de ECAR debe confiar en su certificado.
- Sin probar en el entorno de ECAR: el envío por el servidor SMTP real, el directorio activo, SGI.Identity real (la
  integración se probó con un simulador que sigue su guía de integración y su documento de arquitectura v1.0) y la cámara de un celular Android con la dirección
  definitiva.

## Créditos

Sistema desarrollado por el equipo del proyecto (BE = backend, FE = frontend; 0 = líder). Repositorio de desarrollo: [carlostamayo1295/Proyecto_ECAR](https://github.com/carlostamayo1295/Proyecto_ECAR).
La misma lista está en el código (`ECAR.Shared/Creditos.cs`) y en la pantalla **Créditos** del menú de la aplicación.

| Código | Integrante | Rol | GitHub |
|---|---|---|---|
| BE-0 | Carlos Tamayo | Líder de backend: arquitectura del API, migraciones, auditoría transaccional y sello de seguridad de las sesiones. | [@carlostamayo1295](https://github.com/carlostamayo1295) |
| BE-1 | Juan David López | Hallazgos y novedades, anulación de inspecciones, consultas de auditoría y búsqueda de usuarios para reportes. | [@juandalopeztrujillo-code](https://github.com/juandalopeztrujillo-code) |
| BE-2 | Alejandro Gómez | Reportes en PDF y Excel, registro PDF de la inspección, tablero en PDF y retiro de evidencias. | [@alejogomez21](https://github.com/alejogomez21) |
| BE-3 | Simón Guarín | Firma electrónica de la Parte 11 (HMAC, reautenticación) y seguridad de cuentas. | [@sguarin757](https://github.com/sguarin757) |
| FE-0 | Juan Alberto Zuluaga | Líder de frontend: DTOs y servicios del cliente, integración de ramas, pruebas integrales y entrega. | [@JuanZuAl](https://github.com/JuanZuAl) |
| FE-1 | Erica Avendaño | Pantallas de hallazgos y novedades, colores por tipo de acción y formularios. | [@ericamarcelaavendano-cmd](https://github.com/ericamarcelaavendano-cmd) |
| FE-2 | Santiago Arango | Pantallas de reportes y de evidencias retiradas; reportes de QA. | [@backyardignas](https://github.com/backyardignas) |
| FE-3 | Tatiana Suárez | Firma en pantalla, resultado de la inspección y cuentas; reportes de pruebas. | [@Gary1913](https://github.com/Gary1913) |
