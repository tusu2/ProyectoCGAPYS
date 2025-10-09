using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Data; // Reemplaza por tu namespace de DbContext
using ProyectoCGAPYS.Models; // Reemplaza por tu namespace de Models
using ProyectoCGAPYS.ViewModels; // Reemplaza por tu namespace de ViewModels
using System.Linq;
using System.Threading.Tasks;
using ProyectoCGAPYS.Datos;
using static ProyectoCGAPYS.ViewModels.LicitacionDetalleViewModel;

// Solo los usuarios autorizados (administradores/empleados) podrán acceder a este controlador.
[Authorize(Roles = "Jefa,Empleado1,Empleado2,Empleado3")]
public class LicitacionesController : Controller
{
    private readonly ApplicationDbContext _context;

    // Inyectamos el DbContext para poder interactuar con la base de datos.
    public LicitacionesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Licitaciones
    // Muestra una lista de todas las licitaciones creadas.
    public async Task<IActionResult> Index()
    {
        // Obtenemos las licitaciones y también cargamos la información del proyecto asociado.
        var licitaciones = await _context.Licitaciones
                                         .Include(l => l.Proyecto)
                                         .OrderByDescending(l => l.FechaInicio)
                                         .ToListAsync();
        return View(licitaciones);
    }

    // GET: /Licitaciones/Crear?proyectoId=PROY-001
    // Muestra el formulario para crear una nueva licitación asociada a un proyecto.
    public async Task<IActionResult> Crear(string proyectoId)
    {
        if (string.IsNullOrEmpty(proyectoId))
        {
            return BadRequest("El ID del proyecto es necesario.");
        }

        var proyecto = await _context.Proyectos.FindAsync(proyectoId);
        if (proyecto == null)
        {
            return NotFound("El proyecto especificado no existe.");
        }

        // Creamos el ViewModel y le pasamos la información del proyecto.
        var viewModel = new CrearLicitacionViewModel
        {
            ProyectoId = proyecto.Id,
            ProyectoNombre = proyecto.NombreProyecto,
            FechaInicio = DateTime.Today // Pre-llenamos la fecha de inicio.
        };

        return View(viewModel);
    }

    // POST: /Licitaciones/Crear
    // Procesa los datos enviados desde el formulario de creación.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearLicitacionViewModel viewModel)
    {
        // Verificamos si el modelo es válido según las reglas que definimos (ej. [Required]).
        if (ModelState.IsValid)
        {
            // Creamos una nueva entidad Licitacion con los datos del ViewModel.
            var nuevaLicitacion = new Licitacion
            {
                ProyectoId = viewModel.ProyectoId,
                NumeroLicitacion = viewModel.NumeroLicitacion,
                Descripcion = viewModel.Descripcion,
                FechaInicio = viewModel.FechaInicio,
                FechaFinPropuestas = viewModel.FechaFinPropuestas,
                Estado = "Abierta" // Estado inicial por defecto.
            };

            _context.Licitaciones.Add(nuevaLicitacion);
            await _context.SaveChangesAsync(); // Guardamos los cambios en la BD.

            // Redirigimos al usuario a la página de detalles de la licitación recién creada.
            // (Crearemos esta vista en el siguiente paso).
            return RedirectToAction("Detalles", new { id = nuevaLicitacion.Id });
        }

        // Si el modelo no es válido, volvemos a mostrar el formulario con los errores.
        // Recargamos el nombre del proyecto, ya que no se envía de vuelta con el POST.
        var proyecto = await _context.Proyectos.FindAsync(viewModel.ProyectoId);
        viewModel.ProyectoNombre = proyecto?.NombreProyecto;

        return View(viewModel);
    }
    public async Task<IActionResult> DetallesPorProyecto(string proyectoId)
    {
        if (string.IsNullOrEmpty(proyectoId))
        {
            return BadRequest();
        }

        var licitacion = await _context.Licitaciones
                                       .FirstOrDefaultAsync(l => l.ProyectoId == proyectoId);

        if (licitacion == null)
        {
            // Opcional: Podrías redirigir a la página de Crear Licitación si no existe una.
            return NotFound("No se encontró una licitación para el proyecto especificado.");
        }

        return RedirectToAction("Detalles", new { id = licitacion.Id });
    }

    // GET: Licitaciones/Detalles/5
    // LicitacionesController.cs -> Acción "Detalles"

