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
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using System.Data;
using Newtonsoft.Json; 
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
        private readonly IHttpClientFactory _httpClientFactory;

        // 2. Modifica tu constructor para recibirlo:
        public DashboardController(ApplicationDbContext context, IWebHostEnvironment hostingEnvironment,
                                   ILogger<DashboardController> logger, IServiceProvider serviceProvider,
                                   IConfiguration configuration, IHttpClientFactory httpClientFactory) // <--- AÑADE ESTO
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory; // <--- AÑADE ESTO
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
        [HttpPost]
        public async Task<IActionResult> AskAiAssistant([FromBody] AskAiRequest request)
        {
            if (string.IsNullOrEmpty(request.Prompt))
            {
                return BadRequest("El prompt no puede estar vacío.");
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                // --- PASO 1: GENERAR EL SQL (Llamada 1 a Ollama) ---

                // Describimos las tablas clave. Eres el "dueño" de este prompt.
                // Si añades más tablas, la IA podrá consultarlas.
                string schemaDescription = @"
        [Proyectos]:
        - Id (nvarchar, PK): ID único del proyecto (Ej: 'PROY-001').
        - NombreProyecto (nvarchar): Nombre descriptivo.
        - Presupuesto (decimal): Monto asignado.
        - Estatus (nvarchar): **MUY IMPORTANTE: El nombre es 'Estatus' (con E), NO 'Status' (con S).** Representa el estado actual (Ej: 'Activo', 'Finalizado').
        - IdCampusFk (int, FK): Referencia a la tabla Campus.
        - IdDependenciaFk (nvarchar, FK): Referencia a la tabla Dependencias.
        - IdTipoFondoFk (nvarchar, FK): Referencia a la tabla TiposFondo.
        - IdFaseFk (int, FK): Referencia a la tabla Fases.

    [Fases]:
        - Id (int, PK): ID.
        - Nombre (nvarchar): Nombre de la fase. Los valores posibles son: 'Recepción / Análisis', 'En Elaboración de Anteproyecto', 'En Elaboración de Presupuesto', 'En Licitación', 'En Ejecución', 'Finalizado', 'Cancelado'.

   [Campus]:
- Id (int, PK): ID.
- Nombre (nvarchar): Nombre del campus. Los valores posibles son: 'NORTE', 'LAGUNA', 'SURESTE'.
- **IMPORTANTE: El usuario puede escribir 'Unidad Sureste', 'unidad laguna', etc. TÚ DEBES TRADUCIRLO a los valores exactos 'NORTE', 'LAGUNA' o 'SURESTE' en mayúsculas.**
        
        [Dependencias]:
        - Id (nvarchar, PK): ID.
        - Nombre (nvarchar): Nombre de la dependencia (Ej: 'ESCUELA DE BACHILLERES...', 'FACULTAD DE...').

        [TiposFondo]:
        - Id (nvarchar, PK): ID.
        - Nombre (nvarchar): Nombre del fondo (Ej: 'ESCUELAS AL CIEN', 'FAM SUPERIOR 2025').
        ";

                var systemMessageSql = new OllamaMessage
                {
                    role = "system",
                    content = $@"Eres un asistente experto en bases de datos SQL Server. Tu única tarea es convertir la pregunta del usuario en una consulta SQL segura y eficiente.
- USA SOLAMENTE las tablas y columnas descritas en el siguiente esquema:
{schemaDescription}
- DEBES usar los nombres exactos de tablas y columnas como 'IdCampusFk', 'NombreProyecto', etc.
- La consulta DEBE ser una única instrucción SELECT.
- NO uses comillas triples (''') ni nada que no sea T-SQL válido.
- NO respondas con nada más que la consulta SQL.
- REGLA IMPORTANTE: Cuando uses subconsultas (sub-queries) para IDs (como IdFaseFk o IdCampusFk), SIEMPRE usa 'IN' en lugar de '='. (Ejemplo: 'IdCampusFk IN (SELECT Id ...)' en lugar de 'IdCampusFk = (SELECT Id ...)'.)**
- Envuelve la consulta final en etiquetas <SQL> y </SQL>."
                };

                var userMessageSql = new OllamaMessage
                {
                    role = "user",
                    content = request.Prompt
                };

                var ollamaRequestSql = new OllamaChatRequest();
                ollamaRequestSql.messages.Add(systemMessageSql);
                ollamaRequestSql.messages.Add(userMessageSql);

                var responseSql = await client.PostAsJsonAsync("http://localhost:11434/api/chat", ollamaRequestSql);

                if (!responseSql.IsSuccessStatusCode)
                {
                    return StatusCode(500, new { message = "Error en la Llamada 1 a la IA (Generación de SQL)." });
                }

                var ollamaResponseSql = await responseSql.Content.ReadFromJsonAsync<OllamaChatResponse>();
                string rawResponse = ollamaResponseSql.message.content;

                // --- PASO 2: EXTRAER Y EJECUTAR EL SQL (Lógica de C#) ---
                string sqlQuery = "";
             

                // 1. Limpiamos el Markdown (```sql ... ```) si existe
                if (rawResponse.Contains("```sql"))
                {
                    rawResponse = rawResponse.Replace("```sql", "").Replace("```", "");
                }

                // 2. Buscamos el SQL usando Regex o por "SELECT"
                Match match = Regex.Match(rawResponse, @"<SQL>(.*?)<\/SQL>", RegexOptions.Singleline);
                if (match.Success)
                {
                    sqlQuery = match.Groups[1].Value.Trim();
                }
                else if (rawResponse.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    sqlQuery = rawResponse.Trim();
                }
                else
                {
                    // No es un SQL, es charla casual.
                    _logger.LogWarning("La IA no devolvió un SQL válido. Respuesta: {Response}", rawResponse);
                    return Ok(new { responseText = rawResponse });
                }

                // 3. Limpiamos texto conversacional que la IA pega al final
                // (Buscamos el último ';' o el último ')' y cortamos ahí)
                int lastSemicolon = sqlQuery.LastIndexOf(';');
                int lastParenthesis = sqlQuery.LastIndexOf(')');

                if (lastSemicolon > -1)
                {
                    sqlQuery = sqlQuery.Substring(0, lastSemicolon + 1);
                }
                else if (lastParenthesis > -1)
                {
                    // Si no hay ';', confiamos en el último ')'
                    sqlQuery = sqlQuery.Substring(0, lastParenthesis + 1);
                }

                // 4. --- 🚨 FILTRO DE SEGURIDAD MEJORADO (PERMITE ';') 🚨 ---
                if (!sqlQuery.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                    sqlQuery.Contains("--") ||
                    sqlQuery.Contains("DROP", StringComparison.OrdinalIgnoreCase) ||
                    sqlQuery.Contains("INSERT", StringComparison.OrdinalIgnoreCase) ||
                    sqlQuery.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                    sqlQuery.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Intento de SQL inseguro bloqueado: {Query}", sqlQuery);
                    return BadRequest("La consulta generada no es segura y ha sido bloqueada.");
                }

                // 5. Usamos ADO.NET para ejecutar la consulta
                string jsonData = "[]";
                try
                {
                    var connectionString = _context.Database.GetConnectionString();
                    using (var connection = new SqlConnection(connectionString))
                    {
                        await connection.OpenAsync();
                        using (var command = new SqlCommand(sqlQuery, connection))
                        {
                            var adapter = new SqlDataAdapter(command);
                            var dataTable = new DataTable();
                            await Task.Run(() => adapter.Fill(dataTable));

                            // Esta es la versión corregida
                            jsonData = JsonConvert.SerializeObject(dataTable);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ¡Aquí es donde debería haber fallado tu consulta!
                    _logger.LogError(ex, "Error al ejecutar el SQL generado por la IA: {Query}", sqlQuery);
                    // Devolvemos el error a la IA para que intente corregirlo (o simplemente nos informe)
                    return StatusCode(500, new { message = $"La IA generó un SQL que falló: '{sqlQuery}'. Error: {ex.Message}" });
                }


                // --- PASO 3: RESUMIR LOS DATOS (Llamada 2 a Ollama) ---

                var systemMessageSummary = new OllamaMessage
                {
                    role = "system",
                    content = @"Eres un asistente experto de CGAPYS. Te proporcionaré datos en formato JSON y la pregunta original del usuario.
Tu trabajo es formular una respuesta amigable y concisa en español, basándote ÚNICAMENTE en los datos JSON.
No inventes información. Si el JSON está vacío, informa al usuario que no se encontraron resultados."
                };

                var userMessageSummary = new OllamaMessage
                {
                    role = "user",
                    content = $@"**Datos JSON:**
{jsonData}

**Pregunta Original:**
{request.Prompt}

**Tu Respuesta Amigable:**"
                };

                var ollamaRequestSummary = new OllamaChatRequest();
                ollamaRequestSummary.messages.Add(systemMessageSummary);
                ollamaRequestSummary.messages.Add(userMessageSummary);

                var responseSummary = await client.PostAsJsonAsync("http://localhost:11434/api/chat", ollamaRequestSummary);

                if (!responseSummary.IsSuccessStatusCode)
                {
                    return StatusCode(500, new { message = "Error en la Llamada 2 a la IA (Resumen de datos)." });
                }

                var ollamaResponseSummary = await responseSummary.Content.ReadFromJsonAsync<OllamaChatResponse>();

                return Ok(new { responseText = ollamaResponseSummary.message.content });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al llamar a AskAiAssistant");
                return StatusCode(500, new { message = "Error interno del servidor." });
            }
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

    // Clases para enviar la solicitud a Ollama
    public class OllamaChatRequest
    {
        public string model { get; set; } = "phi3"; // El modelo que descargaste
        public List<OllamaMessage> messages { get; set; } = new List<OllamaMessage>();
        public bool stream { get; set; } = false; // Por ahora, no usaremos streaming
    }

    public class OllamaMessage
    {
        public string role { get; set; } // "user" o "assistant"
        public string content { get; set; }
    }

    // Clases para recibir la respuesta de Ollama
    public class OllamaChatResponse
    {
        public OllamaMessage message { get; set; }
        // Aquí vendrían otras propiedades si las necesitaras (como 'done', 'total_duration', etc.)
    }

    // Clase para la solicitud desde nuestra vista
    public class AskAiRequest
    {
        public string Prompt { get; set; }
    }
}
public class EnvioCorreoRequest
{
    public string ProyectoId { get; set; }
    public string EmailDestino { get; set; }
}

