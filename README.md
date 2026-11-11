# ECAR — Sistema de Gestión de Equipos e Inspecciones

Laboratorios ECAR S.A. · Entrega final del proyecto (noviembre de 2026).

| Carpeta | Qué es |
|---|---|
| [`Cliente PROD`](Cliente%20PROD/README.md) | Versión para el servidor de ECAR: código (.NET 10, Blazor WebAssembly, SQL Server), script de creación de la base (`BaseDeDatos/`) y documentación en Word y PDF (`Documentación/`: gestión e inicio del proyecto, documentación técnica, manual de usuario y guía de primer inicio en el servidor). Su README explica la instalación y la configuración. |
| [`Cliente TEST`](Cliente%20TEST/README.md) | Entorno de pruebas del inicio de sesión con SGI.Identity: el mismo código con otros puertos y otra base, y `configurar-secretos.ps1` para cargar sus secretos. |

## Secretos

El repositorio no contiene claves ni contraseñas. `JWT:Secret`, `Firma:ClaveHmac`, `AdminPassword`, el `ClientId` y el
`ClientSecret` de SGI.Identity, la cadena de conexión con contraseña y la cuenta de correo se cargan como user-secrets
en un equipo de desarrollo o como variables de entorno en el servidor (README de cada carpeta y Guía de primer inicio,
pasos 5 y 6). Los archivos `appsettings.Development.json` y `appsettings.Production.json` no se versionan.

## Créditos

Sistema desarrollado por el equipo del proyecto (BE = backend, FE = frontend; 0 = líder). Repositorio de desarrollo: [carlostamayo1295/Proyecto_ECAR](https://github.com/carlostamayo1295/Proyecto_ECAR).
La misma lista está en el código (`ECAR.Shared/Creditos.cs` de cada carpeta) y en la pantalla **Créditos** del menú de la aplicación.

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
