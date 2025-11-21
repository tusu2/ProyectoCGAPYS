using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; // ¡Muy importante para las consultas!; 
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.Models;
using ProyectoCGAPYS.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using ProyectoCGAPYS.ViewModels;
using Microsoft.AspNetCore.Authorization;
[Authorize(Roles = "Jefa")]
public class ProyectosController : Controller
{
    
    private readonly ApplicationDbContext _context;

    public ProyectosController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Proyectos/Detalle/FAM-LAG-2025-01
    public async Task<IActionResult> Detalle(string id, string tab = "resumen")
    {
        if (string.IsNullOrEmpty(id))
        {
            return NotFound();
        }

        // 1. Buscamos el proyecto y cargamos sus datos relacionados
        var proyecto = await _context.Proyectos
            .Include(p => p.Fase)       // Incluimos la fase para saber el nombre
            .Include(p => p.Campus)     // Incluimos el campus
            .Include(p => p.Dependencia) // Y la dependencia
             .Include(p => p.CostosDelProyecto).ThenInclude(costo => costo.Concepto)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (proyecto == null)
        {
            return NotFound();
        }

        // 2. Creamos el ViewModel
        var viewModel = new ProyectoDetalleViewModel
        {
            Proyecto = proyecto,
            CostosDelProyecto = proyecto.CostosDelProyecto.ToList(),
            // 3. Llenamos los datos para cada pestaña
            // (Aquí irían las consultas para Estimaciones, Documentos, Bitácora)
            Estimaciones = await _context.Estimaciones.Where(e => e.IdProyectoFk == id).ToListAsync(),
            // Documentos = ...
            // EntradasBitacora = ...

            TabActiva = tab // Para saber qué pestaña activar
        };

        // Calculamos los KPIs financieros
        viewModel.TotalEjercido = viewModel.Estimaciones.Where(e => e.Estado == "Pagada").Sum(e => e.Monto);
        viewModel.MontoContratado = proyecto.Presupuesto; // O el campo que corresponda
        viewModel.SaldoRestante = viewModel.MontoContratado - viewModel.TotalEjercido;
        ViewBag.Conceptos = new SelectList(await _context.Conceptos.ToListAsync(), "Id", "Descripcion");

        // 4. Mandamos el ViewModel a la vista
        return View(viewModel);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarCosto(AgregarCostoViewModel viewModel) // <-- Recibimos el ViewModel
    {
        if (ModelState.IsValid)
        {
            // El ViewModel es válido, ahora creamos la entidad que irá a la base de datos.
            var nuevoCosto = new Proyectos_Costos
            {
                Id = Guid.NewGuid().ToString(),
                IdProyectoFk = viewModel.IdProyectoFk,
                IdConceptoFk = viewModel.IdConceptoFk,
                Cantidad = viewModel.Cantidad,
                PrecioUnitario = viewModel.PrecioUnitario,
                ImporteTotal = viewModel.Cantidad * viewModel.PrecioUnitario // Calculamos el total
            };

            _context.Add(nuevoCosto);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "¡Concepto agregado exitosamente!";
        }
        else
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = "Error de validación: " + string.Join(" | ", errors);
        }

        return RedirectToAction("Detalle", new { id = viewModel.IdProyectoFk, tab = "financiero" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCosto(EditarCostoViewModel viewModel) // <-- Recibimos el ViewModel
    {
        if (ModelState.IsValid)
        {
            // 1. Buscamos el costo original en la base de datos
            var costoOriginal = await _context.Proyectos_Costos.FindAsync(viewModel.Id);

            if (costoOriginal != null)
            {
                // 2. Actualizamos solo las propiedades que cambiaron
                costoOriginal.Cantidad = viewModel.Cantidad;
                costoOriginal.PrecioUnitario = viewModel.PrecioUnitario;
                costoOriginal.ImporteTotal = viewModel.Cantidad * viewModel.PrecioUnitario; // Recalculamos

                // 3. Guardamos la entidad actualizada
                _context.Update(costoOriginal);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "¡Concepto actualizado exitosamente!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error: No se encontró el concepto a editar.";
            }
        }
        else
        {
            TempData["ErrorMessage"] = "No se pudo actualizar el concepto. Verifica los datos.";
        }

        return RedirectToAction("Detalle", new { id = viewModel.IdProyectoFk, tab = "financiero" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCosto(string Id, string IdProyectoFk)
    {
        var costoAEliminar = await _context.Proyectos_Costos.FindAsync(Id);
        if (costoAEliminar != null)
        {
            _context.Proyectos_Costos.Remove(costoAEliminar);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "¡Concepto eliminado exitosamente!";
        }
        else
        {
            TempData["ErrorMessage"] = "Error: No se encontró el concepto a eliminar.";
        }
        return RedirectToAction("Detalle", new { id = IdProyectoFk, tab = "financiero" });
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarDetalles(string Id, string Descripcion, string Prioridad)
    {
        var proyecto = await _context.Proyectos.FindAsync(Id);

        if (proyecto != null)
        {
            // 1. Actualizamos los datos básicos
            proyecto.Descripcion = Descripcion;
            proyecto.Prioridad = Prioridad;

            // 2. Lógica de Cambio de Fase Automático
            // Si estamos en Fase 1 (Recepción) y se ha asignado una prioridad, avanzamos a Fase 2
            if (proyecto.IdFaseFk == 1 && !string.IsNullOrEmpty(Prioridad))
            {
                int faseAnterior = 1;
                int faseNueva = 2; // "En Elaboración de Anteproyecto" según tu SQL

                proyecto.IdFaseFk = faseNueva;

                // 3. IMPORTANTE: Registrar en el Historial de Fases
                // Esto es necesario para que el Dashboard y el historial funcionen correctamente
                var historial = new HistorialFase
                {
                    ProyectoId = proyecto.Id,
                    FaseAnteriorId = faseAnterior,
                    FaseNuevaId = faseNueva,
                    Comentario = $"Cambio automático al asignar Prioridad: {Prioridad}",
                    FechaCambio = DateTime.Now,
                    TipoCambio = "Automático",
                    UsuarioId = null // O User.Identity.Name si tienes el ID del usuario logueado a la mano
                };

                _context.HistorialFases.Add(historial);

                TempData["SuccessMessage"] = "¡Detalles actualizados y Fase avanzada a 'Anteproyecto'!";
            }
            else
            {
                TempData["SuccessMessage"] = "¡Los detalles del proyecto se han actualizado!";
            }

            _context.Update(proyecto);
            await _context.SaveChangesAsync();
        }
        else
        {
            TempData["ErrorMessage"] = "No se encontró el proyecto para actualizar.";
        }

        return RedirectToAction("Detalle", new { id = Id, tab = "resumen" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelarProyecto(string Id)
    {
        var proyecto = await _context.Proyectos.FindAsync(Id);
        if (proyecto != null)
        {
            proyecto.Estatus = "Cancelado"; // Cambiamos el estado
            _context.Update(proyecto);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "El proyecto ha sido cancelado.";
        }
        return RedirectToAction("Index"); // Lo redirigimos a la lista principal de proyectos
    }

    
}

