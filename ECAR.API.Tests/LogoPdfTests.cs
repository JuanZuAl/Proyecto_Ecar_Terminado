using ECAR.API.Services.Documentos;
using ECAR.Shared.DTOs;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Advanced;
using PdfSharp.Pdf.IO;
using Xunit;

namespace ECAR.API.Tests;

/// <summary>
/// 09/11/2026: todos los PDF del sistema (reportes, auditoría, tablero y registro de la inspección) llevan en el
/// encabezado de cada página el logo de ECAR de la pantalla de inicio. Todos se arman con ExportadorTabla.Documento.
/// </summary>
public class LogoPdfTests
{
    private static PieDocumento Pie() => new("Prueba", "ninguno", "Carla Admin", DateTime.UtcNow);

    [Fact]
    public void El_logo_de_la_pantalla_de_inicio_va_incrustado_en_el_API()
    {
        var logo = ExportadorTabla.Logo;

        Assert.NotNull(logo);
        var esperado = File.ReadAllBytes(Path.Combine(RaizSolucion(), "ECAR.Client", "wwwroot", "images", "logo-ecar.png"));
        Assert.Equal(esperado, logo);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void El_encabezado_lleva_el_logo_a_escala_y_el_contenido_empieza_debajo(bool horizontal)
    {
        var seccion = ExportadorTabla.Documento("Prueba", Pie(), horizontal).LastSection;

        var imagen = Assert.Single(ImagenesDelEncabezado(seccion));
        Assert.Equal(ExportadorTabla.AltoLogoCm, imagen.Height.Centimeter, 3);
        Assert.True(imagen.LockAspectRatio);
        Assert.True(seccion.PageSetup.TopMargin.Centimeter >= seccion.PageSetup.HeaderDistance.Centimeter + ExportadorTabla.AltoLogoCm);
    }

    [Fact]
    public void El_tablero_usa_el_mismo_encabezado()
    {
        var tablero = new DashboardDto { Periodo = "noviembre de 2026" };

        var seccion = TableroPdf.Crear(tablero, "todas las plantas", "Sara Supervisora", DateTime.UtcNow).LastSection;

        Assert.Single(ImagenesDelEncabezado(seccion));
    }

    [Fact]
    public void Cada_pagina_de_un_reporte_largo_lleva_el_logo()
    {
        var filas = Enumerable.Range(1, 150)
            .Select(i => (IReadOnlyList<object?>)[i, $"Equipo {i}", "Planta Principal - Pesaje"])
            .ToList();

        var bytes = ExportadorTabla.Pdf("Reporte de prueba", [new("N.º"), new("Equipo"), new("Ubicación")], filas, Pie());

        using var pdf = PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount > 1);
        Assert.All(pdf.Pages.Cast<PdfPage>(), pagina => Assert.True(TieneImagen(pagina), "una página sin el logo"));
    }

    private static IEnumerable<Image> ImagenesDelEncabezado(Section seccion) =>
        seccion.Headers.Primary.Elements.OfType<Paragraph>().SelectMany(p => p.Elements.OfType<Image>());

    private static bool TieneImagen(PdfPage pagina)
    {
        var xobjetos = pagina.Elements.GetDictionary("/Resources")?.Elements.GetDictionary("/XObject");
        return xobjetos != null && xobjetos.Elements.Values
            .Select(v => (v as PdfReference)?.Value as PdfDictionary ?? v as PdfDictionary)
            .Any(d => d?.Elements.GetName("/Subtype") == "/Image");
    }

    private static string RaizSolucion()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta != null && !File.Exists(Path.Combine(carpeta.FullName, "ECAR.AuditoriaEquipos.slnx")))
        {
            carpeta = carpeta.Parent;
        }

        return carpeta?.FullName ?? throw new InvalidOperationException("No se encontró la carpeta de la solución");
    }
}
