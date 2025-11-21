using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ProyectoCGAPYS.Models; // Asegúrate que este sea el namespace de tus modelos
using ProyectoCGAPYS.ViewModels; // Y este el de tus ViewModels
using System.Collections.Generic;
using System.IO;

// Clase que define la estructura de nuestro documento PDF
public class ProjectReportDocument : IDocument
{
    private readonly Proyectos _proyecto;
    private readonly List<ProyectoImagen> _imagenes;
    private readonly string _wwwRootPath;

    public ProjectReportDocument(Proyectos proyecto, List<ProyectoImagen> imagenes, string wwwRootPath)
    {
        _proyecto = proyecto;
        _imagenes = imagenes;
        _wwwRootPath = wwwRootPath;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    // Aquí se construye el diseño del PDF
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            // --- Configuración de la página ---
            page.Margin(50);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

            // --- Cabecera ---
            page.Header().Element(ComposeHeader);

            // --- Contenido Principal ---
            page.Content().Element(ComposeContent);

            // --- Pie de Página ---
            page.Footer().AlignCenter().Text(x =>
            {
                x.Span("Página ");
                x.CurrentPageNumber();
            });
        });
    }

    void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"Reporte de Proyecto: {_proyecto.NombreProyecto}")
                    .Bold().FontSize(20).FontColor(Colors.Blue.Darken2);

                column.Item().Text($"ID de Proyecto: {_proyecto.Id}");
                column.Item().Text($"Fecha de Generación: {System.DateTime.Now:dd/MM/yyyy}");
            });
        });
    }

    void ComposeContent(IContainer container)
    {
        container.PaddingVertical(40).Column(column =>
        {
            column.Spacing(20);

            // --- Sección de Detalles del Proyecto ---
            column.Item().Element(ComposeProjectDetails);

            // --- Sección de Informe (con Lorem Ipsum) ---
            column.Item().Element(ComposeReportText);

            // --- Page Break antes de las imágenes ---
            column.Item().PageBreak();

            // --- Sección de Imágenes ---
            column.Item().Element(ComposeImages);
        });
    }

    void ComposeProjectDetails(IContainer container)
    {
        container.Grid(grid =>
        {
            grid.VerticalSpacing(5);
            grid.HorizontalSpacing(5);
            grid.Columns(2); // Dos columnas

            grid.Item(1).Text("Estatus:").SemiBold();
            grid.Item(1).Text(_proyecto.Estatus);

            grid.Item(1).Text("Presupuesto:").SemiBold();
            grid.Item(1).Text($"{_proyecto.Presupuesto:C}");

            grid.Item(1).Text("Responsable:").SemiBold();
            grid.Item(1).Text(_proyecto.NombreResponsable);

            grid.Item(1).Text("Descripción:").SemiBold();
            grid.Item(11).Text(_proyecto.Descripcion); // Ocupa 11 celdas para que se expanda
        });
    }

    void ComposeReportText(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(10).Text("Informe de Avances").Bold().FontSize(16);
            column.Item().Text(Placeholders.LoremIpsum()); // Texto de relleno
        });
    }

    void ComposeImages(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Imágenes del Proyecto").Bold().FontSize(16);
            /* 
            foreach (var imagen in _imagenes)
            {
                // Leemos la imagen desde wwwroot
                var imagePath = Path.Combine(_wwwRootPath, imagen.ImagenUrl.TrimStart('/'));
                if (File.Exists(imagePath))
                {
                    column.Item().PaddingTop(10).Image(imagePath).FitWidth();
                }
            }
            */
        });
    }
}