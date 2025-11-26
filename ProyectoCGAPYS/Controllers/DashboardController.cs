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
using System.Text.Json.Serialization;
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
            //     dashboardViewModel.EstimacionesPendientes = await _context.Set<ProyectoAlertaViewModel>()
            //                                                     .FromSqlRaw("EXEC sp_GetDashboard_EstimacionesPendientes")
            //                                                   .ToListAsync();
          
            dashboardViewModel.EstimacionesPendientes = new List<ProyectoAlertaViewModel>();
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
                var apiKey = _configuration["Gemini:ApiKey"];
                if (string.IsNullOrEmpty(apiKey) || apiKey == "TU_API_KEY_DE_GEMINI_VA_AQUI")
                {
                    return StatusCode(500, new { message = "Error: La API Key de Gemini no está configurada en appsettings.json." });
                }
                var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";


                // --- PASO 1: GENERAR EL SQL (Llamada 1 a Gemini) ---

                // El esquema de la base de datos (que ya tenías) es perfecto. No lo cambiamos.
                string schemaDescription = @"[Proyectos]:
        - Id (nvarchar, PK): ID único del proyecto (Ej: 'PROY-001').
        - NombreProyecto (nvarchar): Nombre descriptivo.
        - Presupuesto (decimal): Monto asignado.
        - Estatus (nvarchar): **MUY IMPORTANTE: El nombre es 'Estatus' (con E), NO 'Status' (con S).** Representa el estado actual (Ej: 'Activo', 'Finalizado').
        - IdCampusFk (int, FK): Referencia a la tabla Campus.
        - IdDependenciaFk (nvarchar, FK): Referencia a la tabla Dependencias.
        - IdTipoFondoFk (nvarchar, FK): Referencia a la tabla TiposFondo.
        - IdFaseFk (int, FK): Referencia a la tabla Fases.
        - Prioridad (nvarchar): Nivel de prioridad (ej: 'verde', 'rojo').
        - FechaSolicitud (datetime2): Cuándo se pidió.
        - FechaFinalizacionAprox (datetime2): Cuándo debe terminar.
        - NombreResponsable (nvarchar):

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
                string promptSql = $@"Eres un asistente experto en bases de datos SQL Server. Tu única tarea es convertir la pregunta del usuario en una consulta SQL segura y eficiente.
- USA SOLAMENTE las tablas y columnas descritas en el siguiente esquema:
{schemaDescription}
- DEBES usar los nombres exactos de tablas y columnas como 'IdCampusFk', 'NombreProyecto', etc.
- La consulta DEBE ser una única instrucción SELECT.
- NO respondas con nada más que la consulta SQL.
- REGLA IMPORTANTE: Cuando uses subconsultas (sub-queries) para IDs (como IdFaseFk o IdCampusFk), SIEMPRE usa 'IN' en lugar de '='. (Ejemplo: 'IdCampusFk IN (SELECT Id ...)' en lugar de 'IdCampusFk = (SELECT Id ...)'.)**
- Envuelve la consulta final en etiquetas <SQL> y </SQL>.