    // GET: Licitaciones/Detalles/5
    public async Task<IActionResult> Detalles(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var licitacion = await _context.Licitaciones
             .Include(l => l.Proyecto)
                 .ThenInclude(p => p.Campus) // Incluimos Campus
             .Include(l => l.Proyecto)
                 .ThenInclude(p => p.Dependencia) // Incluimos Dependencia
             .Include(l => l.Proyecto)
                 .ThenInclude(p => p.TipoFondo) // Incluimos TipoFondo
             .Include(l => l.Proyecto)
                 .ThenInclude(p => p.Documentos)
             .Include(l => l.ContratistasParticipantes)
                 .ThenInclude(pc => pc.Contratista)
             .FirstOrDefaultAsync(m => m.Id == id);

        if (licitacion == null)
        {
            return NotFound();
        }

        // Ahora, llenamos nuestro ViewModel con los datos que obtuvimos.
        var viewModel = new LicitacionDetalleViewModel
        {
            LicitacionId = licitacion.Id,
            ProyectoId = licitacion.ProyectoId,
            NumeroLicitacion = licitacion.NumeroLicitacion,
            ProyectoNombre = licitacion.Proyecto.NombreProyecto,
            DescripcionLicitacion = licitacion.Descripcion,
            Estado = licitacion.Estado,
            FechaInicio = licitacion.FechaInicio,
            FechaFinPropuestas = licitacion.FechaFinPropuestas,

            // --- INFORMACIÓN NUEVA ---
            // Pasamos las coordenadas del proyecto al ViewModel.
            Latitud = licitacion.Proyecto.Latitud,
            Longitud = licitacion.Proyecto.Longitud,
            // -------------------------
            FolioProyecto = licitacion.Proyecto.Folio,
            PresupuestoProyecto = licitacion.Proyecto.Presupuesto,
            // Usamos el operador 'null conditional' (?) por si alguna relación no existiera
            CampusNombre = licitacion.Proyecto.Campus?.Nombre,
            DependenciaNombre = licitacion.Proyecto.Dependencia?.Nombre,
            TipoFondoNombre = licitacion.Proyecto.TipoFondo?.Nombre,

            Participantes = licitacion.ContratistasParticipantes.Select(p => new ParticipanteViewModel
            {
                ContratistaId = p.ContratistaId,
                RazonSocial = p.Contratista.RazonSocial,
                RFC = p.Contratista.RFC,
                EstadoParticipacion = p.EstadoParticipacion,
                FechaInvitacion = p.FechaInvitacion
            }).ToList(),
            Documentos = licitacion.Proyecto.Documentos.Select(d => new DocumentoViewModel
            {
                NombreArchivo = d.NombreArchivo,
                RutaArchivo = d.RutaArchivo
            }).ToList()
        };

        return View(viewModel);
    }

    // GET: Licitaciones/Invitar/5
    public async Task<IActionResult> Invitar(int id)
    {
        var licitacion = await _context.Licitaciones
                                       .Include(l => l.Proyecto)
                                       .FirstOrDefaultAsync(l => l.Id == id);
        if (licitacion == null) return NotFound();

        // Obtenemos los IDs de los contratistas que ya están invitados a esta licitación.
        var idsContratistasYaInvitados = await _context.LicitacionContratistas
            .Where(lc => lc.LicitacionId == id)
            .Select(lc => lc.ContratistaId)
            .ToListAsync();

        // Preparamos el ViewModel
        var viewModel = new InvitarContratistaViewModel
        {
            LicitacionId = licitacion.Id,
            NumeroLicitacion = licitacion.NumeroLicitacion,
            ProyectoNombre = licitacion.Proyecto.NombreProyecto,
            // Obtenemos todos los contratistas y marcamos los que ya fueron invitados.
            ContratistasDisponibles = await _context.Contratistas
                .Select(c => new ContratistaSeleccionable
                {
                    Id = c.Id,
                    RazonSocial = c.RazonSocial,
                    RFC = c.RFC,
                    YaEstaInvitado = idsContratistasYaInvitados.Contains(c.Id)
                })
                .ToListAsync()
        };

        return View(viewModel);
    }


