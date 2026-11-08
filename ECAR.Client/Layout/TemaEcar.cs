using MudBlazor;

namespace ECAR.Client.Layout;

/// <summary>
/// El tema de MudBlazor de ECAR, compartido por los dos layouts (MainLayout y EmptyLayout: sin tema, la pantalla
/// pública del QR salía con las tarjetas transparentes). Los colores son los originales de la marca: el azul
/// #397FDE (barra y botones) y el #2A5FB8 (menú lateral); el resto, los de MudBlazor.
/// </summary>
/// <remarks>
/// El primario es el azul de ECAR (con el morado por defecto salían en morado los campos enfocados, los
/// interruptores activos y los botones Outlined y Text: Fase 5, F-01). Error y Warning son lo bastante oscuros
/// para llevar texto blanco encima (contraste ≥ 4,5), como ya eran los botones.
/// </remarks>
public static class TemaEcar
{
    public static readonly MudTheme Tema = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#397FDE",
            PrimaryDarken = "#2A5FB8",
            PrimaryLighten = "#6BA0E6",
            Warning = "#B35400",
            Error = "#C62828",
            AppbarBackground = "#397FDE",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#2A5FB8",
            DrawerText = "#FFFFFF",
            DrawerIcon = "rgba(255,255,255,0.8)"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px"
        },
        Typography = new Typography
        {
            H4 = new H4Typography { FontSize = "1.75rem", FontWeight = "700", LineHeight = "1.2", LetterSpacing = "-0.01em" },
            H5 = new H5Typography { FontSize = "1.3rem", FontWeight = "700", LineHeight = "1.3", LetterSpacing = "-0.005em" },
            H6 = new H6Typography { FontSize = "1.0625rem", FontWeight = "700", LineHeight = "1.4", LetterSpacing = "0" },
            Button = new ButtonTypography { FontSize = "0.875rem", FontWeight = "500", LetterSpacing = "0.01em", TextTransform = "none" }
        }
    };
}