**Pregunta del Usuario:**
{request.Prompt}";
                // CAMBIO: Gemini no usa "roles" (system/user). Combinamos las instrucciones 
                // del sistema y la pregunta del usuario en un solo "prompt".


                // CAMBIO: Usamos las nuevas clases de DTO de Gemini
                var geminiRequestSql = new GeminiChatRequest();
                geminiRequestSql.Contents.Add(new GeminiContent
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = promptSql } }
                });

                // CAMBIO: Llamamos a la URL de Gemini
                var responseSql = await client.PostAsJsonAsync(geminiUrl, geminiRequestSql);

                if (!responseSql.IsSuccessStatusCode)
                {
                    _logger.LogError("Error en la Llamada 1 a Gemini (SQL): {StatusCode} - {Reason}", responseSql.StatusCode, await responseSql.Content.ReadAsStringAsync());
                    return StatusCode(500, new { message = "Error en la Llamada 1 a la IA (Generación de SQL)." });
                }

                // CAMBIO: Leemos la respuesta usando las DTO de Gemini
                var geminiResponseSql = await responseSql.Content.ReadFromJsonAsync<GeminiChatResponse>();

                // --- 👇 CORRECCIÓN 1: VALIDAR RESPUESTA DE SQL ---
                if (geminiResponseSql == null ||
                    geminiResponseSql.Candidates == null ||
                    geminiResponseSql.Candidates.Count == 0 ||
                    geminiResponseSql.Candidates[0].Content == null ||
                    geminiResponseSql.Candidates[0].Content.Parts == null ||
                    geminiResponseSql.Candidates[0].Content.Parts.Count == 0)
                {
                    _logger.LogWarning("La respuesta de Gemini (SQL) no tuvo contenido o fue bloqueada.");
                    // Devolvemos una respuesta de texto amigable en lugar de crashear
                    return Ok(new { responseType = "text", content = "Lo siento, la IA no pudo procesar esa solicitud (respuesta bloqueada o vacía)." });
                }
                string rawResponse = geminiResponseSql.Candidates[0].Content.Parts[0].Text;


                // --- PASO 2: EXTRAER Y EJECUTAR EL SQL (Lógica de C#) ---
                // Esta parte (limpieza de SQL, seguridad y ejecución con ADO.NET) 
                // es tuya y es excelente. La conservamos tal cual.
                string sqlQuery = "";

                if (rawResponse.Contains("```sql"))
                {
                    rawResponse = rawResponse.Replace("```sql", "").Replace("```", "");
                }

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
                    _logger.LogWarning("La IA (Gemini) no devolvió un SQL válido. Respuesta: {Response}", rawResponse);
                    // Si no es SQL, podría ser charla casual, la devolvemos.
                    return Ok(new { responseText = rawResponse });
                }

                // Limpieza de texto conversacional
                int lastSemicolon = sqlQuery.LastIndexOf(';');
                int lastParenthesis = sqlQuery.LastIndexOf(')');
                if (lastSemicolon > -1)
                {
                    sqlQuery = sqlQuery.Substring(0, lastSemicolon + 1);
                }
                else if (lastParenthesis > -1)
                {
                    sqlQuery = sqlQuery.Substring(0, lastParenthesis + 1);
                }

                // Filtro de seguridad (¡Muy importante! Lo conservamos)
                if (!sqlQuery.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) ||
                    sqlQuery.Contains("--") ||
                    sqlQuery.Contains("DROP", StringComparison.OrdinalIgnoreCase) ||
                    sqlQuery.Contains("INSERT", StringComparison.OrdinalIgnoreCase) ||
                    sqlQuery.Contains("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                    sqlQuery.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Intento de SQL inseguro bloqueado (Gemini): {Query}", sqlQuery);
                    return BadRequest(new { responseType = "text", content = "La consulta generada no es segura y ha sido bloqueada." });
                }

                // Ejecución de ADO.NET (La conservamos tal cual)
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
                            jsonData = JsonConvert.SerializeObject(dataTable);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al ejecutar el SQL generado por Gemini: {Query}", sqlQuery);
                    return StatusCode(500, new { responseType = "text", content = $"La IA generó un SQL que falló: '{sqlQuery}'. Error: {ex.Message}" });
                }

                string datosExcel = ObtenerDatosDelDirectorio();
                // --- PASO 3: RESUMIR LOS DATOS (Llamada 2 a Gemini) ---

                // CAMBIO: Combinamos las instrucciones y los datos en un solo prompt para Gemini.
                string promptSummary = $@"
Eres un asistente experto de CGAPYS. Tienes acceso a dos fuentes de información exclusivas. Tu primera tarea es identificar sobre qué te están preguntando.

--------------------------------------------------------
FUENTE DE DATOS 1: DIRECTORIO UNIVERSITARIO (EXCEL)
--------------------------------------------------------
Usa estos datos SOLO si la pregunta se refiere a Facultades, Escuelas, Matrículas, Directores, Teléfonos o Ubicaciones.

### DATOS DEL DIRECTORIO ###
{datosExcel}
############################

**REGLAS ESTRICTAS PARA EL DIRECTORIO:**
1. Busca la respuesta EXACTA en los DATOS DEL DIRECTORIO.
2. NO inventes nada. Tu nivel de creatividad para estos datos es 0.
3. Si te preguntan por una matrícula, director o teléfono y NO está en el texto de arriba, responde EXACTAMENTE: 'No se encontraron resultados'.
4. No uses formato HTML para estas respuestas, solo texto plano directo y veraz.

--------------------------------------------------------
FUENTE DE DATOS 2: PROYECTOS DEL SISTEMA (JSON)
--------------------------------------------------------
Usa estos datos SOLO si la pregunta se refiere a Proyectos, Estatus, Presupuestos o Conteos del sistema CGAPYS.

**Datos JSON:**
{jsonData}

**Reglas para Proyectos (Tu Prompt Original):**
**Regla 1 (Listas/Conteos):**
Si el JSON tiene 0 o más de 1 objeto, O si la pregunta original pide un conteo (ej: 'cuántos'), responde en texto plano simple.
EJEMPLO: ""Hay 5 proyectos activos.""
EJEMPLO: ""No se encontraron proyectos.""

