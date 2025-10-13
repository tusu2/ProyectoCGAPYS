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
              .ThenInclude(l => l.Proyecto)
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
}