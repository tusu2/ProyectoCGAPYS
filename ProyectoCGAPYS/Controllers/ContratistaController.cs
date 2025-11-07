// Sugerencia: /Controllers/ContratistaController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ProyectoCGAPYS.Data;
using ProyectoCGAPYS.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using Microsoft.AspNetCore.Http;

// --- ¡ESTA ES LA LÍNEA CLAVE! ---
// Al poner "using ProyectoCGAPYS.ViewModels;", le dices a C# que busque CUALQUIER
// clase que necesite (como DetallesLicitacionViewModel, PropuestaViewModel, etc.)
// dentro de esa carpeta/namespace, sin importar en cuántos archivos estén divididas.
using ProyectoCGAPYS.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

[Authorize]
public class ContratistaController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ContratistaController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var contratista = await _context.Contratistas.FirstOrDefaultAsync(c => c.UsuarioId == userId);

        if (contratista == null) return Forbid();

        var viewModel = new ContratistaLobbyViewModel
        {
            NombreContratista = contratista.NombreContacto,

             Notificaciones = await _context.Notificaciones
            .Where(n => n.UsuarioId == userId && !n.Leida)
            .OrderByDescending(n => n.FechaCreacion)
            .ToListAsync()
        };
        viewModel.Invitaciones = await _context.LicitacionContratistas
          // --- LÓGICA MODIFICADA AQUÍ ---
          .Where(lc => lc.ContratistaId == contratista.Id &&
                       (lc.Licitacion.Estado == "Activo" || lc.EstadoParticipacion == "Ganador"))
          // --- FIN DE LA MODIFICACIÓN ---
          .Include(lc => lc.Licitacion)
          .ThenInclude(l => l.Proyecto)
          .Select(lc => new InvitacionViewModel
          {
              LicitacionId = lc.LicitacionId,
              NumeroLicitacion = lc.Licitacion.NumeroLicitacion,
              NombreProyecto = lc.Licitacion.Proyecto.NombreProyecto,
              DescripcionProyecto = lc.Licitacion.Proyecto.Descripcion,
              // Mostramos la fecha límite, incluso si ya pasó (para los ganadores)
              FechaFinPropuestas = lc.Licitacion.FechaFinPropuestas ?? DateTime.Now
          })
          .ToListAsync();
        viewModel.HistorialProyectos = await _context.LicitacionContratistas
            .Where(lc => lc.ContratistaId == contratista.Id && lc.Licitacion.Proyecto.Estatus == "Finalizado")
            .Include(lc => lc.Licitacion.Proyecto)
            .Select(lc => new HistorialProyectoViewModel
            {
                ProyectoId = lc.Licitacion.Proyecto.Id,
                NombreProyecto = lc.Licitacion.Proyecto.NombreProyecto,
                Folio = lc.Licitacion.Proyecto.Folio,
                FechaFinalizacion = lc.Licitacion.Proyecto.FechaFinalizacionAprox
            })
            .Distinct()
            .ToListAsync();

        if (viewModel.Notificaciones.Any())
        {
            foreach (var notificacion in viewModel.Notificaciones)
            {
                notificacion.Leida = true;
            }
            await _context.SaveChangesAsync();
        }

        return View(viewModel);
    }

    public async Task<IActionResult> DetallesLicitacion(int id)
    {
        var userId = _userManager.GetUserId(User);
        var contratista = await _context.Contratistas.AsNoTracking().FirstOrDefaultAsync(c => c.UsuarioId == userId);
        if (contratista == null) return Forbid();

        var invitacion = await _context.LicitacionContratistas
          .Include(lc => lc.Licitacion)
              .ThenInclude(l => l.Proyecto) // <- Ya incluyes el Proyecto, ¡perfecto!
                  .ThenInclude(p => p.Documentos)
          .FirstOrDefaultAsync(lc => lc.LicitacionId == id && lc.ContratistaId == contratista.Id);

        if (invitacion == null) return Forbid();

        if (invitacion.Licitacion.Estado != "Activo" && invitacion.EstadoParticipacion != "Ganador")
        {
            TempData["ErrorContratista"] = "Esta licitación ya no está activa y no resultaste ganador.";
            return RedirectToAction("Index");
        }

        // Aquí se usa "DetallesLicitacionViewModel" y C# lo encuentra sin problemas.
        var viewModel = new DetallesLicitacionViewModel
        {
            LicitacionId = invitacion.LicitacionId,
            NumeroLicitacion = invitacion.Licitacion.NumeroLicitacion,
            NombreProyecto = invitacion.Licitacion.Proyecto.NombreProyecto,
            DescripcionProyecto = invitacion.Licitacion.Proyecto.Descripcion,
            FechaFinPropuestas = invitacion.Licitacion.FechaFinPropuestas ?? DateTime.Now,
            Latitud = invitacion.Licitacion.Proyecto.Latitud,
            Longitud = invitacion.Licitacion.Proyecto.Longitud,
            EstadoParticipacion = invitacion.EstadoParticipacion,

            // --- PROPIEDAD NUEVA ASIGNADA ---
            ProyectoId = invitacion.Licitacion.ProyectoId, // <-- La necesitamos

            DocumentosProyecto = invitacion.Licitacion.Proyecto.Documentos.Select(d => new DocumentoViewModel
            {
                NombreArchivo = d.NombreArchivo,
                RutaArchivo = d.RutaArchivo
            }).ToList()
        };
        if (invitacion.EstadoParticipacion == "Ganador")
        {
            // 2. Buscamos la notificación específica de "Felicidades" para esta licitación
            var notificacionGanador = await _context.Notificaciones
                .FirstOrDefaultAsync(n => n.UsuarioId == userId &&
                                          n.Url.Contains("/Contratista/DetallesLicitacion/" + id) &&
                                          n.Mensaje.StartsWith("¡Felicidades!"));

            // 3. Si encontramos la notificación y la acción AÚN NO se ha realizado...
            if (notificacionGanador != null && !notificacionGanador.AccionRealizada)
            {
                // 4. Le damos la señal a la vista para que muestre la animación
                ViewBag.MostrarAnimacionGanador = true;

                // 5. Marcamos la bandera como "realizada" y guardamos en la BD
                notificacionGanador.AccionRealizada = true;
                await _context.SaveChangesAsync();
            }
        }

        viewModel.EstadoParticipacion = invitacion.EstadoParticipacion;
        // Aquí se usa "PropuestaViewModel".
        viewModel.PropuestasSubidas = await _context.PropuestasContratistas
            .Where(p => p.LicitacionId == id && p.ContratistaId == contratista.Id)
            .OrderByDescending(p => p.FechaSubida)
            .Select(p => new PropuestaViewModel
            {
                Id = p.Id,
                NombreArchivo = p.NombreArchivo,
                RutaArchivo = p.RutaArchivo,
                Descripcion = p.Descripcion,
                FechaSubida = p.FechaSubida
            })
            .ToListAsync();

        // Y aquí se usa "PropuestaInputModel".
        viewModel.PropuestaInput = new PropuestaInputModel { LicitacionId = id };
        bool esGanadorYEnEjecucion = (viewModel.EstadoParticipacion == "Ganador") &&
                                     (invitacion.Licitacion.Proyecto.IdFaseFk == 5);
        ViewBag.MostrarGestionEstimaciones = esGanadorYEnEjecucion;

        if (esGanadorYEnEjecucion)
        {
            // 1. Buscamos TODAS las estimaciones de ESTE proyecto
            var todasMisEstimaciones = await _context.Estimaciones
                .Include(e => e.Historial) // Para el comentario de rechazo
                .Where(e => e.IdProyectoFk == invitacion.Licitacion.ProyectoId)
                .OrderByDescending(e => e.FechaEstimacion)
                .ToListAsync();

            // 2. Las agrupamos para el Kanban
            viewModel.EstimacionesAgrupadas = todasMisEstimaciones
                .GroupBy(e => e.Estado)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 3. Preparamos el formulario (sin dropdown)
            viewModel.NuevaEstimacion = new EstimacionCrearViewModel
            {
                FechaEstimacion = DateTime.Today,
                // ¡Clave! Pre-llenamos el ID del proyecto con un campo oculto
                IdProyectoFk = invitacion.Licitacion.ProyectoId
            };
        }
        else
        {
            // Si no, inicializamos el diccionario vacío para evitar errores
            viewModel.EstimacionesAgrupadas = new Dictionary<string, List<Estimaciones>>();
        }

        return View(viewModel);

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DetallesLicitacion([Bind(Prefix = "PropuestaInput")] PropuestaInputModel input)
    {
        if (ModelState.IsValid)
        {
            var userId = _userManager.GetUserId(User);
            var contratista = await _context.Contratistas.AsNoTracking().FirstOrDefaultAsync(c => c.UsuarioId == userId);
            if (contratista == null) return Forbid();

            var invitacion = await _context.LicitacionContratistas
                .AsNoTracking()
                .AnyAsync(lc => lc.LicitacionId == input.LicitacionId && lc.ContratistaId == contratista.Id);
            if (!invitacion) return Forbid();

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/propuestas");
            Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(input.ArchivoPropuesta.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await input.ArchivoPropuesta.CopyToAsync(fileStream);
            }

            var nuevaPropuesta = new PropuestaContratista
            {
                LicitacionId = input.LicitacionId,
                ContratistaId = contratista.Id,
                NombreArchivo = Path.GetFileName(input.ArchivoPropuesta.FileName),
                RutaArchivo = "/uploads/propuestas/" + uniqueFileName,
                Descripcion = input.DescripcionPropuesta,
                FechaSubida = DateTime.Now
            };

            _context.PropuestasContratistas.Add(nuevaPropuesta);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Propuesta enviada correctamente.";
            return RedirectToAction("DetallesLicitacion", new { id = input.LicitacionId });
        }

        var viewModelParaRecargar = await ReconstruirViewModel(input.LicitacionId);
        viewModelParaRecargar.PropuestaInput = input;
        return View(viewModelParaRecargar);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarPropuesta(int propuestaId)
    {
        var userId = _userManager.GetUserId(User);
        var contratista = await _context.Contratistas.AsNoTracking().FirstOrDefaultAsync(c => c.UsuarioId == userId);
        if (contratista == null) return Forbid();

        var propuesta = await _context.PropuestasContratistas
            .FirstOrDefaultAsync(p => p.Id == propuestaId && p.ContratistaId == contratista.Id);

        if (propuesta == null) return NotFound();

        var licitacionId = propuesta.LicitacionId;

        if (!string.IsNullOrEmpty(propuesta.RutaArchivo))
        {
            string physicalPath = Path.Combine(_webHostEnvironment.WebRootPath, propuesta.RutaArchivo.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }
        }

        _context.PropuestasContratistas.Remove(propuesta);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Propuesta eliminada correctamente.";
        return RedirectToAction("DetallesLicitacion", new { id = licitacionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ModificarPropuesta(int propuestaId, string nuevaDescripcion, IFormFile nuevoArchivo)
    {
        var userId = _userManager.GetUserId(User);
        var contratista = await _context.Contratistas.AsNoTracking().FirstOrDefaultAsync(c => c.UsuarioId == userId);
        if (contratista == null) return Forbid();

        var propuesta = await _context.PropuestasContratistas
            .FirstOrDefaultAsync(p => p.Id == propuestaId && p.ContratistaId == contratista.Id);

        if (propuesta == null) return NotFound();

        var licitacionId = propuesta.LicitacionId;
        propuesta.Descripcion = nuevaDescripcion;

        if (nuevoArchivo != null && nuevoArchivo.Length > 0)
        {
            if (!string.IsNullOrEmpty(propuesta.RutaArchivo))
            {
                string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, propuesta.RutaArchivo.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/propuestas");
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(nuevoArchivo.FileName);
            string newFilePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fs = new FileStream(newFilePath, FileMode.Create))
            {
                await nuevoArchivo.CopyToAsync(fs);
            }

            propuesta.NombreArchivo = Path.GetFileName(nuevoArchivo.FileName);
            propuesta.RutaArchivo = "/uploads/propuestas/" + uniqueFileName;
        }

        _context.Update(propuesta);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Propuesta modificada correctamente.";
        return RedirectToAction("DetallesLicitacion", new { id = licitacionId });
    }

    private async Task<DetallesLicitacionViewModel> ReconstruirViewModel(int licitacionId)
    {
        var userId = _userManager.GetUserId(User);
        var contratista = await _context.Contratistas.AsNoTracking().FirstOrDefaultAsync(c => c.UsuarioId == userId);
        var invitacion = await _context.LicitacionContratistas
            .AsNoTracking()
            .Include(lc => lc.Licitacion)
                .ThenInclude(l => l.Proyecto)
                    .ThenInclude(p => p.Documentos)
            .FirstOrDefaultAsync(lc => lc.LicitacionId == licitacionId && lc.ContratistaId == contratista.Id);

        var viewModel = new DetallesLicitacionViewModel
        {
            LicitacionId = invitacion.LicitacionId,
            NumeroLicitacion = invitacion.Licitacion.NumeroLicitacion,
            NombreProyecto = invitacion.Licitacion.Proyecto.NombreProyecto,
            DescripcionProyecto = invitacion.Licitacion.Proyecto.Descripcion,
            FechaFinPropuestas = invitacion.Licitacion.FechaFinPropuestas ?? DateTime.Now,
            Latitud = invitacion.Licitacion.Proyecto.Latitud,
            Longitud = invitacion.Licitacion.Proyecto.Longitud,
            DocumentosProyecto = invitacion.Licitacion.Proyecto.Documentos.Select(d => new DocumentoViewModel
            {
                NombreArchivo = d.NombreArchivo,
                RutaArchivo = d.RutaArchivo
            }).ToList()
        };

        viewModel.PropuestasSubidas = await _context.PropuestasContratistas
             .Where(p => p.LicitacionId == licitacionId && p.ContratistaId == contratista.Id)
             .OrderByDescending(p => p.FechaSubida)
             .Select(p => new PropuestaViewModel
             {
                 Id = p.Id,
                 NombreArchivo = p.NombreArchivo,
                 RutaArchivo = p.RutaArchivo,
                 Descripcion = p.Descripcion,
                 FechaSubida = p.FechaSubida
             })
             .ToListAsync();

        return viewModel;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearEstimacion([Bind(Prefix = "NuevaEstimacion")] EstimacionCrearViewModel viewModel, int LicitacionId)
    {
        
        ModelState.Remove("NuevaEstimacion.ProyectosAsignados");
        ModelState.Remove("ProyectosAsignados");
        // --- FIN DE LA CORRECCIÓN ---
        if (string.IsNullOrEmpty(viewModel.IdProyectoFk))
        {
            ModelState.AddModelError("NuevaEstimacion.IdProyectoFk", "Debe seleccionar un proyecto.");
        }
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors)
                                          .Select(e => e.ErrorMessage)
                                          .ToList();
            string errorDetallado = "Faltaron datos... " + string.Join("; ", errors);
            TempData["Error"] = errorDetallado;

            // 2. ¡Redirigimos a DetallesLicitacion!
            return RedirectToAction("DetallesLicitacion", new { id = LicitacionId });
        }

        var usuarioActual = await _userManager.GetUserAsync(User);

        // 1. Crear la entidad principal (la Estimación)
        var estimacion = new Estimaciones
        {
            IdProyectoFk = viewModel.IdProyectoFk,
            Monto = viewModel.Monto,
            FechaEstimacion = viewModel.FechaEstimacion,
            Descripcion = viewModel.Descripcion,
            Estado = "En Revisión Supervisor"
        };

        _context.Estimaciones.Add(estimacion);
        await _context.SaveChangesAsync();

        try
        {
            // 2. Guardar archivos
            await GuardarArchivoEstimacion(
                estimacion.Id,
                viewModel.ArchivoNumerosGeneradores,
                "NumerosGeneradores",
                usuarioActual.Id);

            await GuardarArchivoEstimacion(
                estimacion.Id,
                viewModel.ArchivoReporteFotografico,
                "ReporteFotografico",
                usuarioActual.Id);

            await GuardarArchivoEstimacion(
                estimacion.Id,
                viewModel.ArchivoBitacora,
                "Bitacora",
                usuarioActual.Id);

            // 3. Crear el primer registro en el Historial
            var historial = new EstimacionHistorial
            {
                EstimacionId = estimacion.Id,
                EstadoAnterior = "En Creación",
                EstadoNuevo = "En Revisión Supervisor",
                UsuarioId = usuarioActual.Id,
                Comentario = "Envío inicial del contratista."
            };
            _context.EstimacionHistorial.Add(historial);

            // 4. Guardar los documentos y el historial
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "¡Estimación enviada a revisión correctamente!";
        }
        catch (Exception ex)
        {
            _context.Estimaciones.Remove(estimacion);
            await _context.SaveChangesAsync();
            TempData["Error"] = "Error al guardar los archivos: " + ex.Message;
        }

        return RedirectToAction("DetallesLicitacion", new { id = LicitacionId });
    }
    // --- FUNCIÓN HELPER PRIVADA ---
    // (Pon esto al final de tu ContratistaController.cs)
    private async Task GuardarArchivoEstimacion(int estimacionId, IFormFile archivo, string tipoDocumento, string usuarioId)
    {
        if (archivo == null || archivo.Length == 0)
            throw new Exception($"El archivo para '{tipoDocumento}' es nulo.");

        // 1. Definir la ruta
        // ¡USAMOS _webHostEnvironment.WebRootPath como tú lo haces!
        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "estimaciones");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(archivo.FileName);
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // 2. Guardar el archivo físico
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await archivo.CopyToAsync(fileStream);
        }

        // 3. Crear el registro en la BD
        var documento = new EstimacionDocumentos
        {
            EstimacionId = estimacionId,
            TipoDocumento = tipoDocumento,
            NombreArchivo = Path.GetFileName(archivo.FileName),
            RutaArchivo = "/uploads/estimaciones/" + uniqueFileName, // Ruta web
            UsuarioId = usuarioId,
            FechaSubida = DateTime.Now
        };

        _context.EstimacionDocumentos.Add(documento);
        // (El SaveChangesAsync() se llama desde la acción principal 'CrearEstimacion')
    }
    public async Task<IActionResult> MisEstimaciones()
    {
        var userId = _userManager.GetUserId(User);
        var contratista = await _context.Contratistas
                                        .AsNoTracking()
                                        .FirstOrDefaultAsync(c => c.UsuarioId == userId);
        if (contratista == null) return Forbid();

        // 1. Buscar todos los proyectos "En Ejecución" (Fase 5) de este contratista
        var idsProyectos = await _context.Licitaciones
            .Where(l => l.ContratistaGanadorId == contratista.Id && l.Proyecto.IdFaseFk == 5)
            .Select(l => l.ProyectoId)
            .Distinct()
            .ToListAsync();

        var proyectosActivos = await _context.Proyectos
            .Where(p => idsProyectos.Contains(p.Id))
            .ToListAsync();

        // 2. Buscar TODAS las estimaciones de esos proyectos
        var todasMisEstimaciones = await _context.Estimaciones
            .Include(e => e.Proyecto) // Para mostrar el nombre del proyecto en la tarjeta
            .Where(e => idsProyectos.Contains(e.IdProyectoFk))
            .OrderByDescending(e => e.FechaEstimacion)
            .ToListAsync();

        // 3. Agruparlas para el "Kanban" (igual que el supervisor)
        var agrupadas = todasMisEstimaciones
            .GroupBy(e => e.Estado)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 4. Preparar el ViewModel
        var viewModel = new ContratistaEstimacionesViewModel
        {
            EstimacionesAgrupadas = agrupadas,

            // Preparamos el formulario de "Crear"
            NuevaEstimacion = new EstimacionCrearViewModel { FechaEstimacion = DateTime.Today },

            // Preparamos el dropdown para el formulario
            ProyectosEnEjecucion = new SelectList(proyectosActivos, "Id", "NombreProyecto")
        };

        return View(viewModel); // Enviaremos esto a la nueva vista "MisEstimaciones.cshtml"
    }
}