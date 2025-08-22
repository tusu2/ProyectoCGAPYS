// En: Controllers/RegistroController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.Models;
using ProyectoCGAPYS.ViewModels; // <-- ¡Muy importante!

namespace ProyectoCGAPYS.Controllers
{
    public class RegistroController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public RegistroController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // Método GET: Prepara y muestra el formulario vacío
        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var viewModel = new CrearProyectoViewModel
            {
                // Carga las opciones para los dropdowns desde la base de datos
                DependenciaOptions = await _context.Dependencias
                    .Select(d => new SelectListItem { Value = d.Id, Text = d.Nombre }).ToListAsync(),
                TipoFondoOptions = await _context.TiposFondo
                    .Select(tf => new SelectListItem { Value = tf.Id, Text = tf.Nombre }).ToListAsync(),
                TipoProyectoOptions = await _context.TiposProyecto
                    .Select(tp => new SelectListItem { Value = tp.Id, Text = tp.Nombre }).ToListAsync(),
                CampusOptions = await _context.Campus 
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Nombre }).ToListAsync(),
           
            };
            return View(viewModel);
        }

        // Método POST: Recibe los datos enviados desde el formulario

        [HttpPost]
        public async Task<IActionResult> Crear(CrearProyectoViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var nuevoProyecto = new Proyectos
                    {
                        // ... (todas tus asignaciones de propiedades se quedan igual)
                        Id = Guid.NewGuid().ToString(),
                        NombreProyecto = viewModel.NombreProyecto,
                        Descripcion = viewModel.Descripcion,
                        FechaSolicitud = viewModel.FechaSolicitud,
                        FechaFinalizacionAprox = viewModel.FechaFinalizacionAprox,
                        NombreResponsable = viewModel.NombreResponsable,
                        Correo = viewModel.Correo,
                        Celular = viewModel.Celular,
                        Latitud = viewModel.Latitud,
                        Longitud = viewModel.Longitud,
                        Folio = "Auto",
                        Estatus = "Registrado",
                        IdCampusFk = viewModel.IdCampusFk,
                        IdDependenciaFk = viewModel.IdDependenciaFk,
                        IdTipoFondoFk = viewModel.IdTipoFondoFk,
                        IdTipoProyectoFk = viewModel.IdTipoProyectoFk
                    };

                    // ---- LÓGICA CORREGIDA PARA GUARDAR EL ARCHIVO ----
                    // Verificamos si se subió un archivo antes de procesarlo
                    if (viewModel.AnteproyectoFile != null && viewModel.AnteproyectoFile.Length > 0)
                    {
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string fileName = Path.GetFileNameWithoutExtension(viewModel.AnteproyectoFile.FileName);
                        string extension = Path.GetExtension(viewModel.AnteproyectoFile.FileName);
                        string uniqueFileName = fileName + DateTime.Now.ToString("yymmssfff") + extension;
                        string path = Path.Combine(wwwRootPath, "documentos", uniqueFileName);

                        using (var fileStream = new FileStream(path, FileMode.Create))
                        {
                            await viewModel.AnteproyectoFile.CopyToAsync(fileStream);
                        }

                        // Solo asignamos el nombre si el archivo fue guardado
                        nuevoProyecto.NombreAnteproyecto = uniqueFileName;
                    }

                    _context.Proyectos.Add(nuevoProyecto);
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "¡Proyecto guardado con éxito!" });
                }
                catch (Exception ex)
                {
                    // Es buena idea registrar el error para futura referencia
                    // logger.LogError(ex, "Error al crear proyecto");
                    return Json(new { success = false, message = "Ocurrió un error inesperado en el servidor."+ex });
                }
            }

            var errorMessages = ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage);

            return Json(new { success = false, message = string.Join("\n", errorMessages) });
        }
    }
}