using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.ViewModels;
using QuestPDF.Fluent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting; // Necesario para IWebHostEnvironment
using QuestPDF.Fluent;
using Microsoft.Extensions.Hosting.Internal;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using Azure.Core;
using System;
using SendGrid.Helpers.Mail;
using SendGrid;
using Microsoft.AspNetCore.Authorization;

namespace ProyectoCGAPYS.Controllers
{
    [Authorize(Roles = "Jefa")]

    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly ILogger<DashboardController> _logger;
        private readonly IServiceProvider _serviceProvider;
        private static readonly ConcurrentDictionary<string, string> ReportGenerationStatus = new ConcurrentDictionary<string, string>();
        private readonly IConfiguration _configuration;
        // Inyectamos el contexto de la base de datos
        public DashboardController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment  , ILogger<DashboardController> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            
        }

        public async Task<IActionResult> Index()
        {
            // Creamos una instancia del ViewModel principal que enviaremos a la vista
            var dashboardViewModel = new DashboardViewModel();

            // 1. Obtener KPIs Principales
            // Usamos 'FromSqlRaw' para llamar al procedimiento. Asumimos el año fiscal 2025.
            var kpis = await _context.Set<KPIsViewModel>()
                                     .FromSqlRaw("EXEC sp_GetDashboard_KPIsPrincipales @AnioFiscal = {0}", 2025)
                                     .ToListAsync();
            dashboardViewModel.KPIs = kpis.FirstOrDefault();

            // 2. Obtener Estado por Fondo
            dashboardViewModel.EstadoPorFondo = await _context.Set<FondoViewModel>()
                                                              .FromSqlRaw("EXEC sp_GetDashboard_EstadoPorFondo")
                                                              .ToListAsync();

            // 3. Obtener Proyectos por Fase
            dashboardViewModel.ProyectosPorFase = await _context.Set<FaseViewModel>()
                                                                .FromSqlRaw("EXEC sp_GetDashboard_ProyectosPorFase")
                                                                .ToListAsync();

            // 4. Obtener Proyectos por Vencer
            dashboardViewModel.ProyectosPorVencer = await _context.Set<ProyectoAlertaViewModel>()
                                                                  .FromSqlRaw("EXEC sp_GetDashboard_ProyectosPorVencer")
                                                                  .ToListAsync();

            // 5. Obtener Estimaciones Pendientes
            dashboardViewModel.EstimacionesPendientes = await _context.Set<ProyectoAlertaViewModel>()
                                                                       .FromSqlRaw("EXEC sp_GetDashboard_EstimacionesPendientes")
                                                                       .ToListAsync();

            // Pasamos el ViewModel completamente poblado a la vista
            return View(dashboardViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetProyectosPorFase(string fase)
        {
            if (string.IsNullOrEmpty(fase))
            {
                return BadRequest("El nombre de la fase es requerido.");
            }

            // Llamamos al nuevo SP usando el DbSet<ProyectoSimpleViewModel> que registramos
            var proyectos = await _context.ProyectosSimples
                .FromSqlInterpolated($"EXEC dbo.sp_GetProyectosPorNombreFase @NombreFase = {fase}")
                .ToListAsync();

            // Devolvemos los resultados en formato JSON para que JavaScript los pueda usar
            return Json(proyectos);
        }
        [HttpGet]
        public IActionResult IniciarGeneracionPdf(string proyectoId)
        {
            if (string.IsNullOrEmpty(proyectoId)) return BadRequest();

            var jobId = Guid.NewGuid().ToString();
            ReportGenerationStatus[jobId] = "Procesando";

            Task.Run(async () =>
            {
                // --- INICIO DE LA SOLUCIÓN ---
                // 1. Creamos un nuevo "ámbito" para esta tarea
                using (var scope = _serviceProvider.CreateScope())
                {
                    // 2. Pedimos un DbContext nuevo que solo existirá dentro de este ámbito
                    var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var scopedWebHostEnvironment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                    var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<DashboardController>>();
                    // --- FIN DE LA SOLUCIÓN ---

                    try
                    {
                        scopedLogger.LogInformation("Iniciando trabajo de PDF para JobId: {JobId}", jobId);

                        // 3. Usamos el nuevo 'scopedContext' en lugar de '_context'
                        var proyecto = await scopedContext.Proyectos.FindAsync(proyectoId);
                        if (proyecto == null)
                        {
                            ReportGenerationStatus[jobId] = "Error: Proyecto no encontrado";
                            return;
                        }

                        var imagenes = await scopedContext.ProyectoImagenes
                                                         .Where(img => img.IdProyectoFk == proyectoId)
                                                         .ToListAsync();

                        var wwwRootPath = scopedWebHostEnvironment.WebRootPath;
                        var document = new ProjectReportDocument(proyecto, imagenes, wwwRootPath);

                        var pdfBytes = document.GeneratePdf();

                        var tempFolderPath = Path.Combine(wwwRootPath, "temp_reports");
                        Directory.CreateDirectory(tempFolderPath);

                        var fileName = $"{jobId}.pdf";
                        var filePath = Path.Combine(tempFolderPath, fileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);

                        scopedLogger.LogInformation("PDF guardado exitosamente para JobId: {JobId}", jobId);
                        ReportGenerationStatus[jobId] = $"/temp_reports/{fileName}";
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.LogError(ex, "Falló la generación de PDF para JobId: {JobId}", jobId);
                        ReportGenerationStatus[jobId] = $"Error: {ex.Message}";
                    }
                } // El 'using' se encarga de cerrar y desechar el 'scopedContext' de forma segura
            });

            return Json(new { jobId = jobId });
        }


        // --- MÉTODO 2: Permite al navegador preguntar si el trabajo ya terminó ---
        [HttpGet]
        public IActionResult VerificarEstadoPdf(string jobId)
        {
            if (string.IsNullOrEmpty(jobId) || !ReportGenerationStatus.TryGetValue(jobId, out var status))
            {
                return NotFound();
            }

            // Si el estado es una URL (empieza con '/'), significa que está listo
            if (status.StartsWith("/"))
            {
                return Json(new { status = "Listo", url = status });
            }

            // Si contiene "Error", lo notificamos
            if (status.StartsWith("Error"))
            {
                return Json(new { status = "Error", message = status });
            }

            // De lo contrario, sigue procesando
            return Json(new { status = "Procesando" });
        }
        [HttpGet]
        public async Task<IActionResult> ReportePdfView(string proyectoId)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.Imagenes) // Incluimos las imágenes relacionadas
                .FirstOrDefaultAsync(p => p.Id == proyectoId);

            if (proyecto == null) return NotFound();

            return View(proyecto);
        }


        // --- MÉTODO 2: El que el usuario llama para descargar el PDF ---
        [HttpGet]
        public async Task<IActionResult> GenerarReporteHtmlAPdf(string proyectoId)
        {
            // 1. Obtenemos la URL completa de nuestra vista de reporte
            var urlDeLaVista = Url.Action("ReportePdfView", "Dashboard", new { proyectoId = proyectoId }, Request.Scheme);

            // 2. Usamos Playwright para "imprimir" esa URL a un PDF
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(); // Podemos usar new() { Headless = false } para ver lo que hace
            var page = await browser.NewPageAsync();
            await page.GotoAsync(urlDeLaVista);

            var pdfBytes = await page.PdfAsync(new PagePdfOptions { PrintBackground = true, Format = "Letter" });
            await browser.CloseAsync();

            // 3. Devolvemos el archivo PDF generado
            return File(pdfBytes, "application/pdf", $"Reporte-{proyectoId}.pdf");
        }
        [HttpPost]
        public async Task<IActionResult> EnviarReportePorCorreo([FromBody] EnvioCorreoRequest request)
        {
            try
            {
                // --- PASO A: Generar el PDF en memoria (sin cambios) ---
                var urlDeLaVista = Url.Action("ReportePdfView", "Dashboard", new { proyectoId = request.ProyectoId }, Request.Scheme);
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync();
                var page = await browser.NewPageAsync();
                await page.GotoAsync(urlDeLaVista);
                var pdfBytes = await page.PdfAsync(new PagePdfOptions { PrintBackground = true, Format = "Letter" });
                await browser.CloseAsync();

                // --- PASO B: Enviar el correo con el PDF adjunto (con logging mejorado) ---
                var apiKey = _configuration["SendGrid:ApiKey"];
                var client = new SendGridClient(apiKey);

                var from = new EmailAddress(_configuration["SendGrid:FromEmail"], _configuration["SendGrid:FromName"]);
                var to = new EmailAddress(request.EmailDestino);

                var subject = $"Reporte del Proyecto: {request.ProyectoId}";
                var plainTextContent = "Se adjunta el reporte del proyecto solicitado.";
                var htmlContent = "<strong>Se adjunta el reporte del proyecto solicitado.</strong>";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                msg.AddAttachment($"Reporte-{request.ProyectoId}.pdf", Convert.ToBase64String(pdfBytes), "application/pdf");

                var response = await client.SendEmailAsync(msg);

                // --- LÓGICA DE DIAGNÓSTICO MEJORADA ---
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Correo enviado exitosamente a {Email}", request.EmailDestino);
                    return Ok(new { message = "Correo enviado exitosamente." });
                }
                else
                {
                    // Leemos el cuerpo del error que nos devuelve SendGrid
                    string responseBody = await response.Body.ReadAsStringAsync();

                    // Registramos el error detallado en nuestra consola de Visual Studio
                    _logger.LogError("SendGrid falló al enviar el correo. Código: {StatusCode}. Respuesta: {ResponseBody}", response.StatusCode, responseBody);

                    // Devolvemos un mensaje de error más específico
                    return StatusCode(500, new { message = "Error al enviar el correo. Revisa los logs del servidor para más detalles." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en EnviarReportePorCorreo");
                return StatusCode(500, new { message = "Ocurrió un error inesperado en el servidor." });
            }
        }
    }

   
}
public class EnvioCorreoRequest
{
    public string ProyectoId { get; set; }
    public string EmailDestino { get; set; }
}