**Regla 2 (Objeto Único/Modal):**
Si el JSON tiene **exactamente 1 objeto** Y la pregunta NO es un conteo:
* Tu respuesta DEBE ser solo HTML, envuelto en `<MODAL_HTML>` y `</MODAL_HTML>`.
* NO AÑADAS NINGÚN TEXTO fuera del `<p>` y el `<ul>`.

* **Formato HTML EXACTO:**
    <p class='text-center mb-3 fst-italic'>¡Claro! Aquí tienes el resultado, jefa del proyecto:</p>
    <ul class='list-group list-group-flush'>
        <li class='list-group-item d-flex justify-content-between align-items-center'>ID del Proyecto: <span class='fw-bold'>[Valor ID]</span></li>
        <li class='list-group-item d-flex justify-content-between align-items-center'>Folio: <span class='fw-bold'>[Valor Folio]</span></li>
        <li class='list-group-item d-flex justify-content-between align-items-center'>ID Campus: <span class='fw-bold'>[Valor Campus]</span></li>
        <li class='list-group-item d-flex justify-content-between align-items-center'>ID Dependencia: <span class='fw-bold'>[Valor Dependencia]</span></li>
        <li class='list-group-item d-flex justify-content-between align-items-center'>ID Fondo: <span class='fw-bold'>[Valor Fondo]</span></li>
        <li class='list-group-item d-flex justify-content-between align-items-center'>Presupuesto: <span class='badge bg-success fs-6'>[Valor Presupuesto con formato $]</span></li>
        <li class='list-group-item d-flex justify-content-between align-items-center'>Estatus: <span class='fw-bold'>[Valor Estatus]</span></li>
        <li class='list-group-item d-flex justify-content-between align-items-center'>Prioridad: <span class='fw-bold'>[Valor Prioridad]</span></li>
        <li class='list-group-item d-flex justify-content-between align-items-center'>Fecha de Solicitud: <span class='fw-bold'>[Valor Fecha dd/MM/yyyy]</span></li>
        <li class='list-group-item d-flex justify-content-between align-items-center'>Fecha de Cierre Aprox.: <span class='fw-bold'>[Valor Fecha dd/MM/yyyy]</span></li>
        <li class='list-group-item d-flex justify-content-between align-items-center'>Responsable: <span class='fw-bold'>[Valor Responsable]</span></li>
    </ul>

--------------------------------------------------------
INSTRUCCIÓN FINAL:
Analiza la siguiente pregunta del usuario y decide qué fuente utilizar.
- Si es sobre el Directorio, aplica las REGLAS ESTRICTAS DEL DIRECTORIO.
- Si es sobre Proyectos, aplica las REGLAS DE PROYECTOS (HTML o Texto).