    // POST: Licitaciones/Invitar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invitar(int licitacionId, List<int> contratistasSeleccionados)
    {
        if (contratistasSeleccionados == null || !contratistasSeleccionados.Any())
        {
            TempData["Error"] = "No se seleccionó ningún contratista.";
            return RedirectToAction("Invitar", new { id = licitacionId });
        }

        // Obtenemos las invitaciones que ya existen para no duplicarlas.
        var idsContratistasYaInvitados = await _context.LicitacionContratistas
            .Where(lc => lc.LicitacionId == licitacionId)
            .Select(lc => lc.ContratistaId)
            .ToListAsync();

        foreach (var contratistaId in contratistasSeleccionados)
        {
            // Solo agregamos la invitación si el contratista no está ya en la lista.
            if (!idsContratistasYaInvitados.Contains(contratistaId))
            {
                var nuevaInvitacion = new LicitacionContratista
                {
                    LicitacionId = licitacionId,
                    ContratistaId = contratistaId,
                    FechaInvitacion = DateTime.Now,
                    EstadoParticipacion = "Invitado" // Estado inicial
                };
                _context.LicitacionContratistas.Add(nuevaInvitacion);
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Contratistas invitados correctamente.";

        // Devolvemos al usuario a la página de detalles para que vea la lista actualizada.
        return RedirectToAction("Detalles", new { id = licitacionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarFechas(int licitacionId, DateTime fechaInicio, DateTime? fechaFinPropuestas)
    {
        var licitacion = await _context.Licitaciones.FindAsync(licitacionId);

        if (licitacion == null)
        {
            return NotFound();
        }

        // Actualizamos las propiedades de la licitación con los valores del formulario
        licitacion.FechaInicio = fechaInicio;
        licitacion.FechaFinPropuestas = fechaFinPropuestas;

        _context.Update(licitacion);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Las fechas de la licitación se han actualizado correctamente.";

        return RedirectToAction("Detalles", new { id = licitacionId });
    }
    // GET: Licitaciones/_InvitarContratistasPartial/5?busqueda=...
    // Esta acción devuelve la vista parcial con la lista de contratistas para el modal.
    public async Task<IActionResult> _InvitarContratistasPartial(int id, string busqueda)
    {
        var licitacion = await _context.Licitaciones
                                       .Include(l => l.Proyecto)
                                       .FirstOrDefaultAsync(l => l.Id == id);
        if (licitacion == null) return NotFound();

        var idsContratistasYaInvitados = await _context.LicitacionContratistas
            .Where(lc => lc.LicitacionId == id)
            .Select(lc => lc.ContratistaId)
            .ToListAsync();

        // Empezamos la consulta de contratistas
        var query = _context.Contratistas.AsQueryable();

        // Si hay un término de búsqueda, lo aplicamos
        if (!string.IsNullOrEmpty(busqueda))
        {
            query = query.Where(c => c.RazonSocial.Contains(busqueda) || c.RFC.Contains(busqueda));
        }

        var viewModel = new InvitarContratistaViewModel
        {
            LicitacionId = licitacion.Id,
            NumeroLicitacion = licitacion.NumeroLicitacion,
            ProyectoNombre = licitacion.Proyecto.NombreProyecto,
            ContratistasDisponibles = await query
                .Select(c => new ContratistaSeleccionable
                {
                    Id = c.Id,
                    RazonSocial = c.RazonSocial,
                    RFC = c.RFC,
                    YaEstaInvitado = idsContratistasYaInvitados.Contains(c.Id)
                })
                .OrderBy(c => c.RazonSocial)
                .ToListAsync()
        };

        return PartialView("_InvitarContratistasPartial", viewModel);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DevolverFase(int licitacionId, string comentario)
    {
        const int fasePresupuestoId = 3;
        const int faseLicitacionId = 4;

        var licitacion = await _context.Licitaciones
                                       .Include(l => l.Proyecto)
                                       .FirstOrDefaultAsync(l => l.Id == licitacionId);

        if (licitacion == null || licitacion.Proyecto == null)
        {
            return NotFound();
        }

        var proyecto = licitacion.Proyecto;
        var faseActualId = proyecto.IdFaseFk;

        if (proyecto.IdFaseFk != faseLicitacionId)
        {
            TempData["Error"] = "Esta acción solo es válida para proyectos en la fase 'En Licitación'.";
            return RedirectToAction("Detalles", new { id = licitacionId });
        }

        // --- BLOQUE NUEVO ---
        // 1. Buscamos y eliminamos primero a los "hijos" (las invitaciones)
        var invitaciones = await _context.LicitacionContratistas
            .Where(lc => lc.LicitacionId == licitacion.Id)
            .ToListAsync();

        if (invitaciones.Any())
        {
            _context.LicitacionContratistas.RemoveRange(invitaciones);
        }
        // -------------------

        // 2. Ahora sí, eliminamos al "padre" (la licitación)
        _context.Licitaciones.Remove(licitacion);

        // 3. Actualizamos el proyecto a la fase anterior
        proyecto.IdFaseFk = fasePresupuestoId;
        _context.Proyectos.Update(proyecto);

        // 4. Registrar el cambio en el historial
        var historial = new HistorialFase
        {
            ProyectoId = proyecto.Id,
            FaseAnteriorId = faseActualId,
            FaseNuevaId = fasePresupuestoId,
            FechaCambio = DateTime.Now,
            Comentario = comentario,
            TipoCambio = "Devuelto"
        };
        _context.HistorialFases.Add(historial);

        // 5. Guardar todos los cambios en la base de datos
        await _context.SaveChangesAsync();

        TempData["Success"] = $"El proyecto '{proyecto.NombreProyecto}' ha sido devuelto a la fase de presupuesto y la licitación ha sido eliminada.";

        return RedirectToAction("Index", "PanelDeFases");
    }

}