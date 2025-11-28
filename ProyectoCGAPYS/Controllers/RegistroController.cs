// En: Controllers/RegistroController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.Models;
using ProyectoCGAPYS.ViewModels; // <-- ¡Muy importante!

namespace ProyectoCGAPYS.Controllers
{
    [Authorize(Roles = "Jefa")]
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
                DependenciaOptions = await _context.Dependencias
                    .Select(d => new SelectListItem { Value = d.Id, Text = d.Nombre }).ToListAsync(),
                TipoFondoOptions = await _context.TiposFondo
                    .Select(tf => new SelectListItem { Value = tf.Id, Text = tf.Nombre }).ToListAsync(),
                TipoProyectoOptions = await _context.TiposProyecto
                    .Select(tp => new SelectListItem { Value = tp.Id, Text = tp.Nombre }).ToListAsync(),
                CampusOptions = await _context.Campus
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Nombre }).ToListAsync(),

                // --- CAMBIO: Filtramos solo los dos usuarios específicos ---
                UsuariosOptions = await _context.Users
                    .Where(u => u.Email == "roselin.salazar@uadec.edu.mx" || u.Email == "jbazald@uadec.edu.mx")
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = u.Email
                    }).ToListAsync()
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

                    var ultimoProyecto = await _context.Proyectos
                .Where(p => p.Id.StartsWith("PROY-"))
                .OrderByDescending(p => p.Id)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

                    int siguienteNumero = 1; // Valor por defecto si es el primero

                    if (ultimoProyecto != null)
                    {
                        // 2. Extraemos la parte numérica. "PROY-" tiene 5 caracteres.
                        // Ejemplo: De "PROY-027" tomamos "027"
                        string numeroStr = ultimoProyecto.Substring(5);

                        if (int.TryParse(numeroStr, out int ultimoNumero))
                        {
                            siguienteNumero = ultimoNumero + 1;
                        }
                    }

                    // 3. Formateamos el nuevo ID con ceros a la izquierda (PadLeft)
                    // "D3" significa decimal con 3 dígitos: 1 -> "001", 28 -> "028"
                    string nuevoIdGenerado = $"PROY-{siguienteNumero:D3}";
                    var nuevoProyecto = new Proyectos
                    {
                        // ... (todas tus asignaciones de propiedades se quedan igual)
                        Id = nuevoIdGenerado,
                        IdFaseFk = 1,
                        NombreProyecto = viewModel.NombreProyecto,
                        Descripcion = string.IsNullOrEmpty(viewModel.Descripcion) ? "Sin descripción detallada" : viewModel.Descripcion,
                        FechaSolicitud = viewModel.FechaSolicitud,
                        FechaFinalizacionAprox = viewModel.FechaFinalizacionAprox,
                        UsuarioResponsableId = viewModel.UsuarioResponsableId,
                        NombreResponsable = null,
                        Correo = null,
                        Celular = null,
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