Pregunta Original:
{request.Prompt}
";

                //  Tu Respuesta (solo texto o HTML dentro de <MODAL_HTML>):";

                var geminiRequestSummary = new GeminiChatRequest();
                geminiRequestSummary.Contents.Add(new GeminiContent
                {
                    Parts = new List<GeminiPart> { new GeminiPart { Text = promptSummary } }
                });

                var responseSummary = await client.PostAsJsonAsync(geminiUrl, geminiRequestSummary);

                if (!responseSummary.IsSuccessStatusCode)
                {
                    _logger.LogError("Error en la Llamada 2 a Gemini (Resumen): {StatusCode} - {Reason}", responseSummary.StatusCode, await responseSummary.Content.ReadAsStringAsync());
                    return StatusCode(500, new { message = "Error en la Llamada 2 a la IA (Resumen de datos)." });
                }

                var geminiResponseSummary = await responseSummary.Content.ReadFromJsonAsync<GeminiChatResponse>();

                if (geminiResponseSummary == null ||
                    geminiResponseSummary.Candidates == null ||
                    geminiResponseSummary.Candidates.Count == 0 ||
                    geminiResponseSummary.Candidates[0].Content == null ||
                    geminiResponseSummary.Candidates[0].Content.Parts == null ||
                    geminiResponseSummary.Candidates[0].Content.Parts.Count == 0)
                {
                    _logger.LogWarning("La respuesta de Gemini (Resumen) no tuvo contenido o fue bloqueada.");
                    return Ok(new { responseType = "text", content = "Lo siento, la IA no pudo procesar esa respuesta (respuesta bloqueada o vacía)." });
                }

                var finalResponseText = geminiResponseSummary.Candidates[0].Content.Parts[0].Text;

                Match modalMatch = Regex.Match(finalResponseText, @"<MODAL_HTML>(.*?)<\/MODAL_HTML>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

                if (modalMatch.Success)
                {
                    string modalHtml = modalMatch.Groups[1].Value.Trim();
                    string modalTitle = "Detalle del Proyecto";

                    try
                    {
                        using (var jsonDoc = JsonDocument.Parse(jsonData))
                        {
                            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array && jsonDoc.RootElement.GetArrayLength() > 0)
                            {
                                var firstProject = jsonDoc.RootElement[0];
                                if (firstProject.TryGetProperty("NombreProyecto", out var nombreProp))
                                {
                                    modalTitle = nombreProp.GetString();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo parsear el JSON para el título del modal, se usará el título por defecto.");
                    }

                    return Ok(new { responseType = "modal", content = modalHtml, title = modalTitle });
                }
                else
                {
                    return Ok(new { responseType = "text", content = finalResponseText });
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción al llamar a AskAiAssistant (Gemini)");
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
        // Método auxiliar para leer el archivo y convertirlo a texto para la IA
        private string ObtenerDatosDelDirectorio()
        {
            // Ajusta la ruta según donde tengas guardado el archivo
            // Si es un CSV (recomendado para rapidez):
            string rutaArchivo = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "DIRECTORIO 2025.csv");

            if (!System.IO.File.Exists(rutaArchivo))
            {
                return "Error: El archivo de directorio no fue encontrado.";
            }

            try
            {
                // Leemos todo el texto
                string contenido = System.IO.File.ReadAllText(rutaArchivo);
                return contenido;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error leyendo el archivo: {ex.Message}");
                return "Error al leer los datos.";
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDashboardFiltered(string fondos)
        {
            // Si fondos viene vacío o dice "todos", enviamos NULL al SP
            string paramFondo = (string.IsNullOrEmpty(fondos) || fondos == "todos") ? null : fondos;

            var connectionString = _context.Database.GetConnectionString();
            var resultado = new DashboardFilteredData();

            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("sp_GetDashboard_FilteredData", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ListaFondos", (object)paramFondo ?? DBNull.Value);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // 1. Leer KPIs
                        if (await reader.ReadAsync())
                        {
                            resultado.Kpis = new KPIsViewModel
                            {
                                PresupuestoTotalAutorizado = reader.GetDecimal(reader.GetOrdinal("PresupuestoTotalAutorizado")),
                                PresupuestoContratado = reader.GetDecimal(reader.GetOrdinal("PresupuestoContratado")),
                                MontoTotalEjercido = reader.GetDecimal(reader.GetOrdinal("MontoTotalEjercido")),
                                BalanceGeneralDisponible = reader.GetDecimal(reader.GetOrdinal("BalanceGeneralDisponible")),
                                ProyectosTotales = reader.GetInt32(reader.GetOrdinal("ProyectosTotales")),
                                ProyectosActivos = reader.GetInt32(reader.GetOrdinal("ProyectosActivos"))
                            };
                        }

                        // 2. Leer Gráfica de Fases
                        await reader.NextResultAsync();
                        resultado.ProyectosPorFase = new List<FaseViewModel>();
                        while (await reader.ReadAsync())
                        {
                            resultado.ProyectosPorFase.Add(new FaseViewModel
                            {
                                Fase = reader.GetString(reader.GetOrdinal("Fase")),
                                TotalProyectos = reader.GetInt32(reader.GetOrdinal("TotalProyectos"))
                            });
                        }

                        // 3. Leer Lista de Proyectos por Vencer
                        await reader.NextResultAsync();
                        resultado.ProyectosPorVencer = new List<dynamic>();
                        while (await reader.ReadAsync())
                        {
                            resultado.ProyectosPorVencer.Add(new
                            {
                                Id = reader.GetString(reader.GetOrdinal("Id")),
                                NombreProyecto = reader.GetString(reader.GetOrdinal("NombreProyecto")),

                                // --- CORRECCIÓN AQUÍ ---
                                // Antes: reader.GetDateTime(...).ToString(...)
                                // Ahora: reader.GetString(...) porque SQL ya lo manda formateado
                                FechaVencimiento = reader.GetString(reader.GetOrdinal("FechaVencimiento")),

                                DiasRestantes = reader.GetInt32(reader.GetOrdinal("DiasRestantes")),
                                ClaseSemaforo = reader.GetString(reader.GetOrdinal("ClaseSemaforo"))
                            });
                        }
                    }
                }
            }

            return Json(resultado);
        }

        // Clase auxiliar DTO para enviar la respuesta JSON

    }
    public class DashboardFilteredData
    {
        public KPIsViewModel Kpis { get; set; }
        public List<FaseViewModel> ProyectosPorFase { get; set; }
        public List<dynamic> ProyectosPorVencer { get; set; }
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

public class GeminiChatRequest
{
    // Usamos JsonPropertyName para asegurarnos de que el nombre sea correcto ("contents")
    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = new List<GeminiContent>();
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new List<GeminiPart>();
}

public class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}

// --- Clases para RECIBIR de la API de Gemini ---
public class GeminiChatResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate> Candidates { get; set; }
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent Content { get; set; }
}

