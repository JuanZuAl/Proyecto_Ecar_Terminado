# Entorno de pruebas de ECAR con SGI.Identity: guarda los secretos en los user-secrets de ECAR.API.
#
# Los user-secrets de este entorno son solo suyos (UserSecretsId propio en ECAR.API.csproj): no se mezclan con los del
# proyecto de desarrollo ni con los de la copia para el cliente. Quedan en
#   %APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json
# fuera de la carpeta del proyecto, así que no se copian ni se suben con el código.
#
# Qué hace:
#   - Pide el ClientId y el ClientSecret de SGI.Identity (las credenciales técnicas de ECAR, que entrega TI) y la
#     contraseña del admin local (admin@ecar.com), sin mostrarlos. Enter deja el valor que ya hubiera.
#   - Genera JWT:Secret y Firma:ClaveHmac (64 caracteres hexadecimales) si todavía no existen. Firma:ClaveHmac
#     nunca se cambia después: con otra clave, las firmas ya hechas en este entorno dejan de verificarse.
#   - No imprime ningún secreto y no los pasa por la línea de comandos (no quedan en el historial).
#
# Lo que NO es confidencial va en ECAR.API\appsettings.Development.json: SgiIdentity:BaseUrl y SgiIdentity:Grupos
# (y la conexión a la base ECARDB_PruebaCliente). El ClientId no va ahí: se guarda aquí, junto al ClientSecret.
#
# Uso, desde esta carpeta:   powershell -ExecutionPolicy Bypass -File .\configurar-secretos.ps1
param(
    [SecureString] $ClientId,
    [SecureString] $ClientSecret,
    [SecureString] $AdminPassword
)

$ErrorActionPreference = 'Stop'

$csproj = Join-Path $PSScriptRoot 'ECAR.API\ECAR.API.csproj'
$coincidencia = Select-String -LiteralPath $csproj -Pattern '<UserSecretsId>([^<]+)</UserSecretsId>'
if (-not $coincidencia) { throw "No se encontró UserSecretsId en $csproj" }
$id = $coincidencia.Matches[0].Groups[1].Value
$carpeta = Join-Path $env:APPDATA "Microsoft\UserSecrets\$id"
$archivo = Join-Path $carpeta 'secrets.json'

function Convertir-ATexto([SecureString] $seguro) {
    if (-not $seguro -or $seguro.Length -eq 0) { return $null }
    $puntero = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($seguro)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($puntero) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($puntero) }
}

function Nueva-Clave {
    $bytes = New-Object byte[] 32
    $generador = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generador.GetBytes($bytes) } finally { $generador.Dispose() }
    return -join ($bytes | ForEach-Object { $_.ToString('x2') })
}

# Lo que ya hay (claves planas, como las guarda «dotnet user-secrets»: "SgiIdentity:ClientSecret").
$secretos = [ordered]@{}
if (Test-Path -LiteralPath $archivo) {
    $actual = Get-Content -LiteralPath $archivo -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($actual) { foreach ($p in $actual.PSObject.Properties) { $secretos[$p.Name] = [string]$p.Value } }
}

# Los avisos se muestran al final, cuando ya se guardó: si algo falla antes, no se guarda nada.
$avisos = New-Object System.Collections.Generic.List[string]
if (-not $secretos.Contains('JWT:Secret')) { $secretos['JWT:Secret'] = Nueva-Clave; $avisos.Add('JWT:Secret generado.') }
else { $avisos.Add('JWT:Secret ya existía: se deja.') }
if (-not $secretos.Contains('Firma:ClaveHmac')) { $secretos['Firma:ClaveHmac'] = Nueva-Clave; $avisos.Add('Firma:ClaveHmac generado.') }
else { $avisos.Add('Firma:ClaveHmac ya existía: se deja (no se cambia nunca).') }

if (-not $PSBoundParameters.ContainsKey('ClientId')) {
    $ClientId = Read-Host -AsSecureString 'ClientId de ECAR en SGI.Identity (Enter para dejar el actual)'
}
$texto = Convertir-ATexto $ClientId
if ($texto) { $secretos['SgiIdentity:ClientId'] = $texto.Trim(); $avisos.Add('SgiIdentity:ClientId guardado.') }

if (-not $PSBoundParameters.ContainsKey('ClientSecret')) {
    $ClientSecret = Read-Host -AsSecureString 'ClientSecret de SGI.Identity (Enter para dejar el actual)'
}
$texto = Convertir-ATexto $ClientSecret
if ($texto) { $secretos['SgiIdentity:ClientSecret'] = $texto; $avisos.Add('SgiIdentity:ClientSecret guardado.') }

if (-not $PSBoundParameters.ContainsKey('AdminPassword')) {
    $AdminPassword = Read-Host -AsSecureString 'Contraseña para admin@ecar.com, la cuenta local de respaldo (Enter para dejar la actual)'
}
$texto = Convertir-ATexto $AdminPassword
if ($texto) {
    if ($texto.Length -lt 10 -or $texto -notmatch '[A-Za-z]' -or $texto -notmatch '\d') {
        throw 'La contraseña del admin debe tener 10 caracteres o más, con letras y números. No se guardó nada.'
    }
    $secretos['AdminPassword'] = $texto
    $avisos.Add('AdminPassword guardada (solo se usa si admin@ecar.com todavía no existe en la base).')
}
$texto = $null

New-Item -ItemType Directory -Force -Path $carpeta | Out-Null
$json = $secretos | ConvertTo-Json
[IO.File]::WriteAllText($archivo, $json, (New-Object Text.UTF8Encoding($false)))

$avisos | ForEach-Object { Write-Host $_ }
Write-Host ''
Write-Host "Secretos de este entorno en: $archivo"
Write-Host ('Claves guardadas: ' + (($secretos.Keys | Sort-Object) -join ', '))
foreach ($falta in @('SgiIdentity:ClientId', 'SgiIdentity:ClientSecret', 'AdminPassword')) {
    if (-not $secretos.Contains($falta)) { Write-Host "Falta todavía: $falta (vuelva a ejecutar el script)." }
}
Write-Host 'Recuerde poner SgiIdentity:BaseUrl y SgiIdentity:Grupos en ECAR.API\appsettings.Development.json.'
