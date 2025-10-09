using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Data; // Asegúrate que este sea el namespace de tu DbContext
using ProyectoCGAPYS.Datos;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoCGAPYS.ViewComponents // El namespace debe coincidir con tu proyecto
{
    public class LicitacionesMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public LicitacionesMenuViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Buscamos los proyectos cuya fase sea "En Licitación".
            // Según tu script de base de datos, el Id de esta fase es 4.
            const int idFaseLicitacion = 4;

            var proyectosEnLicitacion = await _context.Proyectos
                .Where(p => p.IdFaseFk == idFaseLicitacion)
                .OrderBy(p => p.NombreProyecto)
                .ToListAsync();

            // Pasamos la lista de proyectos a una vista especial para el componente.
            return View(proyectosEnLicitacion);
        }
    }
}