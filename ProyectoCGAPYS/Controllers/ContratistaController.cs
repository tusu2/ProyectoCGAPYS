// Sugerencia: /Controllers/ContratistaController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.ViewModels;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ProyectoCGAPYS.Data; // Reemplaza con el namespace de tu DbContext
using ProyectoCGAPYS.Models; // Reemplaza con el namespace de tus modelos
using ProyectoCGAPYS.ViewModels; // El namespace de los ViewModels que creamos
using static ProyectoCGAPYS.ViewModels.InvitacionViewModel;
using Microsoft.AspNetCore.Hosting; // <-- Añade este using
using System.IO;

[Authorize] // Solo usuarios autenticados pueden acceder
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

    public async Task<IActionResult> DetallesLicitacion(int id)
    {
        var userId = _userManager.GetUserId(User);
        var contratista = await _context.Contratistas.FirstOrDefaultAsync(c => c.UsuarioId == userId);

        if (contratista == null) return Forbid();

        // Verificación de seguridad: ¿Este contratista está realmente invitado a esta licitación?
        var invitacion = await _context.LicitacionContratistas
            .Include(lc => lc.Licitacion)
                .ThenInclude(l => l.Proyecto)
            .FirstOrDefaultAsync(lc => lc.LicitacionId == id && lc.ContratistaId == contratista.Id);

        if (invitacion == null)
        {
            // Si no está invitado, no puede ver los detalles.
            return Forbid();
        }

        var viewModel = new DetallesLicitacionViewModel
        {
            LicitacionId = invitacion.LicitacionId,
            NumeroLicitacion = invitacion.Licitacion.NumeroLicitacion,
            NombreProyecto = invitacion.Licitacion.Proyecto.NombreProyecto,
            DescripcionProyecto = invitacion.Licitacion.Proyecto.Descripcion,
            FechaFinPropuestas = invitacion.Licitacion.FechaFinPropuestas ?? DateTime.Now,
        };

        // Cargar las propuestas que ya ha subido para esta licitación
        viewModel.PropuestasSubidas = await _context.PropuestasContratistas
            .Where(p => p.LicitacionId == id && p.ContratistaId == contratista.Id)
            .Select(p => new PropuestaViewModel
            {
                NombreArchivo = p.NombreArchivo,
                Descripcion = p.Descripcion,
                FechaSubida = p.FechaSubida,
                RutaArchivo = p.RutaArchivo
            })
            .ToListAsync();

        return View(viewModel);
    }
    // GET: /Contratista/Index o /Contratista
    public async Task<IActionResult> Index()
    {
        // 1. Obtener el ID del usuario actual
        var userId = _userManager.GetUserId(User);

        // 2. Encontrar el perfil del contratista usando el ID de usuario
        var contratista = await _context.Contratistas.FirstOrDefaultAsync(c => c.UsuarioId == userId);

        if (contratista == null)
        {
            // Si el usuario no es un contratista registrado en la tabla, no tiene acceso.
            return Forbid();
        }

        var viewModel = new ContratistaLobbyViewModel
        {
            NombreContratista = contratista.NombreContacto
        };

        // 3. Buscar las invitaciones activas para este contratista
        viewModel.Invitaciones = await _context.LicitacionContratistas
            .Where(lc => lc.ContratistaId == contratista.Id && lc.EstadoParticipacion == "Invitado")
            .Include(lc => lc.Licitacion)
            .ThenInclude(l => l.Proyecto)
            .Select(lc => new InvitacionViewModel
            {
                LicitacionId = lc.LicitacionId,
                NumeroLicitacion = lc.Licitacion.NumeroLicitacion,
                NombreProyecto = lc.Licitacion.Proyecto.NombreProyecto,
                DescripcionProyecto = lc.Licitacion.Proyecto.Descripcion,
                FechaFinPropuestas = lc.Licitacion.FechaFinPropuestas ?? DateTime.Now // Manejo de nulos
            })
            .ToListAsync();

        // 4. (Lógica para el historial) Buscar proyectos finalizados donde participó el contratista
        // Esta es una implementación de ejemplo, puedes ajustarla a tu lógica de negocio
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
            .Distinct() // Evitar duplicados si un contratista participa en varias licitaciones de un mismo proyecto
            .ToListAsync();


        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DetallesLicitacion(DetallesLicitacionViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        var contratista = await _context.Contratistas.AsNoTracking().FirstOrDefaultAsync(c => c.UsuarioId == userId);

        if (contratista == null) return Forbid();

        // Validación de seguridad de nuevo, ¡siempre importante!
        var invitacion = await _context.LicitacionContratistas
             .AsNoTracking()
            .AnyAsync(lc => lc.LicitacionId == model.LicitacionId && lc.ContratistaId == contratista.Id);

        if (!invitacion) return Forbid();

        if (ModelState.IsValid)
        {
            string uniqueFileName = null;
            if (model.ArchivoPropuesta != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads/propuestas");
                // Asegurarnos de que la carpeta exista
                Directory.CreateDirectory(uploadsFolder);

                uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ArchivoPropuesta.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ArchivoPropuesta.CopyToAsync(fileStream);
                }
            }

            // Guardar la información en la base de datos
            var nuevaPropuesta = new PropuestaContratista
            {
                LicitacionId = model.LicitacionId,
                ContratistaId = contratista.Id,
                NombreArchivo = Path.GetFileName(model.ArchivoPropuesta.FileName),
                RutaArchivo = "/uploads/propuestas/" + uniqueFileName, // Guardamos la ruta web
                Descripcion = model.DescripcionPropuesta,
                FechaSubida = DateTime.Now
            };

            _context.PropuestasContratistas.Add(nuevaPropuesta);
            await _context.SaveChangesAsync();

            // Redirigir a la misma página para que vea el archivo recién subido en la lista
            return RedirectToAction("DetallesLicitacion", new { id = model.LicitacionId });
        }

        // Si el modelo no es válido, volvemos a cargar la página con los datos necesarios
        // (esto es importante para que la lista de archivos ya subidos no desaparezca)
        model.PropuestasSubidas = await _context.PropuestasContratistas
            .Where(p => p.LicitacionId == model.LicitacionId && p.ContratistaId == contratista.Id)
            .Select(p => new PropuestaViewModel { /*...llenar campos...*/ })
            .ToListAsync();

        var licitacion = await _context.Licitaciones.Include(l => l.Proyecto).FirstAsync(l => l.Id == model.LicitacionId);
        model.NombreProyecto = licitacion.Proyecto.NombreProyecto;
        model.DescripcionProyecto = licitacion.Proyecto.Descripcion;

        return View(model);
    }
}