using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ECAR.Client;
using ECAR.Client.Services;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Sesión: SesionService decide cuándo terminó la sesión y manda al login conservando la página.
builder.Services.AddScoped<SesionService>();

// Dirección del API. Abierta en el propio equipo (localhost, entorno de pruebas), el API está en
// https://localhost:7396 (el de desarrollo usa 7296: los dos pueden correr a la vez). Abierta desde cualquier otra dirección —la de la intranet, donde el servidor
// web reenvía /api al API (proxy inverso)— el API está en el mismo origen que la página. "ApiBaseUrl"
// en wwwroot/appsettings.json, si existe, manda sobre las dos.
var origenCliente = new Uri(builder.HostEnvironment.BaseAddress);
var direccionApi = builder.Configuration["ApiBaseUrl"] is { Length: > 0 } configurada
    ? new Uri(configurada)
    : origenCliente.IsLoopback ? new Uri("https://localhost:7396") : new Uri(origenCliente.GetLeftPart(UriPartial.Authority));

// Configurar el HttpClient del API. Pasa por SesionExpiradaHandler: si el API responde 401 a una
// petición que llevaba token, la sesión venció y se vuelve al login.
builder.Services.AddScoped(sp => new HttpClient(
    new SesionExpiradaHandler(sp.GetRequiredService<SesionService>()) { InnerHandler = new HttpClientHandler() })
{
    BaseAddress = direccionApi
});

// Registrar los servicios
builder.Services.AddScoped<AuthService>();
// Lo que pidió SGI.Identity en el login (cambiar la contraseña vencida o configurar la inicial), solo en memoria.
builder.Services.AddScoped<CambioPasswordEstado>();
builder.Services.AddScoped<HttpClientService>();
builder.Services.AddScoped<AuthorizationService>();

// Configurar MudBlazor con el tema corporativo de ECAR
builder.Services.AddMudServices();

// Textos de los controles de MudBlazor en español (B-15): páginas anterior/siguiente, flechas de las
// pestañas, selectores de fecha, errores de conversión, etc.
builder.Services.AddTransient<MudLocalizer, LocalizadorMud>();

// Configurar el tema corporativo de ECAR
builder.Services.AddSingleton(new MudTheme()
{
    PaletteLight = new PaletteLight()
    {
        Primary = "#397FDE",
        Secondary = "#79B75D",
        Info = "#17A2B8",
        Success = "#28A745",
        Warning = "#FFC107",
        Error = "#DC3545",
        Background = "#F4F5F7",
        Surface = "#FFFFFF",
        TextPrimary = "#16438C",
        TextSecondary = "#5A6A7C"
    },
    PaletteDark = new PaletteDark()
    {
        Primary = "#6BA0E6",
        Secondary = "#96C982",
        Info = "#4ECDC4",
        Success = "#6FDCE2",
        Warning = "#FFD54F",
        Error = "#FF6B6B",
        Background = "#1A1A2E",
        Surface = "#252542",
        TextPrimary = "#FFFFFF",
        TextSecondary = "#B0B0B0"
    }
});

await builder.Build().RunAsync();
